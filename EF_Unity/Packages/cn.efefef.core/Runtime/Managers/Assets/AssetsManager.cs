/*
 * ================================================
 * Describe:      EF 核心资产管理器外观 + 工厂注册表 + 句柄化引用计数
 * Author:        Alvin8412
 * CreationTime:  2026-05-01 20:59:20
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-11-18 15:32:00
 * ScriptVersion: 0.5
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

namespace EasyFramework.Managers.Assets
{
    /// <summary>
    /// 资源管理器外观统一入口，路由到已注册的 IAssetsSystem 实现。
    /// 每次加载返回一个 <see cref="AssetHandle{T}"/>，释放由句柄 Dispose 驱动，用户无需手动按路径释放。
    /// <para>Assets system facade that routes to the registered IAssetsSystem implementation.
    /// Each load returns an <see cref="AssetHandle{T}"/>; release is driven by handle disposal.</para>
    /// </summary>
    [Manager(Order = 99600)]
    public class AssetsManager : MonoSingleton<AssetsManager>, ISingleton
    {
        #region 常量 Constants

        // 调试日志默认开关
        private const bool DefaultOpenDebug = false;

        // 日志前缀
        private const string LogTag = "[ AssetsManager ] ";

        // 默认包的键名（用于复合键）
        private const string DefaultPackageKey = "";

        #endregion

        #region 属性 Properties

        /// <summary>
        /// 当前使用的资源管理器类型
        /// <para>Type of the currently active asset system</para>
        /// </summary>
        public AssetsSystemType CurrentSystemType { get; private set; }

        /// <summary>
        /// 是否输出调试日志
        /// <para>Whether to output debug logs</para>
        /// </summary>
        public bool OpenDebug { get; private set; }

        #endregion

        #region 字段 Fields

        // 切换实现的信号量，串行化并发切换
        private readonly SemaphoreSlim _switchLock = new SemaphoreSlim(1, 1);

        // 引用计数锁，保护 _refCounts 的读写
        private readonly object _refGate = new object();

        // 是否已成功初始化过某个实现
        private bool _isInitialized;

        // 正在退出标记：置位后异步切换流程会放弃提交结果，避免 Quit 后新实现又被装载
        private volatile bool _quitting;

        // 当前生效的资源管理器实现
        private IAssetsSystem _assetsManager;

        // 引用计数：复合键(包名+路径) → 引用次数
        private Dictionary<AssetKey, int> _refCounts;

        // 静态工厂注册表 —— 各包在启动时通过 RegisterAssetSystem() 注册自己的 IAssetsSystem 工厂
        private static readonly Dictionary<AssetsSystemType, Func<IAssetsSystem>> Factories = new()
        {
            [AssetsSystemType.Default] = () => new DefaultAssetsSystem()
        };

        #endregion

        #region 嵌套类型 Nested Types

        // 引用计数复合键：区分不同资源包的同名路径
        private readonly struct AssetKey : IEquatable<AssetKey>
        {
            private readonly string _package;
            private readonly string _path;

            public AssetKey(string package, string path)
            {
                _package = package ?? DefaultPackageKey;
                _path = path;
            }

            public bool Equals(AssetKey other)
            {
                return string.Equals(_package, other._package, StringComparison.Ordinal)
                       && string.Equals(_path, other._path, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is AssetKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((_package != null ? _package.GetHashCode() : 0) * 397)
                           ^ (_path != null ? _path.GetHashCode() : 0);
                }
            }
        }

        #endregion

        #region 生命周期 Lifecycle

        void ISingleton.Init()
        {
            OpenDebug = DefaultOpenDebug;
            _quitting = false;
            _refCounts = new Dictionary<AssetKey, int>();
            InitDefaultAsync().Forget();
        }

        void ISingleton.Quit()
        {
            // 置位退出标记：让在途的 ConfirmAssetsManagerType 放弃后续提交，防止 Quit 后新实现又被装载。
            _quitting = true;
            _isInitialized = false;

            lock (_refGate)
            {
                _refCounts?.Clear();
                _refCounts = null;
            }

            // 不再用 _switchLock.Wait() 同步阻塞主线程：持锁方 ConfirmAssetsManagerType 在 await Initialize()
            // 时若需主线程续帧（可替换 IAssetsSystem 可能真异步），同步等待会造成死锁。
            // 改为 fire-and-forget 异步销毁，通过 WaitAsync 与在途切换串行化。
            DestroyCurrentAsync().Forget();
            // 不 Dispose _switchLock：InitDefaultAsync / 在途切换仍可能持有或等待它，随单例对象 GC。
        }

        // 异步销毁当前实现：等待在途切换结束后再销毁，全程不阻塞主线程
        private async UniTask DestroyCurrentAsync()
        {
            try
            {
                await _switchLock.WaitAsync();
                try
                {
                    var manager = _assetsManager;
                    _assetsManager = null;
                    if (manager != null)
                        await manager.Destroy();
                }
                finally
                {
                    _switchLock.Release();
                }
            }
            catch (Exception e)
            {
                Error($"Failed to destroy asset system on quit: {e}");
            }
        }

        // 初始化默认实现，捕获异常避免 fire-and-forget 静默失败
        private async UniTaskVoid InitDefaultAsync()
        {
            try
            {
                await ConfirmAssetsManagerType(AssetsSystemType.Default);
            }
            catch (Exception e)
            {
                Error($"Failed to initialize default asset system: {e}");
            }
        }

        #endregion

        #region 公开方法 Public Methods

        /// <summary>
        /// 注册资源管理器工厂（应在 <see cref="ISingleton.Init"/> 之前调用）
        /// <para>Register an IAssetsSystem factory (call before <see cref="ISingleton.Init"/>)</para>
        /// </summary>
        /// <param name="type">资源管理器类型<para>Asset system type</para></param>
        /// <param name="factory">工厂委托<para>Factory delegate</para></param>
        public static void RegisterAssetSystem(AssetsSystemType type, Func<IAssetsSystem> factory)
        {
            if (factory == null)
            {
                Error($"Cannot register null factory for type: {type}");
                return;
            }

            Factories[type] = factory;
        }

        /// <summary>
        /// 设置是否输出调试日志，并同步到当前实现
        /// <para>Set whether to output debug logs and sync to the active implementation</para>
        /// </summary>
        /// <param name="openDebug">是否开启<para>Whether to enable</para></param>
        public void SetOpenDebug(bool openDebug)
        {
            OpenDebug = openDebug;
            if (_assetsManager != null)
                _assetsManager.OpenDebug = openDebug;
        }

        /// <summary>
        /// 确认 / 切换资源管理器类型
        /// <para>Confirm or switch the asset manager type</para>
        /// </summary>
        /// <param name="systemType">目标管理器类型<para>Target asset system type</para></param>
        public async UniTask ConfirmAssetsManagerType(AssetsSystemType systemType)
        {
            await _switchLock.WaitAsync();
            try
            {
                // 拿锁后先检查退出标记：Quit 已排队销毁，此时不再装载新实现
                if (_quitting)
                    return;

                if (_isInitialized && CurrentSystemType == systemType && _assetsManager != null)
                {
                    Warning($"Already using [{CurrentSystemType}], no switch needed.");
                    return;
                }

                if (!Factories.TryGetValue(systemType, out var factory) || factory == null)
                {
                    Error($"No factory registered for {systemType}. Call RegisterAssetSystem() first.");
                    return;
                }

                IAssetsSystem newManager = factory();
                if (newManager == null)
                {
                    Error($"Factory for {systemType} returned null.");
                    return;
                }

                newManager.OpenDebug = OpenDebug;
                await newManager.Initialize();

                // await 期间可能发生 Quit：新实现已初始化但不能提交，立即销毁避免泄漏。
                // 旧实现由 DestroyCurrentAsync 负责销毁，此处不重复处理。
                if (_quitting)
                {
                    await newManager.Destroy();
                    return;
                }

                var oldManager = _assetsManager;
                if (oldManager != null)
                {
                    Log($"Destroying old manager: {CurrentSystemType}");
                    await oldManager.Destroy();
                }

                lock (_refGate)
                    _refCounts?.Clear();
                _assetsManager = newManager;
                CurrentSystemType = systemType;
                _isInitialized = true;

                Log($"Initialized with [{systemType}].");
            }
            finally
            {
                _switchLock.Release();
            }
        }

        /// <summary>
        /// 从默认包同步加载资源
        /// <para>Synchronously load an asset from the default package</para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="path">资源路径<para>Asset path</para></param>
        /// <returns>加载句柄，失败返回 null<para>Load handle, or null on failure</para></returns>
        public AssetHandle<T> Load<T>(string path) where T : Object
        {
            if (!CheckInitialization())
                return null;
            var obj = _assetsManager.Load<T>(path);
            return WrapHandle(obj, null, path);
        }

        /// <summary>
        /// 从默认包异步加载资源
        /// <para>Asynchronously load an asset from the default package</para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="path">资源路径<para>Asset path</para></param>
        /// <param name="token">取消令牌<para>Cancellation token</para></param>
        /// <returns>加载句柄，失败返回 null<para>Load handle, or null on failure</para></returns>
        public async UniTask<AssetHandle<T>> LoadAsync<T>(string path, CancellationToken token = default)
            where T : Object
        {
            if (!CheckInitialization())
                return null;
            var obj = await _assetsManager.LoadAsync<T>(path, token);
            return WrapHandle(obj, null, path);
        }

        /// <summary>
        /// 从指定资源包同步加载资源
        /// <para>Synchronously load an asset from a named package</para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="packageName">包名<para>Package name</para></param>
        /// <param name="path">资源路径<para>Asset path</para></param>
        /// <returns>加载句柄，失败返回 null<para>Load handle, or null on failure</para></returns>
        public AssetHandle<T> LoadFromPackage<T>(string packageName, string path) where T : Object
        {
            if (!CheckInitialization())
                return null;
            var obj = _assetsManager.LoadFromPackage<T>(packageName, path);
            return WrapHandle(obj, packageName, path);
        }

        /// <summary>
        /// 从指定资源包异步加载资源
        /// <para>Asynchronously load an asset from a named package</para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="packageName">包名<para>Package name</para></param>
        /// <param name="path">资源路径<para>Asset path</para></param>
        /// <param name="token">取消令牌<para>Cancellation token</para></param>
        /// <returns>加载句柄，失败返回 null<para>Load handle, or null on failure</para></returns>
        public async UniTask<AssetHandle<T>> LoadAsyncFromPackage<T>(string packageName, string path,
            CancellationToken token = default) where T : Object
        {
            if (!CheckInitialization())
                return null;
            var obj = await _assetsManager.LoadAsyncFromPackage<T>(packageName, path, token);
            return WrapHandle(obj, packageName, path);
        }

        /// <summary>
        /// 释放所有已加载资源，清空引用计数（兜底用，场景切换 / 退出时调用）
        /// <para>Release all loaded assets and clear ref-counts (fallback for scene switch / exit)</para>
        /// </summary>
        public async UniTask ReleaseAll()
        {
            if (!CheckInitialization())
                return;

            await _assetsManager.ReleaseAll();
            lock (_refGate)
                _refCounts.Clear();
            Log("All assets released.");
        }

        /// <summary>
        /// 主动清理未使用的资源
        /// <para>Proactively unload unused assets</para>
        /// </summary>
        public async UniTask CleanupUnusedAssets()
        {
            if (!CheckInitialization())
                return;

            await _assetsManager.CleanupUnusedAssets();
            Log("Unused assets cleaned up.");
        }

        #endregion

        #region 内部方法 Internal Methods

        // 包装加载结果为句柄，成功时登记引用计数
        private AssetHandle<T> WrapHandle<T>(T obj, string packageName, string path) where T : Object
        {
            if (null == obj)
            {
                Error($"Load failed: {path}");
                return null;
            }

            var key = new AssetKey(packageName, path);
            lock (_refGate)
            {
                // 加载与 Quit 竞态：退出已摘走引用计数表时不再登记，句柄退化为不计数（其 Dispose 也会因 _quitting 安全返回）
                if (_refCounts == null)
                    return new AssetHandle<T>(this, packageName, path, obj);
                _refCounts.TryGetValue(key, out var count);
                _refCounts[key] = count + 1;
            }

            Log($"Load succeeded: {packageName ?? "default"}:{path}");
            return new AssetHandle<T>(this, packageName, path, obj);
        }

        // 供句柄 Dispose 调用：引用计数减一，归零时释放底层资源
        internal void ReleaseInternal(string packageName, string path)
        {
            // 退出流程可能已把实现与引用计数表摘走：句柄在此时 Dispose 属于正常竞态，静默忽略即可
            if (_quitting || _assetsManager == null)
                return;

            // 捕获当前实现引用：异步释放期间若 ConfirmAssetsManagerType/Quit 改写了 _assetsManager，
            // 必须向持有资源的那个实现释放，而非新实现（否则释放错对象/NRE，引用计数已扣无法回滚）
            var manager = _assetsManager;

            var key = new AssetKey(packageName, path);
            bool shouldRelease;
            lock (_refGate)
            {
                if (_refCounts == null || !_refCounts.TryGetValue(key, out var count))
                    return;
                shouldRelease = count <= 1;
                if (shouldRelease)
                    _refCounts.Remove(key);
                else
                    _refCounts[key] = count - 1;
            }

            if (shouldRelease)
                ReleaseUnderlyingAsync(manager, packageName, path).Forget();
        }

        // 真正释放底层资源，隔离异常避免句柄 Dispose 抛出
        private async UniTaskVoid ReleaseUnderlyingAsync(IAssetsSystem manager, string packageName, string path)
        {
            try
            {
                await manager.Release(packageName, path);
                Log($"Released: {packageName ?? "default"}:{path}");
            }
            catch (Exception e)
            {
                Error($"Failed to release {path}: {e}");
            }
        }

        // 检查是否已通过 ConfirmAssetsManagerType 完成初始化
        private bool CheckInitialization()
        {
            if (_isInitialized && _assetsManager != null)
                return true;

            Error("Call ConfirmAssetsManagerType() before any asset operations.");
            return false;
        }

        private void Log(string msg)
        {
            if (OpenDebug) D.Log(LogTag + msg);
        }

        private void Warning(string msg)
        {
            if (OpenDebug) D.Warning(LogTag + msg);
        }

        private static void Error(string msg) => D.Error(LogTag + msg);

        #endregion
    }
}
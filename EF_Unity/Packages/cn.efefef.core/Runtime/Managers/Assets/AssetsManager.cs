/*
 * ================================================
 * Describe:      EF 核心资产管理器外观 + 工厂注册表 + 引用计数
 * Author:        Alvin8412
 * CreationTime:  2026-05-01 20:59:20
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-30 17:34:00
 * ScriptVersion: 0.3
 * ================================================
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

namespace EasyFramework.Managers.Assets
{
    /// <summary>
    /// 资源管理器外观统一入口，路由到已注册的 IAssetsSystem 实现
    /// <para>Assets system facade unified entry point that routes to the registered IAssetsSystem implementation</para>
    /// </summary>
    [Manager(Order = 99600)]
    public class AssetsManager : MonoSingleton<AssetsManager>, ISingleton
    {
        /// <summary>
        /// 当前使用的资源管理器类型
        /// <para>Type of the currently active asset system</para>
        /// </summary>
        public AssetsSystemType CurrentSystemType { get; private set; }

        private bool _openDebug;
        private bool _isInitialized;

        /// <summary>当前生效的资源管理器实现<para>The active IAssetsSystem implementation</para></summary>
        private IAssetsSystem _assetsManager;

        /// <summary>引用计数器（key = 资源路径，value = 引用次数）<para>Reference counter (key = asset path, value = reference count)</para></summary>
        private Dictionary<string, int> _refCounts;

        /// <summary>
        /// 静态工厂注册表 —— 各包在启动时通过 RegisterAssetSystem() 注册自己的 IAssetsSystem 工厂
        /// <para>Static factory registry — each package registers its IAssetsSystem factory via RegisterAssetSystem() at startup</para>
        /// </summary>
        private static readonly Dictionary<AssetsSystemType, Func<IAssetsSystem>> Factories = new()
        {
            [AssetsSystemType.Default] = () => new DefaultAssetsSystem()
        };

        void ISingleton.Init()
        {
            _openDebug = false;
            _refCounts = new Dictionary<string, int>();
            ConfirmAssetsManagerType(AssetsSystemType.Default).Forget();
        }

        void ISingleton.Quit()
        {
            if (null != _assetsManager)
            {
                _assetsManager.Destroy().Forget();
                _assetsManager = null;
            }

            _refCounts?.Clear();
            _refCounts = null;
        }

        /// <summary>
        /// 注册资源管理器工厂（应在 <see cref="ISingleton.Init"/> 之前调用）
        /// <para>Register an IAssetsSystem factory (call before <see cref="ISingleton.Init"/>)</para>
        /// </summary>
        /// <param name="type">资源管理器类型<para>Asset system type</para></param>
        /// <param name="factory">工厂委托：返回一个新的 IAssetsSystem 实例<para>Factory delegate that returns a new IAssetsSystem instance</para></param>
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
        /// 确认 / 切换资源管理器类型 —— 必须在加载任何资源之前调用
        /// <para>Confirm or switch the asset manager type — must be called before any asset loading</para>
        /// </summary>
        /// <param name="systemType">目标管理器类型<para>Target asset system type</para></param>
        public async UniTask ConfirmAssetsManagerType(AssetsSystemType systemType)
        {
            if (_isInitialized && CurrentSystemType == systemType)
            {
                Warning($"Already using [{CurrentSystemType}], no switch needed.");
                return;
            }

            if (_assetsManager != null)
            {
                Log($"Destroying old manager: {CurrentSystemType}");
                await _assetsManager.Destroy();
                _assetsManager = null;
            }

            _refCounts?.Clear();

            // 从工厂注册表中按类型创建实例
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

            newManager.OpenDebug = _openDebug;
            await newManager.Initialize();

            _assetsManager = newManager;
            CurrentSystemType = systemType;
            _isInitialized = true;

            Log($"Initialized with [{systemType}].");
        }

        /// <summary>
        /// 从默认包同步加载资源
        /// <para>Synchronously load an asset from the default package</para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="path">资源路径<para>Asset path</para></param>
        /// <returns>加载成功返回资源对象，失败返回 null<para>The loaded asset, or null on failure</para></returns>
        public T Load<T>(string path) where T : Object
        {
            if (!CheckInitialization())
                return null;
            var obj = _assetsManager.Load<T>(path);
            CheckObject(obj, path);
            return obj;
        }

        /// <summary>
        /// 从默认包异步加载资源
        /// <para>Asynchronously load an asset from the default package</para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="path">资源路径<para>Asset path</para></param>
        /// <returns>加载成功返回资源对象，失败返回 null<para>The loaded asset, or null on failure</para></returns>
        public async UniTask<T> LoadAsync<T>(string path) where T : Object
        {
            if (!CheckInitialization())
                return null;
            var obj = await _assetsManager.LoadAsync<T>(path);
            CheckObject(obj, path);
            return obj;
        }

        /// <summary>
        /// 从指定资源包同步加载资源
        /// <para>Synchronously load an asset from a named resource package</para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="packageName">包名（需已通过 PatchManager.RegisterSubPackages 注册）<para>Package name (must be registered via PatchManager.RegisterSubPackages)</para></param>
        /// <param name="path">资源路径<para>Asset path</para></param>
        /// <returns>加载成功返回资源对象，失败返回 null<para>The loaded asset, or null on failure</para></returns>
        public T LoadFromPackage<T>(string packageName, string path) where T : Object
        {
            if (!CheckInitialization())
                return null;
            var obj = _assetsManager.LoadFromPackage<T>(packageName, path);
            CheckObject(obj, path);
            return obj;
        }

        /// <summary>
        /// 从指定资源包异步加载资源
        /// <para>Asynchronously load an asset from a named resource package</para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="packageName">包名（需已通过 PatchManager.RegisterSubPackages 注册）<para>Package name (must be registered via PatchManager.RegisterSubPackages)</para></param>
        /// <param name="path">资源路径<para>Asset path</para></param>
        /// <returns>加载成功返回资源对象，失败返回 null<para>The loaded asset, or null on failure</para></returns>
        public async UniTask<T> LoadAsyncFromPackage<T>(string packageName, string path) where T : Object
        {
            if (!CheckInitialization())
                return null;
            var obj = await _assetsManager.LoadAsyncFromPackage<T>(packageName, path);
            CheckObject(obj, path);
            return obj;
        }

        /// <summary>
        /// 获取资源的当前引用计数
        /// <para>Get the current reference count for an asset</para>
        /// </summary>
        /// <param name="path">资源路径<para>Asset path</para></param>
        /// <returns>引用次数，未加载则返回 0<para>Reference count, or 0 if the asset has not been loaded</para></returns>
        public async UniTask<int> GetRefCount(string path)
        {
            await UniTask.CompletedTask;
            if (!CheckInitialization())
                return 0;
            Log($"Query refCount: {path}");
            return _refCounts.GetValueOrDefault(path, 0);
        }

        /// <summary>
        /// 释放指定资源（引用计数减 1；归零时真正释放底层资源）
        /// <para>Release an asset (decrements ref-count; truly releases the underlying asset when count reaches zero)</para>
        /// </summary>
        /// <param name="path">资源路径<para>Asset path</para></param>
        public async UniTask Release(string path)
        {
            if (!CheckInitialization())
                return;

            if (!_refCounts.TryGetValue(path, out var count))
            {
                Warning($"Attempt to release unloaded asset: {path}");
                return;
            }

            if (--count < 0)
            {
                Warning($"Ref-count underflow for: {path}, count={count}");
                count = 0;
            }

            if (count == 0)
            {
                await _assetsManager.Release(path);
                _refCounts.Remove(path);
                Log($"Released: {path}");
            }
            else
            {
                _refCounts[path] = count;
            }
        }

        /// <summary>
        /// 释放所有已加载资源，清空引用计数
        /// <para>Release all loaded assets and clear all reference counts</para>
        /// </summary>
        public async UniTask ReleaseAll()
        {
            if (!CheckInitialization())
                return;

            await _assetsManager.ReleaseAll();
            _refCounts.Clear();
            Log("All assets released.");
        }

        /// <summary>
        /// 主动清理未使用的资源（GameObject / Component 等无法被 UnloadAsset 回收的对象也会被清理）
        /// <para>Proactively unload unused assets (including GameObjects and Components that cannot be freed by UnloadAsset alone)</para>
        /// </summary>
        public async UniTask CleanupUnusedAssets()
        {
            if (!CheckInitialization())
                return;

            await _assetsManager.CleanupUnusedAssets();
            Log("Unused assets cleaned up.");
        }

        #region 私有函数

        /// <summary>
        /// 检查是否已通过 ConfirmAssetsManagerType 完成初始化
        /// <para>Verify that the manager has been initialized via ConfirmAssetsManagerType</para>
        /// </summary>
        private bool CheckInitialization()
        {
            if (_isInitialized && _assetsManager != null)
                return true;

            Error("Call ConfirmAssetsManagerType() before any asset operations.");
            return false;
        }

        /// <summary>
        /// 校验加载结果，并将路径记入引用计数（首次加载 count=1，重复加载 count+1）
        /// <para>Validate the load result and record the path in the reference counter (first load count=1, repeat load count+1)</para>
        /// </summary>
        private void CheckObject<T>(T obj, string path) where T : Object
        {
            if (null == obj)
            {
                Error($"Load failed: {path}");
                return;
            }

            Log($"Load succeeded: {path}");
            int refCount = 1;
            if (_refCounts.TryGetValue(path, out int existing))
                refCount += existing;
            _refCounts[path] = refCount;
        }

        private void Log(string msg)
        {
            if (_openDebug) D.Log($"[ AssetsManager ] {msg}");
        }

        private void Warning(string msg)
        {
            if (_openDebug) D.Warning($"[ AssetsManager ] {msg}");
        }

        private static void Error(string msg) => D.Error($"[ AssetsManager ] {msg}");

        #endregion
    }
}
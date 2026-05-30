/*
 * ================================================
 * Describe:      框架中的核心资产管理器
 * Author:        Alvin8412
 * CreationTime:  2026-05-01 20:59:20
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-29 16:20:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

namespace EasyFramework.Managers.Assets
{
    /// <summary>
    /// 资源管理器
    /// <para>Assets system facade - routes to registered IAssetsSystem implementations</para>
    /// </summary>
    [Manager(Order = 99600)]
    public class AssetsManager : MonoSingleton<AssetsManager>, ISingleton
    {
        /// <summary> 当前管理器类型 <para>Current asset system type</para></summary>
        public AssetsSystemType CurrentSystemType { get; private set; }

        private bool _openDebug;
        private bool _isInitialized;

        private IAssetsSystem _assetsManager;
        private Dictionary<string, int> _refCounts;        // 引用计数器（key = 资源路径）

        // 工厂注册表：各包通过 RegisterAssetSystem(type, factory) 注册实现
        private static readonly Dictionary<AssetsSystemType, Func<IAssetsSystem>> Factories = new()
        {
            [AssetsSystemType.Default] = () => new DefaultAssetsSystem()
        };

        /// <summary>
        /// 注册资源管理器工厂（应在 AssetsSystem.Init 之前调用）
        /// <para>Register an IAssetsSystem factory. Call before AssetsSystem.Init.</para>
        /// </summary>
        /// <param name="type">资源管理器类型<para>Asset system type</para></param>
        /// <param name="factory">工厂委托<para>Factory delegate that returns a new IAssetsSystem instance</para></param>
        public static void RegisterAssetSystem(AssetsSystemType type, Func<IAssetsSystem> factory)
        {
            if (factory == null)
            {
                D.Error($"[ AssetsRootManager ] Cannot register null factory for type: {type}");
                return;
            }

            Factories[type] = factory;
            D.Log($"[ AssetsRootManager ] Registered asset system factory for: {type}");
        }

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
        /// 确认/切换资源管理器类型（需在加载任何资源前调用）
        /// <para>Confirm or switch the asset manager type. Must be called before any asset loading.</para>
        /// </summary>
        public async UniTask ConfirmAssetsManagerType(AssetsSystemType systemType)
        {
            if (_isInitialized && CurrentSystemType == systemType)
            {
                Warning($"The current manager [ {CurrentSystemType} ] you are using is exactly the one you want to switch to.");
                return;
            }

            if (_assetsManager != null)
            {
                Log($"Destroying old assets manager: {CurrentSystemType}");
                await _assetsManager.Destroy();
                _assetsManager = null;
            }

            _refCounts?.Clear();

            // 从工厂注册表中创建实例
            if (!Factories.TryGetValue(systemType, out var factory) || factory == null)
            {
                Error($"No factory registered for AssetsSystemType: {systemType}. Call RegisterAssetSystem() first.");
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

            Log($"initialized by type[ {systemType} ].");
        }

        /// <summary>
        /// 同步加载资源
        /// <para>Synchronously load an asset by path</para>
        /// </summary>
        /// <param name="path">资源地址<para>Asset path</para></param>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <returns>资源<para>Loaded asset, or null on failure</para></returns>
        public T Load<T>(string path) where T : Object
        {
            if (!CheckInitialization())
                return null;
            var obj = _assetsManager.Load<T>(path);
            CheckObject(obj, path);
            return obj;
        }

        /// <summary>
        /// 异步加载资源
        /// <para>Asynchronously load an asset by path</para>
        /// </summary>
        /// <param name="path">资源地址<para>Asset path</para></param>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <returns>资源<para>Loaded asset, or null on failure</para></returns>
        public async UniTask<T> LoadAsync<T>(string path) where T : Object
        {
            await UniTask.CompletedTask;
            if (!CheckInitialization())
                return null;
            var obj = await _assetsManager.LoadAsync<T>(path);
            CheckObject(obj, path);
            return obj;
        }

        /// <summary>
        /// 获取资源引用计数
        /// <para>Get the reference count for an asset</para>
        /// </summary>
        /// <param name="path">资源地址<para>Asset path</para></param>
        /// <returns>引用数量<para>Reference count</para></returns>
        public async UniTask<int> GetRefCount(string path)
        {
            await UniTask.CompletedTask;
            if (!CheckInitialization())
                return 0;
            Log($"Get refCount by path: {path}");
            return _refCounts.GetValueOrDefault(path, 0);
        }

        /// <summary>
        /// 释放资源（引用计数减1，归零时真正释放）
        /// <para>Release an asset (decrements ref-count; truly releases when count reaches zero)</para>
        /// </summary>
        /// <param name="path">资源地址<para>Asset path</para></param>
        public async UniTask Release(string path)
        {
            await UniTask.CompletedTask;
            if (!CheckInitialization())
                return;

            if (!_refCounts.TryGetValue(path, out var count))
            {
                Warning($"Attempt to release unloaded assets: {path}");
                return;
            }

            if (--count < 0)
            {
                Warning($"Resource reference count exception: {path}, Current count: {count}");
                count = 0;
            }

            if (count == 0)
            {
                await _assetsManager.Release(path);
                _refCounts.Remove(path);
                Log($"Release succeed by path: {path}");
            }
            else
                _refCounts[path] = count;
        }

        /// <summary>
        /// 释放全部资源
        /// <para>Release all loaded assets</para>
        /// </summary>
        public async UniTask ReleaseAll()
        {
            await UniTask.CompletedTask;
            if (!CheckInitialization())
                return;

            await _assetsManager.ReleaseAll();
            _refCounts.Clear();   // 清空所有引用计数
            Log("Release all succeed.");
        }

        /// <summary>
        /// 主动清理未使用的资源（包括之前被 Release 的 GameObject预制体 或 Component 资源）
        /// <para>Unload unused assets proactively</para>
        /// </summary>
        public async UniTask CleanupUnusedAssets()
        {
            await UniTask.CompletedTask;
            if (!CheckInitialization())
                return;

            await _assetsManager.CleanupUnusedAssets();
            Log("Cleanup the unused assets succeed.");
        }

        #region 私有函数

        // 检查是否已初始化
        private bool CheckInitialization()
        {
            if (_isInitialized && _assetsManager != null)
                return true;

            Error("You should first initialize [AssetsRootManager] via ConfirmAssetsManagerType() before using any asset operations.");
            return false;
        }

        // 检查对象并且设置相关引用数量
        private void CheckObject<T>(T obj, string path) where T : Object
        {
            if (null == obj)
            {
                Error($"Loading assets fail: {path}");
                return;
            }

            Log($"Loading assets succeed: {path}");
            int refCount = 1;
            if (_refCounts.TryGetValue(path, out int existing))
                refCount += existing;
            _refCounts[path] = refCount;
        }

        private void Log(string msg)
        {
            if (_openDebug) D.Log($"[ AssetsRootManager ] {msg}");
        }
        private void Warning(string msg)
        {
            if (_openDebug) D.Warning($"[ AssetsRootManager ] {msg}");
        }
        private void Error(string msg) => D.Error($"[ AssetsRootManager ] {msg}");

        #endregion
    }
}

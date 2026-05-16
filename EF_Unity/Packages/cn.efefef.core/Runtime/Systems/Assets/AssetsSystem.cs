/*
 * ================================================
 * Describe:      框架中的核心资产管理器
 * Author:        Alvin8412
 * CreationTime:  2026-05-01 20:59:20
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-01 20:59:20
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers;
using UnityEngine;

namespace EasyFramework.Systems.Assets
{
    /// <summary>
    /// 资源管理器
    /// </summary>
    [Manager]
    public class AssetsSystem : MonoSingleton<AssetsSystem>, ISingleton
    {
        /// <summary> 当前管理器类型 </summary>
        public AssetsSystemType CurrentSystemType { get; private set; }

        private bool _openDebug;
        private bool _isInitialized;

        private IAssetsSystem _assetsManager;
        private Dictionary<string, int> _refCounts;

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

            IAssetsSystem newManager = systemType switch
            {
                AssetsSystemType.Default => new DefaultAssetsSystem(),
                AssetsSystemType.YooAsset => new YooAssetsSystem(),
                _ => null
            };

            if (newManager == null)
            {
                Error($"Unsupported AssetsSystemType: {systemType}");
                return;
            }

            newManager.OpenDebug = _openDebug;
            await newManager.Initialize();

            _assetsManager = newManager;
            CurrentSystemType = systemType;
            _isInitialized = true;

            Log($"[ AssetsRootManager ] initialized by type[ {systemType} ].");
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="path">资源地址</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns>资源</returns>
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
        /// </summary>
        /// <param name="path">资源地址</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns>资源</returns>
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
        /// </summary>
        /// <param name="path">资源地址</param>
        /// <returns>引用数量</returns>
        public async UniTask<int> GetRefCount(string path)
        {
            await UniTask.CompletedTask;
            if (!CheckInitialization()) 
                return 0;
            Log($"[ AssetsRootManager ] Get refCount by path: {path}");
            return _refCounts.GetValueOrDefault(path, 0);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="path">资源地址</param>
        public async UniTask Release(string path)
        {
            await UniTask.CompletedTask;
            if (!CheckInitialization())
                return;
            
            if (!_refCounts.TryGetValue(path, out var count))
            {
                Warning($"[ AssetsRootManager ] Attempt to release unloaded assets: {path}");
                return;
            }

            if (--count < 0)
            {
                Warning($"[ AssetsRootManager ] Resource reference count exception: {path}, Current count: {count}");
                count = 0;
            }

            if (count == 0)
            {
                await _assetsManager.Release(path);
                _refCounts.Remove(path);
                Log($"[ AssetsRootManager ] Release succeed by path: {path}");
            }
            else
                _refCounts[path] = count;
        }

        /// <summary>
        /// 释放全部资源
        /// </summary>
        public async UniTask ReleaseAll()
        {
            await UniTask.CompletedTask;
            if (!CheckInitialization())
                return;
            
            await _assetsManager.ReleaseAll();
            _refCounts.Clear();   // 清空所有引用计数
            Log("[ AssetsRootManager ] Release all succeed.");
        }

        /// <summary>
        /// 主动清理未使用的资源（包括之前被 Release 的 GameObject预制体 或 Component 资源）
        /// </summary>
        public async UniTask CleanupUnusedAssets()
        {
            await UniTask.CompletedTask;
            if (!CheckInitialization())
                return;

            await _assetsManager.CleanupUnusedAssets();
            Log("[ AssetsRootManager ] Cleanup the unused assets succeed.");
        }

        #region 私有函数

        private bool CheckInitialization()
        {
            if (_isInitialized && _assetsManager != null)
                return true;

            Error("You should first initialize [AssetsRootManager] via ConfirmAssetsManagerType() before using any asset operations.");
            return false;
        }

        private void CheckObject<T>(T obj, string path) where T : Object
        {
            if (null == obj)
            {
                Error($"[ AssetsRootManager ] Loading assets fail: {path}");
                return;
            }

            Log($"[ AssetsRootManager ] Loading assets succeed: {path}");
            int refCount = 1;
            if (_refCounts.TryGetValue(path, out int existing))
                refCount += existing;
            _refCounts[path] = refCount;
        }

        private void Log(string msg)
        {
            if (_openDebug) D.Log(msg);
        }
        private void Warning(string msg)
        {
            if (_openDebug) D.Warning(msg);
        }
        private void Error(string msg)
        {
            if (_openDebug) D.Error(msg);
        }
        
        #endregion
    }
}
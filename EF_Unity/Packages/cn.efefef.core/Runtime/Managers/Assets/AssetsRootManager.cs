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
using EasyFramework;
using EasyFramework.Managers.Assets;
using UnityEngine;

namespace EFExample
{
    /// <summary>
    /// 资源管理器
    /// </summary>
    public class AssetsRootManager : MonoSingleton<AssetsRootManager>, IManager
    {
        /// <summary> 当前管理器类型 </summary>
        public AssetsManagerType CurrentManagerType { get; private set; }

        private bool _openDebug;
        private bool _isInitialized;

        private IAssetsManager _assetsManager;

        private Dictionary<string, int> _refCounts;

        void ISingleton.Init()
        {
            _openDebug = true;
            _refCounts = new Dictionary<string, int>();
        }

        void ISingleton.Quit()
        {
            if (null != _assetsManager)
            {
                _assetsManager.Destroy();
                _assetsManager = null;
            }

            if (null != _refCounts)
            {
                _refCounts.Clear();
                _refCounts = null;
            }
        }

        public async UniTask ConfirmAssetsManagerType(AssetsManagerType managerType)
        {
            if (!_isInitialized)
            {
                //  初始化


                _isInitialized = true;
            }

            if (CurrentManagerType == managerType)
            {
                Warning($"The current manager [ {CurrentManagerType} ] you are using is exactly the one you want to switch to.");
                return;
            }
            
            CurrentManagerType = managerType;
            //  切换

            switch (managerType)
            {
                case AssetsManagerType.Default:
                    _assetsManager = new DefaultAssetsManager();
                    break;
                case AssetsManagerType.YooAsset:
                    _assetsManager = new YooAssetsManager();
                    break;
                default:
                    break;
            }
            
            _assetsManager.OpenDebug = _openDebug;
            await _assetsManager.Initialize();
            Log($"[ AssetsRootManager ] initialized by type[ {managerType} ].");
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
            Log($"[ AssetsRootManager ] Get refCount by path: {path}");
            return !CheckInitialization() ? 0 : _refCounts.GetValueOrDefault(path, 0);
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
                Warning($"[ AssetsRootManager ] Attempt to release unloaded resources: {path}");
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
            Log("[ AssetsRootManager ] Cleanup ths unused assets succeed.");
        }

        private bool CheckInitialization()
        {
            if (_isInitialized)
                return true;
            
            Error("Your should been initialize the [ AssetsRootManager ], through the ConfirmAssetsManagerType function");
            return false;
        }

        private void CheckObject<T>(T obj, string path)
        {
            if (null == obj)
            {
                Error($"[ AssetsRootManager ] Loading resources fail: {path}");
                return;
            }
            Log($"[ AssetsRootManager ] Loading resources succeed: {path}");
            int refCount = 1;
            if (_refCounts.TryGetValue(path, out int count))
                refCount += count;
            _refCounts[path] = refCount;
        }
        
        private void Log(string msg)
        {
            if (_openDebug)
            {
                D.Log(msg);
            }
        }
        private void Warning(string msg)
        {
            if (_openDebug)
            {
                D.Warning(msg);
            }
        }
        private void Error(string msg)
        {
            if (_openDebug)
            {
                D.Error(msg);
            }
        }
    }
}
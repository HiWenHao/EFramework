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


        private bool _isInitialized;

        private IAssetsManager _assetsManager;

        void ISingleton.Init()
        {
            ConfirmAssetsManagerType(AssetsManagerType.Default);
        }

        void ISingleton.Quit()
        {
            if (null == _assetsManager)
                return;

            _assetsManager.Destroy();
            _assetsManager = null;
        }

        public void ConfirmAssetsManagerType(AssetsManagerType managerType)
        {
            if (_isInitialized)
            {
                //  初始化


                _isInitialized = true;
            }

            CurrentManagerType = managerType;
            //  切换

            switch (managerType)
            {
                case AssetsManagerType.Default:
                    _assetsManager = new DefaultAssetsManager();
                    break;
                case AssetsManagerType.YooAsset:
                    break;
                default:
                    break;
            }

            _assetsManager.Initialize();
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="path">资源地址</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns>资源</returns>
        public T Load<T>(string path) where T : Object => _assetsManager.Load<T>(path);

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源地址</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns>资源</returns>
        public async UniTask<T> LoadAsync<T>(string path) where T : Object => await _assetsManager.LoadAsync<T>(path);

        /// <summary>
        /// 获取资源引用计数
        /// </summary>
        /// <param name="path">资源地址</param>
        /// <returns>引用数量</returns>
        public int GetRefCount(string path) => _assetsManager.GetRefCount(path);

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="path">资源地址</param>
        public void Release(string path) => _assetsManager.Release(path);

        /// <summary>
        /// 释放全部资源
        /// </summary>
        public void ReleaseAll() => _assetsManager.ReleaseAll();

        /// <summary>
        /// 主动清理未使用的资源（包括之前被 Release 的 GameObject预制体 或 Component 资源）
        /// </summary>
        public void CleanupUnusedAssets() => _assetsManager.CleanupUnusedAssets();
    }
}
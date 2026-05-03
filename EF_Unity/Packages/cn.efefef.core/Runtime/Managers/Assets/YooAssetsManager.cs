/*
 * ================================================
 * Describe:      YooAsset 资产管理器
 * Author:        Alvin8412
 * CreationTime:  2026-05-01 23:24:08
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-01 23:24:08
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace EasyFramework.Managers.Assets
{
    /// <summary>
    /// YooAsset 资产管理器
    /// </summary>
    public class YooAssetsManager : IAssetsManager
    {
        public AssetsManagerType ManagerType => AssetsManagerType.YooAsset;
        public bool OpenDebug { get; set; }

        private ResourcePackage _package;
        private Dictionary<string, AssetHandle> _assetHandles;

        public async UniTask Initialize()
        {
            await UniTask.CompletedTask;

            if (YooAssets.Initialized)
            {
                _package = YooAssets.GetPackage(EF.Patch.PackageName);
            }
            else
            {
                YooAssets.Initialize();
                _package = YooAssets.CreatePackage("DefaultPackage");
                YooAssets.SetDefaultPackage(_package);
            }

            _assetHandles = new Dictionary<string, AssetHandle>();
        }

        public async UniTask Destroy()
        {
            await ReleaseAll();

            _package = null;
        }

        public T Load<T>(string path) where T : UnityEngine.Object
        {
            if (_assetHandles.TryGetValue(path, out var handleAssets) && handleAssets.IsValid)
                return handleAssets.AssetObject as T;

            var handle = _package.LoadAssetSync<T>(path);
            if (!handle.IsValid || null == handle.AssetObject)
                return null;

            _assetHandles[path] = handle;
            return handle.AssetObject as T;
        }

        public async UniTask<T> LoadAsync<T>(string path) where T : UnityEngine.Object
        {
            if (_assetHandles.TryGetValue(path, out var handleAssets) && handleAssets.IsValid)
                return handleAssets.AssetObject as T;

            var handle = _package.LoadAssetAsync<T>(path);
            await handle.ToUniTask();

            if (!handle.IsValid || null == handle.AssetObject)
                return null;

            _assetHandles[path] = handle;
            return handle.AssetObject as T;
        }

        public async UniTask Release(string path)
        {
            await UniTask.CompletedTask;
            if (_assetHandles.TryGetValue(path, out var handle))
            {
                handle.Release();
                _assetHandles.Remove(path);
            }
        }

        public async UniTask ReleaseAll()
        {
            await UniTask.CompletedTask;
            foreach (var kvp in _assetHandles)
            {
                kvp.Value.Release();
            }

            _assetHandles.Clear();
            _package?.UnloadUnusedAssetsAsync();
        }

        public async UniTask CleanupUnusedAssets()
        {
            await _package.UnloadUnusedAssetsAsync().ToUniTask();
        }
    }
}
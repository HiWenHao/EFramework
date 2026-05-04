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
using UnityEngine;

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

        // 默认包名，可根据项目配置修改
        private const string DefaultPackageName = "DefaultPackage";

        public async UniTask Initialize()
        {
            await UniTask.CompletedTask;

            if (!YooAssets.Initialized)
                YooAssets.Initialize();
            else
                _package = YooAssets.GetPackage(EF.Patch.PackageName);
            
            if (_package == null)
            {
                _package = YooAssets.CreatePackage(DefaultPackageName);
                YooAssets.SetDefaultPackage(_package);
            }

            _assetHandles = new Dictionary<string, AssetHandle>();
        }

        public async UniTask Destroy()
        {
            await ReleaseAll();

            _package = null;
        }

        public T Load<T>(string path) where T : Object
        {
            if (_package == null)
                return null;

            if (_assetHandles.TryGetValue(path, out var handle) && handle.IsValid)
                return handle.AssetObject as T;

            var newHandle = _package.LoadAssetSync<T>(path);
            if (!newHandle.IsValid || newHandle.AssetObject == null)
                return null;

            _assetHandles[path] = newHandle;
            return newHandle.AssetObject as T;
        }

        public async UniTask<T> LoadAsync<T>(string path) where T : Object
        {
            if (_package == null)
                return null;

            if (_assetHandles.TryGetValue(path, out var handle) && handle.IsValid)
                return handle.AssetObject as T;

            var newHandle = _package.LoadAssetAsync<T>(path);
            await newHandle.ToUniTask();

            if (!newHandle.IsValid || newHandle.AssetObject == null)
                return null;

            _assetHandles[path] = newHandle;
            return newHandle.AssetObject as T;
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
            if (_package != null)
                await _package.UnloadUnusedAssetsAsync().ToUniTask();
        }

        public async UniTask CleanupUnusedAssets()
        {
            if (_package != null)
                await _package.UnloadUnusedAssetsAsync().ToUniTask();
        }
    }
}
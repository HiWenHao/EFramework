/*
 * ================================================
 * Describe:      默认资产管理器 - 使用Resources
 * Author:        Alvin8412
 * CreationTime:  2026-05-01 21:26:21
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-01 21:26:21
 * ScriptVersion: 0.2
 * ===============================================
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Systems.Assets
{
    /// <summary>
    ///	默认资产管理器 - 使用Resources
    /// </summary>
    public class DefaultAssetsSystem : IAssetsSystem
    {
        public AssetsSystemType SystemType => AssetsSystemType.Default;
        public bool OpenDebug { get; set; }

        private Dictionary<string, Object> _assetsDictionary;

        public async UniTask Initialize()
        {
            await UniTask.CompletedTask;
            _assetsDictionary = new Dictionary<string, Object>();
        }

        public async UniTask Destroy()
        {
            await ReleaseAll();
            await CleanupUnusedAssets();
            _assetsDictionary = null;
        }

        public T Load<T>(string path) where T : Object
        {
            if (_assetsDictionary.TryGetValue(path, out var assetsObject))
                return assetsObject as T;

            T asset = Resources.Load<T>(path);
            if (asset == null)
                return null;

            _assetsDictionary[path] = asset;
            return asset;
        }

        public async UniTask<T> LoadAsync<T>(string path) where T : Object
        {
            if (_assetsDictionary.TryGetValue(path, out var existing))
                return existing as T;

            var asyncOp = Resources.LoadAsync<T>(path);
            await asyncOp;

            if (asyncOp.asset == null)
                return null;

            _assetsDictionary[path] = asyncOp.asset;
            return asyncOp.asset as T;
        }

        public async UniTask Release(string path)
        {
            await UniTask.CompletedTask;
            ReleaseHelper(path);
            _assetsDictionary.Remove(path);
        }

        public async UniTask ReleaseAll()
        {
            await UniTask.CompletedTask;
            if (_assetsDictionary == null)
                return;

            foreach (var kvp in _assetsDictionary)
            {
                ReleaseHelper(kvp.Key);
            }

            _assetsDictionary.Clear();
        }

        public async UniTask CleanupUnusedAssets()
        {
            await UniTask.CompletedTask;
            await Resources.UnloadUnusedAssets();
        }

        //  释放助手
        private void ReleaseHelper(string path)
        {
            if (!_assetsDictionary.TryGetValue(path, out var assetsObject))
                return;

            if (assetsObject is GameObject or Component)
                return;

            Resources.UnloadAsset(assetsObject);
        }
    }
}
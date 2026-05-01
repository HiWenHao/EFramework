/*
 * ================================================
 * Describe:      默认资产管理器 - 使用Resources
 * Author:        Alvin8412
 * CreationTime:  2026-05-01 21:26:21
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-01 21:26:21
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Managers.Assets
{
    /// <summary>
    ///	默认资产管理器 - 使用Resources
    /// </summary>
    public class DefaultAssetsManager : IAssetsManager
    {
        private Dictionary<string, AssetsObject> _assetsDictionary;
        
        public AssetsManagerType ManagerType => AssetsManagerType.Default;
        public void Initialize()
        {
            _assetsDictionary = new Dictionary<string, AssetsObject>();
        }

        public void Destroy()
        {
            ReleaseAll();
            CleanupUnusedAssets();
            _assetsDictionary = null;
        }

        public T Load<T>(string path) where T : Object
        {
            if (_assetsDictionary.TryGetValue(path, out var assetsObject))
            {
                assetsObject.RefCount++;
                return assetsObject.Asset as T;
            }

            T asset = Resources.Load<T>(path);
            if (asset == null)
                return null;

            _assetsDictionary[path] = new AssetsObject()
            {
                RefCount = 1,
                Asset = asset,
                IsReleased = false
            };

            return asset;
        }

        public async UniTask<T> LoadAsync<T>(string path) where T : Object
        {
            if (_assetsDictionary.TryGetValue(path, out var existing))
            {
                existing.RefCount++;
                return existing.Asset as T;
            }

            var asyncOp = Resources.LoadAsync<T>(path);
            await asyncOp;

            if (asyncOp.asset == null)
                return null;

            _assetsDictionary[path] = new AssetsObject()
            {
                RefCount = 1,
                IsReleased = false,
                Asset = asyncOp.asset,
            };
            return asyncOp.asset as T;
        }

        public int GetRefCount(string path)
        {
            return _assetsDictionary.TryGetValue(path, out var assetsObject) ? assetsObject.RefCount : 0;
        }

        public void Release(string path)
        {
            if (ReleaseHelper(path))
                _assetsDictionary.Remove(path);
        }

        public void ReleaseAll()
        {
            if (_assetsDictionary == null)
                return;

            foreach (var kvp in _assetsDictionary)
            {
                ReleaseHelper(kvp.Key);
            }

            _assetsDictionary.Clear();
        }

        public void CleanupUnusedAssets()
        {
            Resources.UnloadUnusedAssets();
        }

        //  释放助手
        private bool ReleaseHelper(string path)
        {
            if (!_assetsDictionary.TryGetValue(path, out var assetsObject))
                return false;

            assetsObject.RefCount--;
            if (assetsObject.RefCount > 0 || assetsObject.IsReleased)
                return false;
            assetsObject.IsReleased = true;

            if (assetsObject.Asset is not (GameObject or Component))
                Resources.UnloadAsset(assetsObject.Asset);
            
            return true;
        }
    }
}
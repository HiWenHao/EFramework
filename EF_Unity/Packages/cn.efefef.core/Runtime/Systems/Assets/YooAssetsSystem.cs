/*
 * ================================================
 * Describe:      YooAsset 资产管理器
 * Author:        Alvin8412
 * CreationTime:  2026-05-01 23:24:08
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-29 16:20:00
 * ScriptVersion: 0.2
 * ================================================
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace EasyFramework.Systems.Assets
{
    /// <summary>
    /// YooAsset 资产管理器
    /// <para>YooAsset asset manager</para>
    /// </summary>
    public class YooAssetsSystem : IAssetsSystem
    {
        public AssetsSystemType SystemType => AssetsSystemType.YooAsset;
        public bool OpenDebug { get; set; }

        // 当前操作的资源包
        private ResourcePackage _package;
        // 资源句柄缓存（key = 资源路径）
        private Dictionary<string, AssetHandle> _assetHandles;

        // 默认包名，可根据项目配置修改
        private const string DefaultPackageName = "DefaultPackage";

        public async UniTask Initialize()
        {
            await UniTask.CompletedTask;

            // 确保 YooAssets 已初始化（通常 PatchManager.Init 已处理）
            if (!YooAssets.Initialized)
                YooAssets.Initialize();

            // 获取或创建默认包
            _package = YooAssets.TryGetPackage(DefaultPackageName);
            if (_package == null)
            {
                _package = YooAssets.CreatePackage(DefaultPackageName);
                YooAssets.SetDefaultPackage(_package);
            }

            _assetHandles = new Dictionary<string, AssetHandle>(128);
            Log("YooAssetsSystem initialized.");
        }

        public async UniTask Destroy()
        {
            await ReleaseAll();
            _package = null;
            Log("YooAssetsSystem destroyed.");
        }

        public T Load<T>(string path) where T : Object
        {
            if (_package == null)
            {
                Error("Package is null, has Initialize() been called?");
                return null;
            }

            // 命中缓存则直接返回
            if (_assetHandles.TryGetValue(path, out var handle) && handle.IsValid)
                return handle.AssetObject as T;

            // 同步加载
            var newHandle = _package.LoadAssetSync<T>(path);
            if (!newHandle.IsValid || newHandle.AssetObject == null)
            {
                Error($"Failed to load: {path}");
                return null;
            }

            _assetHandles[path] = newHandle;
            Log($"Loaded: {path}");
            return newHandle.AssetObject as T;
        }

        public async UniTask<T> LoadAsync<T>(string path) where T : Object
        {
            if (_package == null)
            {
                Error("Package is null, has Initialize() been called?");
                return null;
            }

            // 命中缓存则直接返回
            if (_assetHandles.TryGetValue(path, out var handle) && handle.IsValid)
                return handle.AssetObject as T;

            // 异步加载
            var newHandle = _package.LoadAssetAsync<T>(path);
            await newHandle.ToUniTask();

            if (!newHandle.IsValid || newHandle.AssetObject == null)
            {
                Error($"Failed to load async: {path}");
                return null;
            }

            _assetHandles[path] = newHandle;
            Log($"Loaded async: {path}");
            return newHandle.AssetObject as T;
        }

        public async UniTask Release(string path)
        {
            await UniTask.CompletedTask;
            if (_assetHandles.TryGetValue(path, out var handle))
            {
                handle.Release();
                _assetHandles.Remove(path);
                Log($"Released: {path}");
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
            Log("All handles released.");
        }

        public async UniTask CleanupUnusedAssets()
        {
            if (_package != null)
                await _package.UnloadUnusedAssetsAsync().ToUniTask();
            Log("Unused assets cleaned up.");
        }

        #region Utils

        private void Log(string msg)
        {
            if (OpenDebug) D.Log($"[ YooAssetsSystem ] {msg}");
        }
        private void Error(string msg) => D.Error($"[ YooAssetsSystem ] {msg}");

        #endregion
    }
}

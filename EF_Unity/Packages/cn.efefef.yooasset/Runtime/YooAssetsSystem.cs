/*
 * ================================================
 * Describe:      YooAsset 资产管理器 —— 集成 PatchManager 的分包加载实现
 * Author:        Alvin8412
 * CreationTime:  2026-05-29 16:20:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-29 16:20:00
 * ScriptVersion: 0.1
 * ================================================
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework.Systems.Assets;
using UnityEngine;
using YooAsset;

namespace EasyFramework.Managers.Assets
{
    /// <summary>
    /// YooAsset 资产管理器
    /// <para>YooAsset asset manager — integrates with PatchManager for sub-package loading</para>
    /// </summary>
    public class YooAssetsSystem : IAssetsSystem
    {
        public AssetsSystemType SystemType => AssetsSystemType.YooAsset;
        public bool OpenDebug { get; set; }

        private Dictionary<string, AssetHandle> _assetHandles;        // 资源句柄缓存（key = "包名|资源路径" 或纯路径）

        public async UniTask Initialize()
        {
            await UniTask.CompletedTask;

            // 确保 YooAssets 已初始化（PatchManager.Init 会先于此处执行）
            _assetHandles = new Dictionary<string, AssetHandle>();

            Log("YooAssetsSystem initialized.");
        }

        public async UniTask Destroy()
        {
            await ReleaseAll();
            _assetHandles = null;
            Log("YooAssetsSystem destroyed.");
        }

        #region 核心加载

        /// <summary>
        /// 同步加载资源（使用默认包）
        /// <para>Synchronously load an asset from the default package</para>
        /// </summary>
        public T Load<T>(string path) where T : Object
        {
            var pkg = GetDefaultPackage();
            if (pkg == null) return null;

            return LoadFromPackageInternal<T>(pkg, path);
        }

        /// <summary>
        /// 异步加载资源（使用默认包）
        /// <para>Asynchronously load an asset from the default package</para>
        /// </summary>
        public async UniTask<T> LoadAsync<T>(string path) where T : Object
        {
            var pkg = GetDefaultPackage();
            if (pkg == null) return null;

            return await LoadFromPackageAsyncInternal<T>(pkg, path);
        }

        #endregion

        #region 按包名加载（分包场景）

        /// <summary>
        /// 从指定包同步加载资源
        /// <para>Synchronously load an asset from a specific sub-package</para>
        /// </summary>
        /// <param name="packageName">包名<para>Package name</para></param>
        /// <param name="path">资源地址<para>Asset path</para></param>
        public T LoadFromPackage<T>(string packageName, string path) where T : Object
        {
            var pkg = PatchManager.Instance.GetPackage(packageName);
            if (pkg == null)
            {
                Error($"Package not found: {packageName}");
                return null;
            }

            return LoadFromPackageInternal<T>(pkg, path);
        }

        /// <summary>
        /// 从指定包异步加载资源
        /// <para>Asynchronously load an asset from a specific sub-package</para>
        /// </summary>
        /// <param name="packageName">包名<para>Package name</para></param>
        /// <param name="path">资源地址<para>Asset path</para></param>
        public async UniTask<T> LoadAsyncFromPackage<T>(string packageName, string path) where T : Object
        {
            var pkg = PatchManager.Instance.GetPackage(packageName);
            if (pkg == null)
            {
                Error($"Package not found: {packageName}");
                return null;
            }

            return await LoadFromPackageAsyncInternal<T>(pkg, path);
        }

        #endregion

        #region 释放

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
            var pkg = GetDefaultPackage();
            if (pkg != null)
                await pkg.UnloadUnusedAssetsAsync().ToUniTask();
        }

        /// <summary>
        /// 清理指定包的未使用资源
        /// <para>Unload unused assets from a specific package</para>
        /// </summary>
        public async UniTask CleanupUnusedAssetsFromPackage(string packageName)
        {
            var pkg = PatchManager.Instance.GetPackage(packageName);
            if (pkg != null)
                await pkg.UnloadUnusedAssetsAsync().ToUniTask();
        }

        /// <summary>
        /// 清理所有已注册包的未使用资源
        /// <para>Unload unused assets from all registered packages</para>
        /// </summary>
        public async UniTask CleanupAllPackages()
        {
            var registeredConfigs = PatchManager.Instance.GetRegisteredConfigs();
            if (registeredConfigs == null) return;

            foreach (var cfg in registeredConfigs)
            {
                var pkg = PatchManager.Instance.GetPackage(cfg.PackageName);
                if (pkg != null)
                    await pkg.UnloadUnusedAssetsAsync().ToUniTask();
            }
        }

        #endregion

        #region 内部实现

        // 获取默认资源包
        private ResourcePackage GetDefaultPackage()
        {
            // 1. 优先使用 PatchManager 中注册的默认包
            var registered = PatchManager.Instance.GetRegisteredConfigs();
            if (registered != null && registered.Count > 0)
            {
                // 找第一个核心包作为默认
                foreach (var cfg in registered)
                {
                    if (cfg.IsEssential)
                        return PatchManager.Instance.GetPackage(cfg.PackageName);
                }
                // 没有核心包则取第一个
                return PatchManager.Instance.GetPackage(registered[0].PackageName);
            }

            // 2. 回退到 PatchManager 单包模式
            var pkg = PatchManager.Instance.GetPackage("DefaultPackage");
            if (pkg != null) return pkg;

            // 3. 最终兜底：直接用 YooAssets 查或创建
            pkg = YooAssets.TryGetPackage("DefaultPackage");
            if (pkg == null)
            {
                pkg = YooAssets.CreatePackage("DefaultPackage");
                YooAssets.SetDefaultPackage(pkg);
            }
            return pkg;
        }

        // 同步加载（包级）
        private T LoadFromPackageInternal<T>(ResourcePackage pkg, string path) where T : Object
        {
            // 检查缓存
            if (_assetHandles.TryGetValue(path, out var handle) && handle.IsValid)
                return handle.AssetObject as T;

            // 加载新资源
            var newHandle = pkg.LoadAssetSync<T>(path);
            if (!newHandle.IsValid || newHandle.AssetObject == null)
            {
                Error($"Failed to load: {path} from package: {pkg.PackageName}");
                return null;
            }

            _assetHandles[path] = newHandle;
            Log($"Loaded: {path} [{pkg.PackageName}]");
            return newHandle.AssetObject as T;
        }

        // 异步加载（包级）
        private async UniTask<T> LoadFromPackageAsyncInternal<T>(ResourcePackage pkg, string path) where T : Object
        {
            // 检查缓存
            if (_assetHandles.TryGetValue(path, out var handle) && handle.IsValid)
                return handle.AssetObject as T;

            // 加载新资源
            var newHandle = pkg.LoadAssetAsync<T>(path);
            await newHandle.ToUniTask();

            if (!newHandle.IsValid || newHandle.AssetObject == null)
            {
                Error($"Failed to load async: {path} from package: {pkg.PackageName}");
                return null;
            }

            _assetHandles[path] = newHandle;
            Log($"Loaded async: {path} [{pkg.PackageName}]");
            return newHandle.AssetObject as T;
        }

        #endregion

        #region Utils

        private void Log(string msg)
        {
            if (OpenDebug) D.Log($"[ YooAssetsSystem ] {msg}");
        }
        private void Error(string msg) => D.Error($"[ YooAssetsSystem ] {msg}");

        #endregion
    }
}

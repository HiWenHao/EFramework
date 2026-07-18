/*
 * ================================================
 * Describe:      YooAsset 资产管理器 —— 集成 PatchManager 的分包加载实现
 * Author:        Alvin8412
 * CreationTime:  2026-05-29 16:20:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-30 17:34:00
 * ScriptVersion: 0.2
 * ================================================
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace EasyFramework.Managers.Assets
{
    /// <summary>
    /// YooAsset 资产管理器 —— 实现 <see cref="IAssetsSystem"/>，支持分包加载
    /// <para>YooAsset asset manager — implements <see cref="IAssetsSystem"/> with sub-package loading support</para>
    /// </summary>
    public class YooAssetsSystem : IAssetsSystem
    {
        public bool OpenDebug { get; set; }
        public AssetsSystemType SystemType => AssetsSystemType.YooAsset;

        /// <summary>资源句柄缓存（key = 资源路径）<para>Asset handle cache (key = asset path)</para></summary>
        private Dictionary<string, AssetHandle> _assetHandles;

        /// <summary>
        /// 初始化管理器 —— PatchManager.Init 已先行初始化 YooAssets，此处仅创建句柄缓存
        /// <para>Initialize the manager — PatchManager.Init already initialized YooAssets; here we only create the handle cache</para>
        /// </summary>
        public async UniTask Initialize()
        {
            await UniTask.CompletedTask;
            _assetHandles = new Dictionary<string, AssetHandle>();
            Log("YooAssetsSystem initialized.");
        }

        /// <summary>
        /// 销毁管理器 —— 释放所有句柄并清空缓存
        /// <para>Destroy the manager — release all handles and clear the cache</para>
        /// </summary>
        public async UniTask Destroy()
        {
            await ReleaseAll();
            _assetHandles = null;
            Log("YooAssetsSystem destroyed.");
        }

        /// <summary>
        /// 从默认包同步加载资源
        /// <para>Synchronously load an asset from the default package</para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="path">资源路径<para>Asset path</para></param>
        /// <returns>加载成功返回资源对象，失败返回 null<para>The loaded asset, or null on failure</para></returns>
        public T Load<T>(string path) where T : Object
        {
            var pkg = GetDefaultPackage();
            if (pkg == null) return null;
            return LoadInternal<T>(pkg, path);
        }

        /// <summary>
        /// 从默认包异步加载资源
        /// <para>Asynchronously load an asset from the default package</para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="path">资源路径<para>Asset path</para></param>
        /// <returns>加载成功返回资源对象，失败返回 null<para>The loaded asset, or null on failure</para></returns>
        public async UniTask<T> LoadAsync<T>(string path) where T : Object
        {
            var pkg = GetDefaultPackage();
            if (pkg == null) return null;
            return await LoadAsyncInternal<T>(pkg, path);
        }

        /// <summary>
        /// 从指定资源包同步加载资源
        /// <para>Synchronously load an asset from a named resource package</para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="packageName">包名（需已通过 PatchManager.RegisterSubPackages 注册）<para>Package name (must be registered via PatchManager.RegisterSubPackages)</para></param>
        /// <param name="path">资源路径<para>Asset path</para></param>
        /// <returns>加载成功返回资源对象，失败返回 null<para>The loaded asset, or null on failure</para></returns>
        public T LoadFromPackage<T>(string packageName, string path) where T : Object
        {
            var pkg = PatchManager.Instance.GetPackage(packageName);
            if (pkg == null)
            {
                Error($"Package not found: {packageName}");
                return null;
            }

            return LoadInternal<T>(pkg, path);
        }

        /// <summary>
        /// 从指定资源包异步加载资源
        /// <para>Asynchronously load an asset from a named resource package</para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="packageName">包名（需已通过 PatchManager.RegisterSubPackages 注册）<para>Package name (must be registered via PatchManager.RegisterSubPackages)</para></param>
        /// <param name="path">资源路径<para>Asset path</para></param>
        /// <returns>加载成功返回资源对象，失败返回 null<para>The loaded asset, or null on failure</para></returns>
        public async UniTask<T> LoadAsyncFromPackage<T>(string packageName, string path) where T : Object
        {
            var pkg = PatchManager.Instance.GetPackage(packageName);
            if (pkg == null)
            {
                Error($"Package not found: {packageName}");
                return null;
            }

            return await LoadAsyncInternal<T>(pkg, path);
        }

        /// <summary>
        /// 释放指定路径的资源句柄
        /// <para>Release the asset handle at the given path</para>
        /// </summary>
        /// <param name="path">资源路径<para>Asset path</para></param>
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

        /// <summary>
        /// 释放所有已缓存的资源句柄
        /// <para>Release all cached asset handles</para>
        /// </summary>
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

        /// <summary>
        /// 清理默认包的未使用资源
        /// <para>Unload unused assets from the default package</para>
        /// </summary>
        public async UniTask CleanupUnusedAssets()
        {
            var pkg = GetDefaultPackage();
            if (pkg != null)
                await pkg.UnloadUnusedAssetsAsync().ToUniTask();
            Log("Default package unused assets cleaned up.");
        }

        /// <summary>
        /// 清理指定包的未使用资源
        /// <para>Unload unused assets from a specific sub-package</para>
        /// </summary>
        /// <param name="packageName">包名<para>Package name</para></param>
        public async UniTask CleanupUnusedAssetsFromPackage(string packageName)
        {
            var pkg = PatchManager.Instance.GetPackage(packageName);
            if (pkg != null)
                await pkg.UnloadUnusedAssetsAsync().ToUniTask();
            Log($"Unused assets cleaned up from package: {packageName}");
        }

        /// <summary>
        /// 清理所有已注册包的未使用资源
        /// <para>Unload unused assets from all registered sub-packages</para>
        /// </summary>
        public async UniTask CleanupAllPackages()
        {
            var registeredConfigs = PatchManager.Instance.GetRegisteredConfigs();
            if (registeredConfigs == null) return;

            foreach (var cfg in registeredConfigs)
            {
                var pkg = PatchManager.Instance.GetPackage(cfg.PackageName);
                if (pkg == null) continue;
                await pkg.UnloadUnusedAssetsAsync().ToUniTask();
            }

            Log("All packages unused assets cleaned up.");
        }

        #region 私有函数

        /// <summary>
        /// 获取默认资源包 —— 路由优先级：第一个核心包 → 第一个注册包 → DefaultPackage → 兜底创建
        /// <para>Resolve the default resource package — priority: first essential package → first registered package → DefaultPackage → fallback create</para>
        /// </summary>
        private ResourcePackage GetDefaultPackage()
        {
            // 1. 从 PatchManager 已注册包中查找第一个核心包
            var registered = PatchManager.Instance.GetRegisteredConfigs();
            if (registered != null && registered.Count > 0)
            {
                foreach (var cfg in registered)
                {
                    if (!cfg.IsEssential) continue;
                    return PatchManager.Instance.GetPackage(cfg.PackageName);
                }

                return PatchManager.Instance.GetPackage(registered[0].PackageName);
            }

            var pkg = PatchManager.Instance.GetPackage("DefaultPackage");
            if (pkg != null) return pkg;

            pkg = YooAssets.TryGetPackage("DefaultPackage");
            if (pkg == null)
            {
                pkg = YooAssets.CreatePackage("DefaultPackage");
                YooAssets.SetDefaultPackage(pkg);
            }
            return pkg;
        }

        /// <summary>
        /// 从指定 ResourcePackage 同步加载资源（命中缓存则直接返回）
        /// <para>Synchronously load an asset from a specific ResourcePackage (returns cached handle if available)</para>
        /// </summary>
        private T LoadInternal<T>(ResourcePackage pkg, string path) where T : Object
        {
            if (_assetHandles.TryGetValue(path, out var handle) && handle.IsValid)
                return handle.AssetObject as T;

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

        /// <summary>
        /// 从指定 ResourcePackage 异步加载资源（命中缓存则直接返回）
        /// <para>Asynchronously load an asset from a specific ResourcePackage (returns cached handle if available)</para>
        /// </summary>
        private async UniTask<T> LoadAsyncInternal<T>(ResourcePackage pkg, string path) where T : Object
        {
            if (_assetHandles.TryGetValue(path, out var handle) && handle.IsValid)
                return handle.AssetObject as T;

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

        private void Log(string msg)
        {
            if (OpenDebug) D.Log($"[ YooAssetsSystem ] {msg}");
        }
        private void Error(string msg) => D.Error($"[ YooAssetsSystem ] {msg}");

        #endregion
    }
}
/*
 * ================================================
 * Describe:      默认资产管理器 —— 基于 Resources API
 * Author:        Alvin8412
 * CreationTime:  2026-05-01 21:26:21
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-30 17:34:00
 * ScriptVersion: 0.3
 * ================================================
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Managers.Assets
{
    /// <summary>
    /// 默认资产管理器 —— 基于 Unity Resources API
    /// <para>Default asset manager implementation using Unity's Resources API</para>
    /// </summary>
    public class DefaultAssetsSystem : IAssetsSystem
    {
        public AssetsSystemType SystemType => AssetsSystemType.Default;
        public bool OpenDebug { get; set; }

        /// <summary>已加载资源缓存（key = 资源路径）<para>Loaded asset cache (key = asset path)</para></summary>
        private Dictionary<string, Object> _assetsDictionary;

        /// <summary>
        /// 初始化管理器，创建资源缓存字典
        /// <para>Initialise the manager and create the asset cache dictionary</para>
        /// </summary>
        public async UniTask Initialize()
        {
            await UniTask.CompletedTask;
            _assetsDictionary = new Dictionary<string, Object>();
            Log("DefaultAssetsSystem initialized.");
        }

        /// <summary>
        /// 销毁管理器：释放全部资源 + 清理未使用对象 + 清空缓存
        /// <para>Destroy the manager: release all assets, clean up unused objects, and clear the cache</para>
        /// </summary>
        public async UniTask Destroy()
        {
            await ReleaseAll();
            await CleanupUnusedAssets();
            _assetsDictionary = null;
            Log("DefaultAssetsSystem destroyed.");
        }

        /// <summary>
        /// 从 Resources 同步加载资源（命中缓存则直接返回）
        /// <para>Synchronously load an asset from Resources (returns cached instance if available)</para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="path">Resources 路径（不含后缀）<para>Resources path (without extension)</para></param>
        /// <returns>加载成功返回资源对象，失败返回 null<para>The loaded asset, or null on failure</para></returns>
        public T Load<T>(string path) where T : Object
        {
            if (_assetsDictionary.TryGetValue(path, out var assetsObject))
                return assetsObject as T;

            T asset = Resources.Load<T>(path);
            if (asset == null)
            {
                Error($"Failed to load: {path}");
                return null;
            }

            _assetsDictionary[path] = asset;
            Log($"Loaded: {path}");
            return asset;
        }

        /// <summary>
        /// 从 Resources 异步加载资源（命中缓存则直接返回）
        /// <para>Asynchronously load an asset from Resources (returns cached instance if available)</para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="path">Resources 路径（不含后缀）<para>Resources path (without extension)</para></param>
        /// <returns>加载成功返回资源对象，失败返回 null<para>The loaded asset, or null on failure</para></returns>
        public async UniTask<T> LoadAsync<T>(string path) where T : Object
        {
            if (_assetsDictionary.TryGetValue(path, out var existing))
                return existing as T;

            var asyncOp = Resources.LoadAsync<T>(path);
            await asyncOp;

            if (asyncOp.asset == null)
            {
                Error($"Failed to load async: {path}");
                return null;
            }

            _assetsDictionary[path] = asyncOp.asset;
            Log($"Loaded async: {path}");
            return asyncOp.asset as T;
        }

        /// <summary>
        /// 从指定包同步加载 —— Resources 不支持分包概念，直接转发到 <see cref="Load{T}"/>
        /// <para>Load from a named package — Resources has no sub-package concept, delegates to <see cref="Load{T}"/></para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="packageName">包名（忽略）<para>Package name (ignored)</para></param>
        /// <param name="path">Resources 路径<para>Resources path</para></param>
        /// <returns>加载成功返回资源对象，失败返回 null<para>The loaded asset, or null on failure</para></returns>
        public T LoadFromPackage<T>(string packageName, string path) where T : Object => Load<T>(path);

        /// <summary>
        /// 从指定包异步加载 —— Resources 不支持分包概念，直接转发到 <see cref="LoadAsync{T}"/>
        /// <para>Load async from a named package — Resources has no sub-package concept, delegates to <see cref="LoadAsync{T}"/></para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="packageName">包名（忽略）<para>Package name (ignored)</para></param>
        /// <param name="path">Resources 路径<para>Resources path</para></param>
        /// <returns>加载成功返回资源对象，失败返回 null<para>The loaded asset, or null on failure</para></returns>
        public async UniTask<T> LoadAsyncFromPackage<T>(string packageName, string path) where T : Object =>
            await LoadAsync<T>(path);

        /// <summary>
        /// 释放指定路径的资源（GameObject / Component 不调用 UnloadAsset，避免误删实例）
        /// <para>Release the asset at the given path (GameObject / Component are NOT passed to UnloadAsset to avoid destroying instances)</para>
        /// </summary>
        /// <param name="path">资源路径<para>Asset path</para></param>
        public async UniTask Release(string path)
        {
            await UniTask.CompletedTask;
            ReleaseHelper(path);
            _assetsDictionary.Remove(path);
            Log($"Released: {path}");
        }

        /// <summary>
        /// 释放所有已加载资源，并清空缓存
        /// <para>Release all loaded assets and clear the cache</para>
        /// </summary>
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
            Log("All assets released.");
        }

        /// <summary>
        /// 调用 Resources.UnloadUnusedAssets() 异步清理未使用资源
        /// <para>Call Resources.UnloadUnusedAssets() to asynchronously clean up unused assets</para>
        /// </summary>
        public async UniTask CleanupUnusedAssets()
        {
            await Resources.UnloadUnusedAssets();
            Log("Unused assets cleaned up.");
        }

        /// <summary>
        /// 释放助手：跳过 GameObject 和 Component（避免销毁场景中的实例），其余调用 Resources.UnloadAsset
        /// <para>Release helper: skips GameObject and Component to avoid destroying scene instances; calls Resources.UnloadAsset for everything else</para>
        /// </summary>
        /// <param name="path">资源路径<para>Asset path</para></param>
        private void ReleaseHelper(string path)
        {
            if (!_assetsDictionary.TryGetValue(path, out var assetsObject))
                return;

            // GameObject / Component 实例可能仍在场景中引用，跳过 UnloadAsset
            if (assetsObject is GameObject or Component)
                return;

            Resources.UnloadAsset(assetsObject);
        }

        private void Log(string msg)
        {
            if (OpenDebug) D.Log($"[ DefaultAssetsSystem ] {msg}");
        }

        private void Error(string msg) => D.Error($"[ DefaultAssetsSystem ] {msg}");
    }
}
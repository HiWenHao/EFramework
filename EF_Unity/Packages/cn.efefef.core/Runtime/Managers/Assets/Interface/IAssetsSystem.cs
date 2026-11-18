/*
 * ================================================
 * Describe:      资源管理器统一接口 —— 屏蔽 Default / YooAsset 底层差异
 * Author:        Alvin8412
 * CreationTime:  2026-05-01 21:03:43
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-11-18 15:32:00
 * ScriptVersion: 0.3
 * ===============================================
 */

using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Managers.Assets
{
    /// <summary>
    /// 资源管理器统一接口
    /// <para>Unified asset manager interface — abstracts away the underlying Default / YooAsset implementation</para>
    /// </summary>
    public interface IAssetsSystem
    {
        /// <summary>
        /// 是否开启调试日志
        /// <para>Enable debug logging for load / release operations</para>
        /// </summary>
        bool OpenDebug { get; set; }

        /// <summary>
        /// 当前管理器类型
        /// <para>Type of the current asset system implementation</para>
        /// </summary>
        AssetsSystemType SystemType { get; }

        /// <summary>
        /// 初始化管理器
        /// <para>Initialize the asset system</para>
        /// </summary>
        UniTask Initialize();

        /// <summary>
        /// 销毁管理器，释放所有已加载资源
        /// <para>Destroy the asset system and release all loaded assets</para>
        /// </summary>
        UniTask Destroy();

        /// <summary>
        /// 从默认包同步加载资源
        /// <para>Synchronously load an asset from the default package</para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="path">资源路径<para>Asset path</para></param>
        /// <returns>加载成功返回资源对象，失败返回 null<para>The loaded asset, or null on failure</para></returns>
        T Load<T>(string path) where T : Object;

        /// <summary>
        /// 从默认包异步加载资源
        /// <para>Asynchronously load an asset from the default package</para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="path">资源路径<para>Asset path</para></param>
        /// <param name="token">取消令牌<para>Cancellation token</para></param>
        /// <returns>加载成功返回资源对象，失败返回 null<para>The loaded asset, or null on failure</para></returns>
        UniTask<T> LoadAsync<T>(string path, CancellationToken token = default) where T : Object;

        /// <summary>
        /// 从指定资源包同步加载资源
        /// <para>Synchronously load an asset from a named package</para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="packageName">包名<para>Package name</para></param>
        /// <param name="path">资源路径<para>Asset path</para></param>
        /// <returns>加载成功返回资源对象，失败返回 null<para>The loaded asset, or null on failure</para></returns>
        T LoadFromPackage<T>(string packageName, string path) where T : Object;

        /// <summary>
        /// 从指定资源包异步加载资源
        /// <para>Asynchronously load an asset from a named package</para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="packageName">包名<para>Package name</para></param>
        /// <param name="path">资源路径<para>Asset path</para></param>
        /// <param name="token">取消令牌<para>Cancellation token</para></param>
        /// <returns>加载成功返回资源对象，失败返回 null<para>The loaded asset, or null on failure</para></returns>
        UniTask<T> LoadAsyncFromPackage<T>(string packageName, string path, CancellationToken token = default)
            where T : Object;

        /// <summary>
        /// 释放指定资源（引用归零时由门面调用）
        /// <para>Release an asset (called by the facade when ref-count reaches zero)</para>
        /// </summary>
        /// <param name="packageName">包名，null 表示默认包<para>Package name, null for default</para></param>
        /// <param name="path">资源路径<para>Asset path</para></param>
        UniTask Release(string packageName, string path);

        /// <summary>
        /// 释放所有已加载资源
        /// <para>Release all loaded assets</para>
        /// </summary>
        UniTask ReleaseAll();

        /// <summary>
        /// 主动清理未使用的资源
        /// <para>Proactively unload unused assets</para>
        /// </summary>
        UniTask CleanupUnusedAssets();
    }
}
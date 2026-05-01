/*
 * ================================================
 * Describe:      资源管理器接口
 * Author:        Alvin8412
 * CreationTime:  2026-05-01 21:03:43
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-01 21:03:43
 * ScriptVersion: 0.1
 * ===============================================
 */

using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Managers.Assets
{
    /// <summary>
    /// 资源管理器接口
    /// </summary>
    public interface IAssetsManager
    {
        /// <summary>
        /// 资源管理器类型
        /// </summary>
        AssetsManagerType ManagerType { get; }

        /// <summary>
        /// 初始化
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// 销毁
        /// </summary>
        void Destroy();
        
        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="path">资源地址</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns>资源</returns>
        T Load<T>(string path) where T : Object;

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源地址</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns>资源</returns>
        UniTask<T> LoadAsync<T>(string path) where T : Object;

        /// <summary>
        /// 获取资源引用计数
        /// </summary>
        /// <param name="path">资源地址</param>
        /// <returns>引用数量</returns>
        int GetRefCount(string path);

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="path">资源地址</param>
        void Release(string path);
        
        /// <summary>
        /// 释放全部资源
        /// </summary>
        void ReleaseAll();

        /// <summary>
        /// 主动清理未使用的资源（包括之前被 Release 的 GameObject预制体 或 Component 资源）
        /// </summary>
        void CleanupUnusedAssets();
    }
}
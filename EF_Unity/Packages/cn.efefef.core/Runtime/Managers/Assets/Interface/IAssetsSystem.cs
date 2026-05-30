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
    public interface IAssetsSystem
    {
        /// <summary>
        /// 资源管理器类型
        /// </summary>
        AssetsSystemType SystemType { get; }

        /// <summary>
        /// 开启日志
        /// </summary>
        bool OpenDebug { get; set; }
        
        /// <summary>
        /// 初始化
        /// </summary>
        UniTask Initialize();
        
        /// <summary>
        /// 销毁
        /// </summary>
        UniTask Destroy();
        
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
        /// 释放资源
        /// </summary>
        /// <param name="path">资源地址</param>
        UniTask Release(string path);
        
        /// <summary>
        /// 释放全部资源
        /// </summary>
        UniTask ReleaseAll();

        /// <summary>
        /// 主动清理未使用的资源（包括之前被 Release 的 GameObject预制体 或 Component 资源）
        /// </summary>
        UniTask CleanupUnusedAssets();
    }
}
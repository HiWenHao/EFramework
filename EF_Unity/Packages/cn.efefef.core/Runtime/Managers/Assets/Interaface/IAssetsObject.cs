/*
 * ================================================
 * Describe:      单一资产接口
 * Author:        Alvin8412
 * CreationTime:  2026-05-01 21:41:12
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-01 21:41:12
 * ScriptVersion: 0.1
 * ===============================================
 */

using UnityEngine;

namespace EasyFramework.Managers.Assets
{
    /// <summary>
    /// 单一资产接口
    /// </summary>
    public interface IAssetsObject
    {
        /// <summary>
        /// 引用数量
        /// </summary>
        int RefCount { get; set; }
        
        /// <summary>
        /// 已经释放
        /// </summary>
        bool IsReleased { get; set; }
        
        /// <summary>
        /// 资产
        /// </summary>
        Object Asset { get; set; }
    }
}
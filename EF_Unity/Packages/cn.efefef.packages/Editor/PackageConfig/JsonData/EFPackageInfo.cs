/*
 * ================================================
 * Describe:     展示单个资源包信息
 * Author:        Alvin8412
 * CreationTime:  2026-04-14 15:41:21
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-29 23:45:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using System;
using UnityEngine;

namespace EasyFramework.Edit.Packages
{
    /// <summary>
    /// 包来源类型
    /// </summary>
    [Serializable]
    public enum EFPackageSource
    {
        /// <summary> 未知 </summary>
        Unknown = 0,
        /// <summary> 本地嵌入式包（在 Packages/ 目录下，可直接编辑源码） </summary>
        Local,
        /// <summary> Git 远端包（通过 Git URL 安装，只读） </summary>
        Git,
        /// <summary> 未安装（服务器上有记录，但本地未安装） </summary>
        NotInstalled,
    }

    /// <summary>
    /// 单个资源包信息
    /// </summary>
    [Serializable]
    public class EFPackageInfo
    {
        /// <summary>
        /// 包名
        /// </summary>
        [SerializeField] public string Name;
        
        /// <summary> 展示名 </summary>
        [SerializeField] public string DisplayName;
        
        /// <summary>
        /// 包来源类型
        /// </summary>
        [SerializeField] public EFPackageSource SourceType = EFPackageSource.Unknown;
        
        /// <summary>
        /// 需要更新
        /// </summary>
        [SerializeField] public bool NeedUpdate;
        
        /// <summary>
        /// 包体描述
        /// </summary>
        [SerializeField] public string Description = "This is a description";
        
        /// <summary>
        /// 服务器版本
        /// </summary>
        [SerializeField] public string ServerVersion = "0.0.1";

        /// <summary>
        /// 当前版本
        /// </summary>
        [SerializeField] public string CurrentVersion = "0.0.1";
        
    }
}
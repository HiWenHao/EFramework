/*
 * ================================================
 * Describe:     展示单个资源包信息
 * Author:        Alvin8412
 * CreationTime:  2026-04-14 15:41:21
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-14 15:41:21
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using UnityEngine;

namespace EasyFramework.Edit.Windows.ConfigPanel
{
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
        
        /// <summary>
        /// 来源于Git
        /// </summary>
        [SerializeField] public bool FromGit;
        
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
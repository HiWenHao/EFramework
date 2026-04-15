/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-04-13 14:49:01
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-13 14:49:01
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Edit.Packages
{
    /// <summary>
    /// 用来管理配置
    /// </summary>
    public class PackageConfig : ScriptableObject
    {
        /// <summary> 自动更新 </summary>
        [SerializeField] [HideInInspector] public bool autoUpdate = true;

        /// <summary> 上次更新时间戳 </summary>
        [SerializeField] [HideInInspector] public long lastUpdateTimestamp;
        
        /// <summary> 全部包名 </summary>
        [SerializeField]  public List<EFPackageInfo> packagesInfo;
    }
}
/*
 * ================================================
 * Describe:      官方包目录 — 提交到远端仓库的干净包列表
 * Author:        Alvin8412
 * CreationTime:  2026-05-30 00:06:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Edit.Packages
{
    /// <summary>
    /// 官方包目录条目
    /// 只有包的基本信息，不含本地配置
    /// </summary>
    [Serializable]
    public class EFPackageCatalogEntry
    {
        [SerializeField] public string name;
        [SerializeField] public string displayName;
        [SerializeField] public string description;
        [SerializeField] public string version;       // 官方版本号
    }

    /// <summary>
    /// 官方包目录（提交到 Git 的版本）
    /// 维护者通过 "Generate Catalog" 生成此文件，开发者通过 "Update All" 拉取此文件
    /// </summary>
    [Serializable]
    public class EFPackageCatalog
    {
        [SerializeField] public long generatedTimestamp;
        [SerializeField] public List<EFPackageCatalogEntry> packages = new();

        public static EFPackageCatalog FromJson(string json) =>
            JsonUtility.FromJson<EFPackageCatalog>(json);
        
        public string ToJson() => JsonUtility.ToJson(this);
    }
}

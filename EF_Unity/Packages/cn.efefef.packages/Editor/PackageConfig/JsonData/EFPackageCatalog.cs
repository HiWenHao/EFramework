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

namespace EasyFramework.Packages
{
    /// <summary>
    /// 官方包目录条目
    /// 只有包的基本信息，不含本地配置
    /// <para>Official package catalog entry</para>
    /// <para>Only basic package information, no local configuration</para>
    /// </summary>
    [Serializable]
    public class EFPackageCatalogEntry
    {
        /// <summary>
        /// 包名
        /// <para>Package name</para>
        /// </summary>
        [SerializeField] public string name;
        /// <summary>
        /// 展示名
        /// <para>Display name</para>
        /// </summary>
        [SerializeField] public string displayName;
        /// <summary>
        /// 包描述
        /// <para>Package description</para>
        /// </summary>
        [SerializeField] public string description;
        /// <summary>
        /// 官方版本号
        /// <para>Official version number</para>
        /// </summary>
        [SerializeField] public string version; // 官方版本号
    }

    /// <summary>
    /// 官方包目录（提交到 Git 的版本）
    /// 维护者通过 "Generate Catalog" 生成此文件，开发者通过 "Update All" 拉取此文件
    /// <para>Official package catalog (the version committed to Git)</para>
    /// <para>Maintainers generate this file via "Generate Catalog"; developers pull it via "Update All"</para>
    /// </summary>
    [Serializable]
    public class EFPackageCatalog
    {
        /// <summary>
        /// 目录生成时间戳
        /// <para>Catalog generation timestamp</para>
        /// </summary>
        [SerializeField] public long generatedTimestamp;
        /// <summary>
        /// 包条目列表
        /// <para>List of package entries</para>
        /// </summary>
        [SerializeField] public List<EFPackageCatalogEntry> packages = new();

        /// <summary>
        /// 从 JSON 反序列化官方包目录
        /// <para>Deserialize the official package catalog from JSON.</para>
        /// </summary>
        /// <param name="json">JSON 文本<para>JSON text.</para></param>
        /// <returns>官方包目录<para>The official package catalog.</para></returns>
        public static EFPackageCatalog FromJson(string json) =>
            JsonUtility.FromJson<EFPackageCatalog>(json);

        /// <summary>
        /// 将官方包目录序列化为 JSON
        /// <para>Serialize the official package catalog to JSON.</para>
        /// </summary>
        /// <returns>JSON 文本<para>JSON text.</para></returns>
        public string ToJson() => JsonUtility.ToJson(this, true);
    }
}
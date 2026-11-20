/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-04-13 14:49:01
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-29 23:45:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyFramework.Edit.Packages
{
    /// <summary>
    /// 服务器类型
    /// <para>Server type</para>
    /// </summary>
    [Serializable]
    public enum ServerType
    {
        /// <summary>
        /// Gitee 平台
        /// <para>Gitee platform</para>
        /// </summary>
        Gitee,
        /// <summary>
        /// GitHub 平台
        /// <para>GitHub platform</para>
        /// </summary>
        GitHub,
        /// <summary>
        /// 本地
        /// <para>Local</para>
        /// </summary>
        Local,
    }

    /// <summary>
    /// 用来管理配置
    /// <para>Used to manage configuration</para>
    /// </summary>
    [Serializable]
    public class PackageConfig
    {
        /// <summary>
        /// 服务地址
        /// <para>Server address</para>
        /// </summary>
        [SerializeField] public ServerType serverType = ServerType.Gitee;

        /// <summary>
        /// git请求令牌
        /// <para>Git request token</para>
        /// </summary>
        [SerializeField] public string token;

        /// <summary>
        /// 上次更新时间戳
        /// <para>Last update timestamp</para>
        /// </summary>
        [SerializeField] public long lastUpdateTimestamp;

        /// <summary>
        /// 全部包名
        /// <para>All package names</para>
        /// </summary>
        [SerializeField] public List<EFPackageInfo> packagesInfo = new();

        #region 统计摘要（运行时自动计算）

        /// <summary>
        /// 总包数
        /// <para>Total package count</para>
        /// </summary>
        [SerializeField] public int totalCount;

        /// <summary>
        /// 本地包数
        /// <para>Local package count</para>
        /// </summary>
        [SerializeField] public int localCount;

        /// <summary>
        /// Git包数
        /// <para>Git package count</para>
        /// </summary>
        [SerializeField] public int gitCount;

        /// <summary>
        /// 未安装包数
        /// <para>Not-installed package count</para>
        /// </summary>
        [SerializeField] public int notInstalledCount;

        /// <summary>
        /// 需要更新的包数
        /// <para>Packages needing update</para>
        /// </summary>
        [SerializeField] public int needUpdateCount;

        #endregion

        /// <summary>
        /// 刷新统计摘要
        /// <para>Refresh the summary statistics</para>
        /// </summary>
        public void RefreshSummary()
        {
            totalCount = packagesInfo.Count;
            localCount = packagesInfo.Count(p => p.SourceType == EFPackageSource.Local);
            gitCount = packagesInfo.Count(p => p.SourceType == EFPackageSource.Git);
            notInstalledCount = packagesInfo.Count(p => p.SourceType == EFPackageSource.NotInstalled);
            needUpdateCount = packagesInfo.Count(p => p.NeedUpdate);
        }

        /// <summary>
        /// 从 JSON 反序列化包配置并刷新统计摘要
        /// <para>Deserialize the package configuration from JSON and refresh the summary.</para>
        /// </summary>
        /// <param name="json">JSON 文本<para>JSON text.</para></param>
        /// <returns>包配置对象<para>The package configuration.</para></returns>
        public static PackageConfig FromJson(string json)
        {
            var config = JsonUtility.FromJson<PackageConfig>(json);
            config.RefreshSummary();
            return config;
        }

        /// <summary>
        /// 刷新统计摘要并将包配置序列化为 JSON
        /// <para>Refresh the summary and serialize the package configuration to JSON.</para>
        /// </summary>
        /// <returns>JSON 文本<para>JSON text.</para></returns>
        public string ToJson()
        {
            RefreshSummary();
            return JsonUtility.ToJson(this, true);
        }
    }
}
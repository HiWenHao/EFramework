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
    /// </summary>
    [Serializable]
    public enum ServerType
    {
        Gitee,
        GitHub,
        Local,
    }

    /// <summary>
    /// 用来管理配置
    /// </summary>
    [Serializable]
    public class PackageConfig
    {
        /// <summary> 服务地址 </summary>
        [SerializeField] public ServerType serverType = ServerType.Gitee;

        /// <summary> git请求令牌 </summary>
        [SerializeField] public string token;

        /// <summary> 上次更新时间戳 </summary>
        [SerializeField] public long lastUpdateTimestamp;

        /// <summary> 全部包名 </summary>
        [SerializeField] public List<EFPackageInfo> packagesInfo = new();

        #region 统计摘要（运行时自动计算）

        /// <summary> 总包数 </summary>
        [SerializeField] public int totalCount;
        /// <summary> 本地包数 </summary>
        [SerializeField] public int localCount;
        /// <summary> Git包数 </summary>
        [SerializeField] public int gitCount;
        /// <summary> 未安装包数 </summary>
        [SerializeField] public int notInstalledCount;
        /// <summary> 需要更新的包数 </summary>
        [SerializeField] public int needUpdateCount;

        #endregion

        /// <summary>
        /// 刷新统计摘要
        /// </summary>
        public void RefreshSummary()
        {
            totalCount = packagesInfo.Count;
            localCount = packagesInfo.Count(p => p.SourceType == EFPackageSource.Local);
            gitCount = packagesInfo.Count(p => p.SourceType == EFPackageSource.Git);
            notInstalledCount = packagesInfo.Count(p => p.SourceType == EFPackageSource.NotInstalled);
            needUpdateCount = packagesInfo.Count(p => p.NeedUpdate);
        }

        public static PackageConfig FromJson(string json)
        {
            var config = JsonUtility.FromJson<PackageConfig>(json);
            config.RefreshSummary();
            return config;
        }

        public string ToJson()
        {
            RefreshSummary();
            return JsonUtility.ToJson(this);
        }
    }
}
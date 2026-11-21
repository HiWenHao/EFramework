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

namespace EasyFramework.Packages
{
    /// <summary>
    /// 包来源类型
    /// <para>Package source type</para>
    /// </summary>
    [Serializable]
    public enum EFPackageSource
    {
        /// <summary>
        /// 未知
        /// <para>Unknown</para>
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// 本地嵌入式包（在 Packages/ 目录下，可直接编辑源码）
        /// <para>Local embedded package (under Packages/, source code is editable)</para>
        /// </summary>
        Local,
        /// <summary>
        /// Git 远端包（通过 Git URL 安装，只读）
        /// <para>Git remote package (installed via Git URL, read-only)</para>
        /// </summary>
        Git,
        /// <summary>
        /// 未安装（服务器上有记录，但本地未安装）
        /// <para>Not installed (recorded on the server but not installed locally)</para>
        /// </summary>
        NotInstalled,
    }

    /// <summary>
    /// 单个资源包信息
    /// <para>Information of a single package</para>
    /// </summary>
    [Serializable]
    public class EFPackageInfo
    {
        /// <summary>
        /// 包名
        /// <para>Package name</para>
        /// </summary>
        [SerializeField] public string Name;
        
        /// <summary>
        /// 展示名
        /// <para>Display name</para>
        /// </summary>
        [SerializeField] public string DisplayName;
        
        /// <summary>
        /// 包来源类型
        /// <para>Package source type</para>
        /// </summary>
        [SerializeField] public EFPackageSource SourceType = EFPackageSource.Unknown;
        
        /// <summary>
        /// 需要更新
        /// <para>Whether an update is needed</para>
        /// </summary>
        [SerializeField] public bool NeedUpdate;
        
        /// <summary>
        /// 包体描述
        /// <para>Package description</para>
        /// </summary>
        [SerializeField] public string Description = "This is a description";
        
        /// <summary>
        /// 服务器版本
        /// <para>Server version</para>
        /// </summary>
        [SerializeField] public string ServerVersion = "0.0.1";

        /// <summary>
        /// 当前版本
        /// <para>Current version</para>
        /// </summary>
        [SerializeField] public string CurrentVersion = "0.0.1";

        /// <summary>
        /// 待发布版本：在面板上做过版本上升、但尚未写入 package.json 的目标版本；
        /// 空串表示当前版本与磁盘一致。只有点击“发布版本”才会真正落盘。
        /// <para>Pending version: the target version bumped in the panel but not yet written to
        /// package.json. Empty means the current version matches the disk; only the publish
        /// action writes it.</para>
        /// </summary>
        [SerializeField] public string PendingVersion = "";
        
    }
}
/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Alvin8412
 * CreationTime:    2026-05-29 15:01:50
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-29 15:01:50
 * ScriptVersion:   0.1
 * ================================================
 */

namespace EasyFramework.Managers.Assets
{
    /// <summary>
    /// 资源子包运行时信息
    /// <para>Runtime information for a sub-package</para>
    /// </summary>
    public class SubPackageInfo
    {
        /// <summary>
        /// 子包配置
        /// <para>Sub-package configuration</para>
        /// </summary>
        public SubPackageConfig Config;

        /// <summary>
        /// YooAsset 资源包实例
        /// <para>YooAsset resource package instance</para>
        /// </summary>
        public YooAsset.ResourcePackage Package;

        /// <summary>
        /// 是否已完成更新检查
        /// <para>Whether the update check has been completed</para>
        /// </summary>
        public bool Checked;

        /// <summary>
        /// 是否需要更新
        /// <para>Whether an update is needed</para>
        /// </summary>
        public bool NeedUpdate;

        /// <summary>
        /// 远端最新版本号
        /// <para>The latest remote version string</para>
        /// </summary>
        public string RemoteVersion;

        /// <summary>
        /// 本地已缓存版本号
        /// <para>The locally cached version string</para>
        /// </summary>
        public string LocalVersion;
    }
}
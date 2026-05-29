/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Alvin8412
 * CreationTime:    2026-05-29 14:49:49
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-29 14:49:49
 * ScriptVersion:   0.1
 * ================================================
 */

namespace EasyFramework.Managers.Assets
{
    /// <summary>
    /// 单个资源包配置,对应一个YooAsset的ResourcePackage
    /// <para>Single resource package configuration, corresponding to one YooAsset's ResourcePackage</para>
    /// </summary>
    public class SubPackageConfig
    {
        /// <summary>
        /// 资源包名
        /// <para>Package Name</para>
        /// </summary>
        public string PackageName = "DefaultPackage";

        /// <summary>
        /// 是否为核心包核心包在启动时下载，非核心按需下载
        /// <para>Is it a core package - Core packages are downloaded at startup,
        /// <br/>while non-core packages are downloaded on demand.</para>
        /// </summary>
        public bool IsEssential = true;

        /// <summary>
        /// 下载时过滤的资源标签（为空时下载该包全部资源）
        /// <para>The filtered resource tags during download (if empty, all resources of this package will be downloaded)</para>
        /// </summary>
        public string[] DownloadTags;

        /// <summary>
        /// 远端主地址（为空则使用 ProjectConfig 全局地址）
        /// <para>Remote main address (if empty, use the global address defined in ProjectConfig)</para>
        /// </summary>
        public string RemoteMainUrl;

        /// <summary>
        /// 远端备用地址（为空则使用 ProjectConfig 全局地址）
        /// <para>Remote fallback address (if empty, use the global address defined in ProjectConfig)</para>
        /// </summary>
        public string RemoteFallbackUrl;
    }
}
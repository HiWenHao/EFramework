/*
 * ================================================
 * Describe:      通过Unity.API获取到从GitHub拉下来的Package包信息
 * Author:        Alvin8412
 * CreationTime:  2026-04-15 22:58:50
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-15 22:58:50
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EasyFramework.Packages
{
    /// <summary>
    /// 通过Unity.API获取到从GitHub拉下来的Package信息
    /// <para>Package information fetched from GitHub via the Unity API</para>
    /// </summary>
    public class GitPackageConfig
    {
        /// <summary>
        /// 包名
        /// <para>Package name</para>
        /// </summary>
        [JsonProperty("name")]
        public string Name;

        /// <summary>
        /// 展示名
        /// <para>Display name</para>
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName;

        /// <summary>
        /// 版本
        /// <para>Version</para>
        /// </summary>
        [JsonProperty("version")]
        public string Version;

        /// <summary>
        /// 最低 Unity 版本
        /// <para>Minimum Unity version</para>
        /// </summary>
        [JsonProperty("unity")]
        public string Unity;

        /// <summary>
        /// Unity 发布版本
        /// <para>Unity release version</para>
        /// </summary>
        [JsonProperty("unityRelease")]
        public string UnityRelease;

        /// <summary>
        /// 包描述
        /// <para>Package description</para>
        /// </summary>
        [JsonProperty("description")]
        public string Description;

        /// <summary>
        /// 作者
        /// <para>Author</para>
        /// </summary>
        [JsonProperty("author")]
        public Author Author;

        /// <summary>
        /// 更新日志地址
        /// <para>Changelog URL</para>
        /// </summary>
        [JsonProperty("changelogUrl")]
        public string ChangelogUrl;

        /// <summary>
        /// 文档地址
        /// <para>Documentation URL</para>
        /// </summary>
        [JsonProperty("documentationUrl")]
        public string DocumentationUrl;

        /// <summary>
        /// 许可证地址
        /// <para>Licenses URL</para>
        /// </summary>
        [JsonProperty("licensesUrl")]
        public string LicensesUrl;

        /// <summary>
        /// 依赖项
        /// <para>Dependencies</para>
        /// </summary>
        [JsonProperty("dependencies")]
        public Dictionary<string, string> Dependencies;

        /// <summary>
        /// 示例列表
        /// <para>List of samples</para>
        /// </summary>
        [JsonProperty("samples")]
        public List<Sample> Samples;
    }

    /// <summary>
    /// 作者信息
    /// <para>Author information</para>
    /// </summary>
    [Serializable]
    public class Author
    {
        /// <summary>
        /// 作者名
        /// <para>Author name</para>
        /// </summary>
        [JsonProperty("name")]
        public string Name;

        /// <summary>
        /// 作者主页地址
        /// <para>Author homepage URL</para>
        /// </summary>
        [JsonProperty("url")]
        public string Url;
    }

    /// <summary>
    /// 示例信息
    /// <para>Sample information</para>
    /// </summary>
    [Serializable]
    public class Sample
    {
        /// <summary>
        /// 展示名
        /// <para>Display name</para>
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName;

        /// <summary>
        /// 描述
        /// <para>Description</para>
        /// </summary>
        [JsonProperty("description")]
        public string Description;

        /// <summary>
        /// 路径
        /// <para>Path</para>
        /// </summary>
        [JsonProperty("path")]
        public string Path;
    }
}

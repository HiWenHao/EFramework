/*
 * ================================================
 * Describe:      用来记录从GitHub上边获取到的Package文件夹下的全部cn.efefef.开头的内容
 * Author:        Alvin8412
 * CreationTime:  2026-04-16 00:22:14
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-16 00:22:14
 * ScriptVersion: 0.1
 * ===============================================
 */

using Newtonsoft.Json;

namespace EasyFramework.Packages
{
	/// <summary>
	/// 用来记录从GitHub上边获取到的Package文件夹下的全部cn.efefef.开头的内容
	/// <para>Records all cn.efefef.-prefixed content under the Package folder fetched from GitHub</para>
	/// </summary>

	public class GitHubContentItem
	{
		/// <summary>
		/// 名称
		/// <para>Name</para>
		/// </summary>
		[JsonProperty("name")]
		public string Name { get; set; }

		/// <summary>
		/// 路径
		/// <para>Path</para>
		/// </summary>
		[JsonProperty("path")]
		public string Path { get; set; }

		/// <summary>
		/// SHA 值
		/// <para>SHA value</para>
		/// </summary>
		[JsonProperty("sha")]
		public string Sha { get; set; }

		/// <summary>
		/// 大小（字节）
		/// <para>Size in bytes</para>
		/// </summary>
		[JsonProperty("size")]
		public int Size { get; set; }

		/// <summary>
		/// API 地址
		/// <para>API URL</para>
		/// </summary>
		[JsonProperty("url")]
		public string Url { get; set; }

		/// <summary>
		/// HTML 页面地址
		/// <para>HTML page URL</para>
		/// </summary>
		[JsonProperty("html_url")]
		public string HtmlUrl { get; set; }

		/// <summary>
		/// Git 地址
		/// <para>Git URL</para>
		/// </summary>
		[JsonProperty("git_url")]
		public string GitUrl { get; set; }

		/// <summary>
		/// 下载地址（可能为 null）
		/// <para>Download URL (may be null)</para>
		/// </summary>
		[JsonProperty("download_url")]
		public string DownloadUrl { get; set; }  // 可能为 null

		/// <summary>
		/// 类型
		/// <para>Type</para>
		/// </summary>
		[JsonProperty("type")]
		public string Type { get; set; }

		/// <summary>
		/// 链接集合
		/// <para>Links</para>
		/// </summary>
		[JsonProperty("_links")]
		public Links Links { get; set; }
	}

	/// <summary>
	/// 链接集合
	/// <para>Links</para>
	/// </summary>
	public class Links
	{
		/// <summary>
		/// 自身链接
		/// <para>Self link</para>
		/// </summary>
		[JsonProperty("self")]
		public string Self { get; set; }

		/// <summary>
		/// Git 链接
		/// <para>Git link</para>
		/// </summary>
		[JsonProperty("git")]
		public string Git { get; set; }

		/// <summary>
		/// HTML 链接
		/// <para>HTML link</para>
		/// </summary>
		[JsonProperty("html")]
		public string Html { get; set; }
	}
}

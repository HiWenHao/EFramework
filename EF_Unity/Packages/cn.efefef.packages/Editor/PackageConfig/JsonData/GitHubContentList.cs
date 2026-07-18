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
using System.Collections.Generic;

namespace EasyFramework.Edit.Packages
{
	/// <summary>
	/// 用来记录从GitHub上边获取到的Package文件夹下的全部cn.efefef.开头的内容
	/// </summary>

	public class GitHubContentItem
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("path")]
		public string Path { get; set; }

		[JsonProperty("sha")]
		public string Sha { get; set; }

		[JsonProperty("size")]
		public int Size { get; set; }

		[JsonProperty("url")]
		public string Url { get; set; }

		[JsonProperty("html_url")]
		public string HtmlUrl { get; set; }

		[JsonProperty("git_url")]
		public string GitUrl { get; set; }

		[JsonProperty("download_url")]
		public string DownloadUrl { get; set; }  // 可能为 null

		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("_links")]
		public Links Links { get; set; }
	}

	public class Links
	{
		[JsonProperty("self")]
		public string Self { get; set; }

		[JsonProperty("git")]
		public string Git { get; set; }

		[JsonProperty("html")]
		public string Html { get; set; }
	}
}

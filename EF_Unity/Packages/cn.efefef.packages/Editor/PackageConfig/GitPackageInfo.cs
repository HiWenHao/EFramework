/*
 * ================================================
 * Describe:      This script is used to .
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

namespace EasyFramework.Edit.Windows.ConfigPanel
{
    public class GitPackageConfig
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("displayName")]
        public string DisplayName;

        [JsonProperty("version")]
        public string Version;

        [JsonProperty("unity")]
        public string Unity;

        [JsonProperty("unityRelease")]
        public string UnityRelease;

        [JsonProperty("description")]
        public string Description;

        [JsonProperty("author")]
        public Author Author;

        [JsonProperty("changelogUrl")]
        public string ChangelogUrl;

        [JsonProperty("documentationUrl")]
        public string DocumentationUrl;

        [JsonProperty("licensesUrl")]
        public string LicensesUrl;

        [JsonProperty("dependencies")]
        public Dictionary<string, string> Dependencies;

        [JsonProperty("samples")]
        public List<Sample> Samples;
    }

    [Serializable]
    public class Author
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("url")]
        public string Url;
    }

    [Serializable]
    public class Sample
    {
        [JsonProperty("displayName")]
        public string DisplayName;

        [JsonProperty("description")]
        public string Description;

        [JsonProperty("path")]
        public string Path;
    }
}

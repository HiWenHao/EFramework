/*
 * ================================================
 * Describe:        This script is used to read and index asmdef files.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 21:20:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-14 21:20:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Launcher.Editor
{
    /// <summary>
    /// 一个 asmdef 的只读信息
    /// <para>Read-only snapshot of one assembly definition.</para>
    /// </summary>
    internal sealed class AsmdefInfo
    {
        public string Path;
        public string Name;
        public string[] References = Array.Empty<string>();
        public string[] PrecompiledReferences = Array.Empty<string>();
        public string[] IncludePlatforms = Array.Empty<string>();

        /// <summary>是否只编进编辑器（不进 Player）</summary>
        public bool IsEditorOnly
        {
            get
            {
                for (int i = 0; i < IncludePlatforms.Length; i++)
                {
                    if (string.Equals(IncludePlatforms[i], "Editor", StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }
        }
    }

    /// <summary>
    /// 全工程 asmdef 索引：支持按路径（GUID 引用）和按名字（name 引用）两种解析
    /// <para>Project-wide asmdef index. Resolves both GUID-based and name-based references.</para>
    /// </summary>
    internal sealed class AsmdefDatabase
    {
        [Serializable]
        private sealed class AsmdefData
        {
            public string name = null;
            public string[] references = null;
            public string[] precompiledReferences = null;
            public string[] includePlatforms = null;
        }

        private readonly Dictionary<string, AsmdefInfo> _byPath =
            new Dictionary<string, AsmdefInfo>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, AsmdefInfo> _byName =
            new Dictionary<string, AsmdefInfo>(StringComparer.Ordinal);

        /// <summary>全部 asmdef</summary>
        public IEnumerable<AsmdefInfo> All => _byPath.Values;

        public static AsmdefDatabase Build()
        {
            var database = new AsmdefDatabase();

            foreach (string guid in AssetDatabase.FindAssets("t:AssemblyDefinitionAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var info = Load(path);
                if (info == null)
                    continue;

                database._byPath[path] = info;
                database._byName[info.Name] = info;
            }

            return database;
        }

        public bool TryGetByPath(string path, out AsmdefInfo info) => _byPath.TryGetValue(path, out info);

        /// <summary>
        /// 解析一条 asmdef 引用（"GUID:xxx" 或程序集名）
        /// </summary>
        public AsmdefInfo Resolve(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                return null;

            if (reference.StartsWith("GUID:", StringComparison.OrdinalIgnoreCase))
            {
                string path = AssetDatabase.GUIDToAssetPath(reference.Substring(5));
                if (string.IsNullOrEmpty(path))
                    return null;

                if (_byPath.TryGetValue(path, out var byPath))
                    return byPath;

                return Load(path);
            }

            return _byName.TryGetValue(reference, out var byName) ? byName : null;
        }

        private static AsmdefInfo Load(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase))
                return null;

            // 直接读文件，不依赖 AssemblyDefinitionAsset（不同 Unity 版本所在程序集不一样）
            string fullPath = System.IO.Path.GetFullPath(path);
            if (!File.Exists(fullPath))
                return null;

            var data = JsonUtility.FromJson<AsmdefData>(File.ReadAllText(fullPath));
            if (data == null)
                return null;

            return new AsmdefInfo
            {
                Path = path,
                Name = string.IsNullOrEmpty(data.name) ? System.IO.Path.GetFileNameWithoutExtension(path) : data.name,
                References = data.references ?? Array.Empty<string>(),
                PrecompiledReferences = data.precompiledReferences ?? Array.Empty<string>(),
                IncludePlatforms = data.includePlatforms ?? Array.Empty<string>(),
            };
        }
    }
}

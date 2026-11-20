/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-05-20 14:44:04
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-20 14:44:04
 * ScriptVersion: 0.2
 * ===============================================
 */

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.Packages
{
    /// <summary>
    /// 包版本写盘结果（版本上升、恢复默认版本共用）
    /// <para>Result of writing a package version (shared by bump and restore).</para>
    /// </summary>
    internal enum PackageVersionWriteResult
    {
        // 上升成功并已写盘
        Success,
        // 包目录下不存在 package.json
        PackageJsonNotFound,
        // 版本号为空或含非数字分段
        VersionUnparsable,
        // 读/写 package.json 失败
        WriteFailed,
    }

    /// <summary>
    /// 框架远端服务工具箱
    /// <para>Framework remote service toolkit</para>
    /// </summary>
    public static class ServerToolkit
    {
        // package.json 中根级 "version" 属性的定位模式（依赖项键名不会是 version，故首匹配即根级）
        private static readonly Regex VersionPropertyPattern =
            new Regex("\"version\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.Compiled);

        /// <summary> 本地工作状态路径（ProjectSettings 下，自动管理） </summary>
        private static string ConfigPath => Path.Combine(Application.dataPath, "../ProjectSettings/EFPackageCache.json");

        /// <summary>
        /// 官方包目录路径（Editor Resources 下，应提交到 Git）
        /// <para>Official package catalog path (under Editor Resources; should be committed to Git)</para>
        /// </summary>
        public static string CatalogPath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "../Packages/cn.efefef.packages/Editor Resources/EFPackageCache.json"));

        /// <summary>
        /// 远端目录相对路径（在仓库中的位置）
        /// <para>Relative path of the remote catalog in the repository</para>
        /// </summary>
        public const string RemoteCatalogRepoPath = "EF_Unity/Packages/cn.efefef.packages/Editor Resources/EFPackageCache.json";

        /// <summary>
        /// 获取包配置数据
        /// <para>Get the package configuration data.</para>
        /// </summary>
        /// <returns>包配置数据，文件不存在时返回 null<para>The package configuration, or null if the file does not exist.</para></returns>
        public static PackageConfig GetPackageConfig()
        {
            if (!File.Exists(ConfigPath))
                return null;

            string json = File.ReadAllText(ConfigPath);
            var packageConfig = JsonUtility.FromJson<PackageConfig>(json);
            return packageConfig;
        }

        /// <summary>
        /// 保存包配置数据
        /// <para>Save the package configuration data.</para>
        /// </summary>
        /// <param name="packageConfig">包配置数据<para>The package configuration data.</para></param>
        public static void SavePackageConfig(PackageConfig packageConfig)
        {
            string json = packageConfig.ToJson();
            File.WriteAllText(ConfigPath,  json);
        }
        
        /// <summary>
        /// 保存包配置 JSON 文本
        /// <para>Save the raw package configuration JSON text.</para>
        /// </summary>
        /// <param name="packageConfig">包配置 JSON 文本<para>The package configuration JSON text.</para></param>
        public static void SavePackageConfig(string packageConfig)
        {
            File.WriteAllText(ConfigPath,  packageConfig);
        }

        #region 官方包目录 (Catalog) — 提交到 Git 的文件

        /// <summary>
        /// 判断当前项目是否为 EFramework 源码项目（而非使用者项目）
        /// 检测标记：存在 EFramework.sln / EasyFramework-Unity.xmind / .github 目录
        /// 同时支持标准布局（sln 与 Assets 同级）和 EF 布局（sln 在上级目录）
        /// <para>Whether the current project is the EFramework source project (rather than a consumer project)</para>
        /// <para>Detection markers: EFramework.sln / EasyFramework-Unity.xmind / .github directory</para>
        /// <para>Supports both the standard layout (sln alongside Assets) and the EF layout (sln one level up)</para>
        /// </summary>
        public static bool IsFrameworkProject
        {
            get
            {
                string dataPathParent = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string grandParent = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
                
                // 检查两个层级
                string[] candidates = { dataPathParent, grandParent };
                foreach (string dir in candidates)
                {
                    if (File.Exists(Path.Combine(dir, "EFramework.sln"))
                        || File.Exists(Path.Combine(dir, "EasyFramework-Unity.xmind"))
                        || Directory.Exists(Path.Combine(dir, ".github")))
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 加载本地官方包目录
        /// <para>Load the local official package catalog.</para>
        /// </summary>
        /// <returns>本地官方包目录，不存在时返回 null<para>The local official catalog, or null if none exists.</para></returns>
        public static EFPackageCatalog LoadCatalog()
        {
            if (!File.Exists(CatalogPath))
                return null;
            return EFPackageCatalog.FromJson(File.ReadAllText(CatalogPath));
        }

        /// <summary>
        /// 保存官方包目录
        /// <para>Save the official package catalog.</para>
        /// </summary>
        /// <param name="catalog">官方包目录<para>The official package catalog.</para></param>
        public static void SaveCatalog(EFPackageCatalog catalog)
        {
            string dir = Path.GetDirectoryName(CatalogPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(CatalogPath, catalog.ToJson());
            
            D.Emphasize($"[EF.Packages] 包目录已保存: {CatalogPath}");
            D.Emphasize($"[EF.Packages] 请将此文件提交到 Git 仓库，开发者即可通过\"更新全部信息\"拉取最新目录。");
        }

        /// <summary>
        /// 从本地扫描所有 EF 包，生成官方目录
        /// <para>Scan all local EF packages and generate the official catalog.</para>
        /// </summary>
        /// <returns>生成的官方包目录<para>The generated official package catalog.</para></returns>
        public static EFPackageCatalog GenerateCatalogFromLocalPackages()
        {
            var catalog = new EFPackageCatalog
            {
                generatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            foreach (var packageInfo in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                if (!packageInfo.name.Contains("cn.efefef."))
                    continue;

                // 版本以磁盘 package.json 为准：Unity 对嵌入式包的 PackageInfo.version 会在
                // package.json 改动后滞后一个解析周期，直接取缓存值会写入过期版本号
                string version = TryReadPackageJsonVersion(packageInfo.name, out string diskVersion)
                    ? diskVersion
                    : packageInfo.version;

                catalog.packages.Add(new EFPackageCatalogEntry
                {
                    name = packageInfo.name,
                    displayName = string.IsNullOrEmpty(packageInfo.displayName)
                        ? packageInfo.name : packageInfo.displayName,
                    description = packageInfo.description,
                    version = version,
                });
            }
            
            D.Emphasize($"[EF.Packages] 已扫描 {catalog.packages.Count} 个 EF 包");
            return catalog;
        }

        #endregion

        #region 包版本维护 (Package Version) — 仅框架源码工程使用

        /// <summary>
        /// 获取包 package.json 相对工程根的路径（AssetDatabase 接口只接受相对路径）
        /// <para>Get a package's package.json path relative to the project root
        /// (AssetDatabase APIs only accept relative paths).</para>
        /// </summary>
        /// <param name="packageName">包名<para>Package name.</para></param>
        /// <returns>形如 "Packages/{包名}/package.json" 的相对路径<para>Relative path like "Packages/{name}/package.json".</para></returns>
        private static string GetPackageJsonAssetPath(string packageName)
        {
            return "Packages/" + packageName + "/package.json";
        }

        /// <summary>
        /// 获取包目录下的 package.json 绝对路径
        /// <para>Get the absolute path of a package's package.json.</para>
        /// </summary>
        /// <param name="packageName">包名<para>Package name.</para></param>
        /// <returns>package.json 绝对路径（不保证存在）<para>Absolute path (existence not guaranteed).</para></returns>
        private static string GetPackageJsonPath(string packageName)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", GetPackageJsonAssetPath(packageName)));
        }

        /// <summary>
        /// 从 package.json 文本中读取根级 version
        /// <para>Read the root-level version from package.json text.</para>
        /// </summary>
        /// <param name="json">package.json 文本<para>package.json text.</para></param>
        /// <param name="version">读到的版本号<para>The version read.</para></param>
        /// <returns>读取成功返回 true<para>True when read successfully.</para></returns>
        private static bool TryReadJsonVersion(string json, out string version)
        {
            version = null;
            if (string.IsNullOrEmpty(json))
                return false;

            Match match = VersionPropertyPattern.Match(json);
            if (!match.Success)
                return false;

            version = match.Groups[1].Value;
            return true;
        }

        /// <summary>
        /// 读取指定包 package.json 中的版本号（不依赖 Unity 的包信息缓存）
        /// <para>Read a package version straight from its package.json, bypassing Unity's
        /// package-info cache.</para>
        /// </summary>
        /// <param name="packageName">包名<para>Package name.</para></param>
        /// <param name="version">读到的版本号<para>The version read.</para></param>
        /// <returns>文件存在且解析成功返回 true<para>True when the file exists and parses.</para></returns>
        public static bool TryReadPackageJsonVersion(string packageName, out string version)
        {
            version = null;
            string path = GetPackageJsonPath(packageName);
            if (!File.Exists(path))
                return false;

            try
            {
                return TryReadJsonVersion(File.ReadAllText(path), out version);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 将版本号解析为数字分段（只接受纯数字，拒绝 "+1"、" 1"、"1,000"、"1.0-beta" 这类写法）
        /// <para>Parse a version string into numeric segments (digits only; rejects "+1", " 1",
        /// "1,000" and "1.0-beta").</para>
        /// </summary>
        /// <param name="version">版本号<para>Version string.</para></param>
        /// <param name="numbers">解析出的各段数值<para>Parsed segment values.</para></param>
        /// <returns>全部段落均为纯数字时返回 true<para>True when every segment is pure digits.</para></returns>
        private static bool TryParseVersionParts(string version, out int[] numbers)
        {
            numbers = null;
            if (string.IsNullOrEmpty(version))
                return false;

            string[] segments = version.Split('.');
            if (segments.Length == 0)
                return false;

            var parsed = new int[segments.Length];
            for (int i = 0; i < segments.Length; i++)
            {
                // NumberStyles.None：不接受符号、空白、千分位与小数点
                if (!int.TryParse(segments[i], NumberStyles.None, CultureInfo.InvariantCulture, out parsed[i]))
                    return false;
            }

            numbers = parsed;
            return true;
        }

        /// <summary>
        /// 按 SemVer 规则上升指定段：MAJOR/MINOR/PATCH，高位+1 时低位归零
        /// <para>Bump a specific SemVer segment (MAJOR/MINOR/PATCH); incrementing a higher
        /// segment resets all lower segments to zero.</para>
        /// </summary>
        /// <param name="version">原版本号；空串按 0.0.0 起算<para>Source version; empty counts as 0.0.0.</para></param>
        /// <param name="segment">要上升的段（0=MAJOR, 1=MINOR, 2=PATCH）<para>Segment to bump (0=MAJOR, 1=MINOR, 2=PATCH).</para></param>
        /// <param name="bumped">上升后的版本号<para>The bumped version.</para></param>
        /// <returns>解析成功返回 true；段越界、非数字或溢出时返回 false<para>True when parsed; false on invalid segment, non-numeric part or overflow.</para></returns>
        public static bool TryBumpSemVer(string version, int segment, out string bumped)
        {
            string source = string.IsNullOrEmpty(version) ? "0.0.0" : version.Trim();
            bumped = source;

            if (segment < 0 || segment > 2)
                return false;
            if (!TryParseVersionParts(source, out int[] parts))
                return false;

            // 不足三段补 0（1.2 -> 1.2.0）；超过三段拒绝（SemVer 只允许三段）
            if (parts.Length > 3)
                return false;
            var numbers = new int[3];
            Array.Copy(parts, numbers, parts.Length);

            if (numbers[segment] == int.MaxValue)
                return false;
            numbers[segment]++;
            for (int i = segment + 1; i < numbers.Length; i++)
                numbers[i] = 0; // 高位进位，低位归零

            var builder = new StringBuilder();
            for (int i = 0; i < numbers.Length; i++)
            {
                if (i > 0)
                    builder.Append('.');
                builder.Append(numbers[i].ToString(CultureInfo.InvariantCulture));
            }
            bumped = builder.ToString();
            return true;
        }

        /// <summary>
        /// 末位版本号 +1 并按十进制进位（0.0.9 → 0.1.0、0.9.9 → 1.0.0、9.9.9 → 10.0.0）
        /// <para>Increment the last version segment by one with decimal carrying
        /// (0.0.9 -> 0.1.0, 0.9.9 -> 1.0.0, 9.9.9 -> 10.0.0).</para>
        /// </summary>
        /// <param name="version">原版本号；空串按 0.0.0 起算<para>Source version; empty counts as 0.0.0.</para></param>
        /// <param name="bumped">上升后的版本号<para>The bumped version.</para></param>
        /// <returns>解析成功返回 true；含非数字分段或溢出时返回 false<para>True when parsed; false on a non-numeric segment or overflow.</para></returns>
        public static bool TryBumpVersion(string version, out string bumped)
        {
            string source = string.IsNullOrEmpty(version) ? "0.0.0" : version.Trim();
            bumped = source;

            if (!TryParseVersionParts(source, out int[] numbers))
                return false;

            // 从最低位 +1，满十归零并向高一位进一；最高位溢出时保留为多位段（9.9.9 → 10.0.0）
            int index = numbers.Length - 1;
            if (numbers[index] == int.MaxValue)
                return false;
            numbers[index]++;

            while (index > 0 && numbers[index] == 10)
            {
                numbers[index] = 0;
                index--;
                if (numbers[index] == int.MaxValue)
                    return false;
                numbers[index]++;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < numbers.Length; i++)
            {
                if (i > 0)
                    builder.Append('.');
                builder.Append(numbers[i].ToString(CultureInfo.InvariantCulture));
            }

            bumped = builder.ToString();
            return true;
        }

        /// <summary>
        /// 用目标版本号替换 package.json 文本中的根级 version 值
        /// <para>Replace the root-level version value inside package.json text.</para>
        /// </summary>
        /// <param name="json">原始文本<para>Original text.</para></param>
        /// <param name="oldVersion">文本中现有的版本号<para>Version currently in the text.</para></param>
        /// <param name="newVersion">要写入的版本号<para>Version to write.</para></param>
        /// <param name="replaced">替换后的文本<para>Text after replacement.</para></param>
        /// <returns>成功定位并替换返回 true<para>True when located and replaced.</para></returns>
        private static bool TryReplaceJsonVersion(string json, string oldVersion, string newVersion, out string replaced)
        {
            replaced = null;

            // 只替换根级 "version" 的属性值本身，其余字段与缩进保持原样。
            // 严禁用 "$1" + newVersion + "$2" 这类拼接替换串：新版本号以数字开头时，
            // "$1" 会与首位数字粘成 "$10" 被解析成第 10 个捕获组（不存在），
            // 导致 "version": " 前缀整段被吞掉、文件变成非法 JSON。
            // 这里改为按捕获组的索引做纯文本拼接，彻底绕开替换串的 $ 语义。
            Match match = VersionPropertyPattern.Match(json);
            if (!match.Success || !string.Equals(match.Groups[1].Value, oldVersion, StringComparison.Ordinal))
                return false;

            Group valueGroup = match.Groups[1];
            string result = json.Substring(0, valueGroup.Index)
                            + newVersion
                            + json.Substring(valueGroup.Index + valueGroup.Length);

            // 写盘前自检：替换结果必须仍能读出目标版本号，杜绝任何情况下产出损坏的 package.json
            if (!TryReadJsonVersion(result, out string writtenVersion)
                || !string.Equals(writtenVersion, newVersion, StringComparison.Ordinal))
                return false;

            replaced = result;
            return true;
        }

        /// <summary>
        /// 通知 Unity 重新解析 package.json（失败只提示手动刷新，不影响已完成的写盘）
        /// <para>Ask Unity to reimport package.json; a failure only warns, since the file is already written.</para>
        /// </summary>
        /// <param name="packageName">包名<para>Package name.</para></param>
        private static void ImportPackageJson(string packageName)
        {
            try
            {
                AssetDatabase.ImportAsset(GetPackageJsonAssetPath(packageName));
            }
            catch (Exception)
            {
                D.Warning($"[EF.Packages] {packageName}/package.json 已写入，但 AssetDatabase 导入失败，请手动刷新工程。");
            }
        }

        /// <summary>
        /// 把指定包的版本号写为目标值并回写 package.json（保留文件原有排版）
        /// <para>Write a target version into a package's package.json, preserving the file's
        /// original formatting.</para>
        /// </summary>
        /// <param name="packageName">包名<para>Package name.</para></param>
        /// <param name="targetVersion">目标版本号（必须是纯数字分段）<para>Target version (numeric segments only).</para></param>
        /// <param name="oldVersion">修改前的版本号<para>Version before the write.</para></param>
        /// <returns>写盘结果<para>The write result.</para></returns>
        internal static PackageVersionWriteResult TryWritePackageVersion(string packageName, string targetVersion, out string oldVersion)
        {
            oldVersion = null;

            // 目标值必须是纯数字分段，避免把空串或预览标签写进 manifest
            if (!TryParseVersionParts(targetVersion, out _))
                return PackageVersionWriteResult.VersionUnparsable;

            string path = GetPackageJsonPath(packageName);
            if (!File.Exists(path))
                return PackageVersionWriteResult.PackageJsonNotFound;

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception)
            {
                return PackageVersionWriteResult.WriteFailed;
            }

            if (!TryReadJsonVersion(json, out oldVersion))
                return PackageVersionWriteResult.VersionUnparsable;

            // 已是目标值：无需写盘，按成功处理（正常情况下按钮此时应已隐藏）
            if (string.Equals(oldVersion, targetVersion, StringComparison.Ordinal))
                return PackageVersionWriteResult.Success;

            if (!TryReplaceJsonVersion(json, oldVersion, targetVersion, out string replaced))
                return PackageVersionWriteResult.WriteFailed;

            try
            {
                File.WriteAllText(path, replaced);
            }
            catch (Exception)
            {
                return PackageVersionWriteResult.WriteFailed;
            }

            ImportPackageJson(packageName);
            return PackageVersionWriteResult.Success;
        }

        #endregion
        
        /// <summary>
        /// 获取请求链接地址
        /// <para>Get the request URL.</para>
        /// </summary>
        /// <param name="platform">远端类型<para>Remote platform type.</para></param>
        /// <returns>框架仓库链接地址<para>The framework repository URL.</para></returns>
        public static string GetFrameworkPath(ServerType platform)
        {
            return platform switch
            {
                ServerType.GitHub => "https://github.com/HiWenHao/EFramework",
                ServerType.Gitee => "https://gitee.com/AlvinCN/EFramework",
                ServerType.Local => "file://",
                _ => throw new NotSupportedException()
            };
        }

        /// <summary>
        /// 获取框架作者名
        /// <para>Get the framework author name.</para>
        /// </summary>
        /// <param name="platform">远端类型<para>Remote platform type.</para></param>
        /// <returns>框架作者名<para>The framework author name.</para></returns>
        public static string GetFrameworkOwner(ServerType platform)
        {
            return platform switch
            {
                ServerType.GitHub => "HiWenHao",
                ServerType.Gitee => "AlvinCN",
                ServerType.Local => "Alvin",
                _ => throw new NotSupportedException()
            };
        }

        /// <summary>
        /// 获取请求链接地址
        /// <para>Get the raw file URL.</para>
        /// </summary>
        /// <param name="platform">远端类型<para>Remote platform type.</para></param>
        /// <param name="owner">仓库拥有者名<para>Repository owner name.</para></param>
        /// <param name="repo">框架名<para>Repository name.</para></param>
        /// <param name="branch">分支名<para>Branch name.</para></param>
        /// <param name="filePath">位于项目下的具体位置<para>Path of the file within the project.</para></param>
        /// <returns>文件的原始链接地址<para>The raw file URL.</para></returns>
        /// <exception cref="NotSupportedException">不支持的远端类型<para>Thrown for an unsupported platform.</para></exception>
        public static string GetRawUrl(ServerType platform, string owner, string repo, string branch, string filePath)
        {
            string encodedPath = Uri.EscapeDataString(filePath);
            return platform switch
            {
                ServerType.GitHub => $"https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{encodedPath}",
                ServerType.Gitee => $"https://gitee.com/{owner}/{repo}/raw/{branch}/{encodedPath}",
                _ => throw new NotSupportedException()
            };
        }
        
        /// <summary>
        /// 获取指定平台的请求令牌
        /// <para>Get the request token for a platform.</para>
        /// </summary>
        /// <param name="platform">远端类型<para>Remote platform type.</para></param>
        /// <returns>请求令牌<para>The request token.</para></returns>
        public static string GetToken(ServerType platform)
        {
            return EditorPrefs.GetString(Application.productName + GetKey(platform), "");
        }

        /// <summary>
        /// 设置指定平台的请求令牌
        /// <para>Set the request token for a platform.</para>
        /// </summary>
        /// <param name="platform">远端类型<para>Remote platform type.</para></param>
        /// <param name="token">请求令牌<para>The request token.</para></param>
        public static void SetToken(ServerType platform, string token)
        {
            EditorPrefs.SetString(UnityEngine.Application.productName + GetKey(platform), token);
        }

        private static string GetKey(ServerType platform)
        {
            return platform switch
            {
                ServerType.GitHub => "EFPackagesGitHub",
                ServerType.Gitee => "EFPackagesGitee",
                ServerType.Local => "Local",
                _ => "Local"
            };
        }
    }
}
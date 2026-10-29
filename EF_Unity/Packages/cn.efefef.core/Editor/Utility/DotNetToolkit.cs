/*
 * ================================================
 * Describe:        .NET 运行时环境检测工具：dotnet CLI 版本校验等
 * Author:          Alvin8412
 * CreationTime:    2026-10-19 14:27:37
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-10-19 14:45:00
 * ScriptVersion:   0.2
 * ================================================
 */

using System;
using UnityEditor;

namespace EasyFramework.Edit
{
    /// <summary>
    /// .NET 运行时环境检测工具
    /// <para>.NET runtime environment checks (dotnet CLI version validation, etc.).</para>
    /// </summary>
    public static class DotNetToolkit
    {
        #region Methods

        /// <summary>
        /// 检测系统 dotnet CLI 版本是否满足最低主版本要求，不满足时弹窗报错
        /// <para>Ensures the system dotnet CLI satisfies the minimum major version;
        /// shows an error dialog on failure.</para>
        /// <para>检测对象是外部进程的 dotnet CLI（Luban 等工具经它运行），而非 Unity 内嵌
        /// Mono 的 Environment.Version——后者恒为 4.x，检测它没有意义。</para>
        /// </summary>
        /// <param name="minMajor">最低主版本号，如 8 表示需要 .NET 8.0 及以上<para>Minimum major version, e.g. 8 for .NET 8.0+.</para></param>
        /// <param name="toolSourceDir">子进程工作目录（通常为工具所在目录）<para>Working directory of the child process (usually the tool folder).</para></param>
        /// <returns>版本满足返回 true；CLI 缺失 / 版本号无法解析 / 低于 minMajor 时返回 false（已弹窗提示）<para>True when satisfied; false on failure (error dialog shown).</para></returns>
        public static bool EnsureDotNetVersion(int minMajor, string toolSourceDir)
        {
            bool isEn = LC.IsEnglish;
            var result = ProcessToolkit.RunCaptured("dotnet", toolSourceDir, new[] { "--version" });
            if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.Output))
            {
                EditorUtility.DisplayDialog(
                    isEn ? "dotnet CLI not found" : "未检测到 dotnet CLI",
                    (isEn
                        ? $"No usable dotnet CLI found. Please install .NET SDK {minMajor}.0 or later and retry.\n\n"
                        : $"未检测到可用的 dotnet CLI，请安装 .NET SDK {minMajor}.0 或更高版本后重试。\n"),
                    LC.Combine(Lc.Ok));
                return false;
            }

            // dotnet --version 正常输出单行版本号；个别环境会附带告警行，取首个可解析行
            string versionText = null;
            foreach (string line in result.Output.Split('\n'))
            {
                if (!Version.TryParse(line.Trim(), out var parsed)) continue;
                versionText = parsed.ToString();
                break;
            }

            if (versionText == null)
            {
                EditorUtility.DisplayDialog(
                    isEn ? "Cannot parse dotnet version" : "无法解析 dotnet 版本",
                    (isEn
                        ? "Cannot parse the dotnet version output. Please check the dotnet installation.\n\n"
                        : "无法解析 dotnet 版本号，请检查 dotnet 安装是否完整。\n")
                    + result.Output.Trim(),
                    LC.Combine(Lc.Ok));
                return false;
            }

            var version = new Version(versionText);
            if (version.Major >= minMajor) return true;

            EditorUtility.DisplayDialog(
                isEn ? "dotnet version too low" : "dotnet 版本过低",
                isEn
                    ? $"dotnet is too old: {version} found, but {minMajor}.0+ is required. Please upgrade the .NET SDK."
                    : $"dotnet 版本过低：当前 {version}，需要 {minMajor}.0 及以上，请升级 .NET SDK 后重试。\n",
                LC.Combine(Lc.Ok));
            return false;
        }

        #endregion
    }
}
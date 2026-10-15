/*
 * ================================================
 * Describe:        多语言模块的全局常量与路径
 * Author:          Alvin5100
 * CreationTime:    2026-10-14 14:03:21
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-14 14:03:21
 * ScriptVersion:   0.1
 * ================================================
 */

using System.IO;
using UnityEngine;

namespace EasyFramework.Localization.Edit
{
    /// <summary>
    /// 多语言模块的全局常量与路径
    /// <para>面板、Inspector、搜索窗共用此处的路径定义，避免各处硬编码字符串。</para>
    /// <para>Shared constants and paths for the panel, inspector and search window.</para>
    /// </summary>
    internal static class LocalizationData
    {
        /// <summary>
        /// Key 清单文件名
        /// <para>多语言 Key 清单的唯一文件名。</para>
        /// </summary>
        internal const string FileName = "LocalizationKeys.json";

        /// <summary>
        /// 存放目录（相对工程 Resources）
        /// <para>放在 Resources 下，运行时可直接通过 Resources 加载。</para>
        /// </summary>
        internal const string Folder = "Resources/L10n";

        /// <summary>
        /// value表目录（相对工程根，非 Resources）
        /// <para>供翻译填写的源文件放这里，由 Luban 导表后产出 bytes进包。</para>
        /// </summary>
        internal const string ValuesDir = "EFTools/Datable/Datas";

        /// <summary>
        /// value 表文件名前缀
        /// <para>按语言分文件：{{Lang}}.csv → {{Lang}}.bytes。</para>
        /// </summary>
        internal const string ValuesFilePrefix = "L10n_";

        /// <summary>
        /// Key 清单 JSON 绝对路径
        /// <para>面板读写的唯一目标文件；不存在时面板会引导创建。</para>
        /// <para>Absolute path of the key manifest.</para>
        /// </summary>
        internal static string JsonPath => Path.Combine(Application.dataPath, Folder, FileName);

        /// <summary>
        /// value 表目录绝对路径, 不存在时导出函数会自动创建
        /// </summary>
        internal static string ValuesPath
            => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValuesDir));

        /// <summary>
        /// 指定语言的 value 表（CSV）绝对路径
        /// </summary>
        /// <param name="language">语言代码，如 zh-CN</param>
        internal static string GetValuesPath(string language)
            => Path.Combine(ValuesPath, $"{ValuesFilePrefix}{language}.csv");
    }
}
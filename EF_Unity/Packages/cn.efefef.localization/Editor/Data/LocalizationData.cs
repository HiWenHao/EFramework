/*
 * ================================================
 * Describe:        多语言模块的全局常量、路径与编辑器偏好
 * Author:          Alvin5100
 * CreationTime:    2026-10-14 14:03:21
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-16 11:42:00
 * ScriptVersion:   0.3
 * ================================================
 */

using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Localization.Edit
{
    /// <summary>
    /// 多语言模块的全局常量、路径与编辑器偏好
    /// <para>面板、Inspector、搜索窗共用此处的路径定义与行为开关，
    /// 避免各处硬编码字符串、避免同类状态散落多个类。</para>
    /// <para>Shared constants, paths and editor-backed options for the panel,
    /// inspector and search window.</para>
    /// </summary>
    internal static class LocalizationData
    {
        #region 路径与常量 Paths & Constants

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

        #endregion

        #region 编辑器偏好 Editor Preferences

        /// <summary>
        /// EditorPrefs 键前缀（带包名，避免与工程内其他设置撞名）
        /// </summary>
        private const string PrefsKeyPrefix = "EF.Localization.";

        /// <summary>
        /// 选择 key 后是否自动把预览译文写入文本框
        /// <para>true：LocalizedText 的 Inspector 隐藏「应用」按钮，选择 key 即自动应用；
        /// false（默认）：显示按钮，由使用者手动应用。</para>
        /// <para>设置面板（<see cref="LocalizationKeyPanel"/>）写入、Inspector
        /// （<see cref="LocalizedTextEditor"/>）读取；写入端只在值变化时赋值，
        /// 避免每帧重绘都落盘 EditorPrefs。</para>
        /// <para>Whether picking a key auto-writes the previewed translation
        /// into the text field. Persisted via EditorPrefs.</para>
        /// </summary>
        internal static bool AutoApplyOnKeySelected
        {
            get => EditorPrefs.GetBool(PrefsKeyPrefix + "AutoApplyOnKeySelected", false);
            set => EditorPrefs.SetBool(PrefsKeyPrefix + "AutoApplyOnKeySelected", value);
        }

        /// <summary>
        /// 编辑器预览语言（Inspector 预览行使用）
        /// <para>静态字段会在每次代码编译（域重载）后清零，故走 EditorPrefs 持久化，
        /// 保持使用者上一次的预览语言选择；默认 zn。</para>
        /// <para>读到已不存在的语言（如语言表被删）时不在此校验——
        /// 下拉框会自然回落到首项，查询端也有 en 兜底。</para>
        /// <para>Editor preview language, persisted via EditorPrefs across
        /// domain reloads; defaults to zn.</para>
        /// </summary>
        internal static string PreviewLanguage
        {
            get => EditorPrefs.GetString(PrefsKeyPrefix + "PreviewLanguage", LanguageCode.Zh);
            set
            {
                // 空值不落盘：语言代码是查询主键，空串会让预览全走兜底
                if (string.IsNullOrEmpty(value)) return;
                EditorPrefs.SetString(PrefsKeyPrefix + "PreviewLanguage", value);
            }
        }

        #endregion
    }
}

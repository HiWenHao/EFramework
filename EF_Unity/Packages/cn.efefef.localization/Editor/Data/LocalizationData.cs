/*
 * ================================================
 * Describe:        多语言模块的全局常量、路径与编辑器偏好
 * Author:          Alvin5100
 * CreationTime:    2026-10-14 14:03:21
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-16 18:45:00
 * ScriptVersion:   0.3
 * ================================================
 */

using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Localization.Edit
{
    // 多语言模块的全局常量、路径与编辑器偏好（面板、Inspector、搜索窗共用，避免硬编码与状态散落）
    internal static class LocalizationData
    {
        #region Fields

        // EditorPrefs 键前缀（带包名，避免与工程内其他设置撞名）
        private const string PrefsKeyPrefix = "EF.Localization.";

        // Key 清单文件名（多语言 Key 清单的唯一文件名）
        internal const string FileName = "LocalizationKeys.json";

        // 存放目录（相对工程 Resources；放 Resources 下运行时可直接加载）
        internal const string Folder = "Resources/L10n";

        // value 表目录（相对工程根，非 Resources；供翻译填写的源文件，Luban 导表后产出 bytes 进包）
        internal const string ValuesDir = "EFTools/Datable/Datas";

        // value 表文件名前缀（按语言分文件：{Lang}.csv → {Lang}.bytes）
        internal const string ValuesFilePrefix = "L10n_";

        #endregion

        #region Properties

        // Key 清单 JSON 绝对路径（面板读写的唯一目标文件；不存在时面板会引导创建）
        internal static string JsonPath => Path.Combine(Application.dataPath, Folder, FileName);

        // value 表目录绝对路径（不存在时导出函数会自动创建）
        internal static string ValuesPath
            => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValuesDir));

        // 选择 key 后是否自动把预览译文写入文本框（true：隐藏「应用」按钮，选 key 即自动应用；
        // false（默认）：显示按钮手动应用。设置面板写入、Inspector 读取；写入端只在值变化时赋值，避免每帧重绘落盘）
        internal static bool AutoApplyOnKeySelected
        {
            get => EditorPrefs.GetBool(PrefsKeyPrefix + "AutoApplyOnKeySelected", false);
            set => EditorPrefs.SetBool(PrefsKeyPrefix + "AutoApplyOnKeySelected", value);
        }

        // 编辑器预览语言（Inspector 预览行使用；静态字段会在每次编译域重载后清零，故走 EditorPrefs
        // 持久化保持上次选择，默认 zh；读到已不存在的语言时不在此校验——下拉框自然回落首项，查询端有 en 兜底）
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

        #region Methods

        // 指定语言的 value 表（CSV）绝对路径
        internal static string GetValuesPath(string language)
            => Path.Combine(ValuesPath, $"{ValuesFilePrefix}{language}.csv");

        #endregion
    }
}
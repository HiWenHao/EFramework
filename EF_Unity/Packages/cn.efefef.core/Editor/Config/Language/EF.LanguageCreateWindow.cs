/*
 * ================================================
 * Describe:      新增编辑器语言配置窗口
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2026-10-29 17:50:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-10-30
 * ScriptVersion: 1.1
 * ================================================
 */

using UnityEditor;
using UnityEngine;
using EasyFramework.Edit.Windows;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 新增编辑器语言配置窗口：选择系统语言与显示名，生成 EFTools 扩展语言模板
    /// <para>Window for creating an external editor language: pick a system language and a
    /// display name, then a template is generated under EFTools/EditorLanguage. The system
    /// language doubles as the code, so Auto detection works out of the box.</para>
    /// </summary>
    internal class EFEditorLanguageCreateWindow : EditorWindowBase
    {
        #region Fields

        private SystemLanguage _systemLanguage;     // 语言标识（兼作文件名与 Auto 推导依据）
        private string _displayName = string.Empty; // 下拉框显示名（空缺省用系统语言名）

        #endregion

        #region Methods

        // 校验并生成语言模板，成功后刷新语言列表并定位到新文件
        private void TryCreate()
        {
            if (!LC.CreateLanguageTemplate(_systemLanguage, _displayName, out string error))
            {
                EditorUtility.DisplayDialog(LC.Combine(Lc.Error), error, LC.Combine(Lc.Confirm));
                return;
            }

            LC.ReloadLanguages();
            EditorUtility.RevealInFinder(LC.GetExternalLanguagePath(_systemLanguage.ToString()));
            Close();
        }

        /// <summary>
        /// 打开新增语言窗口
        /// <para>Open the create-language window.</para>
        /// </summary>
        public static void OpenWindow()
        {
            var window = GetWindow<EFEditorLanguageCreateWindow>(true, "新增编辑器语言 / New Editor Language", true);
            window.minSize = new Vector2(400f, 120f);
        }

        /// <summary>
        /// 绘制语言选择表单与创建按钮，Enter 快捷触发创建
        /// <para>Draws the language selection form and the create button; Enter triggers creation.</para>
        /// </summary>
        protected override void OnSmartGUI()
        {
            var selected =
                (SystemLanguage)EditorGUILayout.EnumPopup(
                    LC.Combine(Lc.Select, Lc.System, Lc.Language), _systemLanguage);
            if (selected != _systemLanguage)
            {
                _systemLanguage = selected;
                if (LC.IsBuiltinCoveredLanguage(_systemLanguage))
                {
                    // 中/英已有内置语言表：日志提示后回落到 0 索引
                    D.Warning(LC.Tr(
                        $"[ 语言 ] 已拥有 '{_systemLanguage}' 的内置语言表，选择已回落到列表首项。",
                        $"[ Language ] A builtin language table for '{_systemLanguage}' already exists; selection reset to the first entry."));
                    _systemLanguage = (SystemLanguage)0;
                }
            }

            _displayName = EditorGUILayout.TextField(LC.Combine(Lc.Display, Lc.Name), _displayName);

            EditorGUILayout.Space(6f);
            if (GUILayout.Button(LC.Combine(Lc.Create), GUILayout.Height(26f)))
                TryCreate();

            if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)
            {
                TryCreate();
                Event.current.Use();
            }
        }

        #endregion
    }
}
/*
 * ================================================
 * Describe:      新增编辑器语言配置窗口
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2026-10-29 17:50:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-10-29
 * ScriptVersion: 1.0
 * ================================================
 */

using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 新增编辑器语言配置窗口：输入语言代码与显示名，生成 EFTools 扩展语言模板
    /// <para>Window for creating an external editor language: enter code and display name,
    /// then a template is generated under EFTools/EditorLanguage.</para>
    /// </summary>
    internal class EFEditorLanguageCreateWindow : EditorWindow
    {
        #region Fields

        private string _code = string.Empty; // 语言代码（即模板文件名）
        private string _displayName = string.Empty; // 下拉框显示名
        private SystemLanguage _systemLanguage = SystemLanguage.Chinese; // 映射的系统语言

        #endregion

        #region Methods

        /// <summary>
        /// 打开新增语言窗口
        /// <para>Open the create-language window.</para>
        /// </summary>
        public static void OpenWindow()
        {
            var window = GetWindow<EFEditorLanguageCreateWindow>(true, "新增编辑器语言 / New Editor Language", true);
            window.minSize = new Vector2(400f, 150f);
        }

        private void OnGUI()
        {
            _code = EditorGUILayout.TextField(
                new GUIContent("语言代码 / Code", "字母、数字、下划线，不超过 16 位。例如：ja"), _code);
            _displayName = EditorGUILayout.TextField(
                new GUIContent("显示名 / Display Name", "下拉框中显示的语言名称。例如：日本語"), _displayName);

            EditorGUILayout.LabelField(
                new GUIContent("对应系统语言 / Sync SystemLanguage",
                    "对应后 Auto 模式可按系统语言自动匹配，并同步运行时覆盖值"));
            _systemLanguage = (SystemLanguage)EditorGUILayout.EnumPopup(_systemLanguage);

            EditorGUILayout.Space(6f);
            if (GUILayout.Button(LC.Combine(Lc.Create), GUILayout.Height(26f)))
                TryCreate();

            if (!Event.current.isKey || Event.current.keyCode != KeyCode.Return) return;
            TryCreate();
            Event.current.Use();
        }

        /// <summary>
        /// 校验并生成语言模板；成功后刷新语言列表并定位到新文件
        /// <para>Validate and create the template; reloads languages and reveals the file on success.</para>
        /// </summary>
        private void TryCreate()
        {
            if (!LC.CreateLanguageTemplate(_code, _displayName, $"{_systemLanguage}", out string error))
            {
                EditorUtility.DisplayDialog(LC.Combine(Lc.Error), error, LC.Combine(Lc.Confirm));
                return;
            }

            LC.ReloadLanguages();
            EditorUtility.RevealInFinder(LC.GetExternalLanguagePath(_code.Trim()));
            Close();
        }

        #endregion
    }
}
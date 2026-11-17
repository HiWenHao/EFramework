/*
 * ================================================
 * Describe:      自定义吉祥物素材窗口：在 EFTools 下生成素材目录与说明文件
 * Author:        WorkBuddy
 * CreationTime:  2026-11-17 17:55:00
 * ScriptVersion: 1.0
 * ===============================================
 */

using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.Windows
{
    /// <summary>
    /// 自定义吉祥物素材窗口：输入素材名称后，在 EFTools/Mascots 下生成目录与说明文件。
    /// <para>Window for creating a custom mascot asset folder under EFTools/Mascots.</para>
    /// </summary>
    internal class EFMascotCreateWindow : EditorWindowBase
    {
        #region 字段 / Fields

        private string _mascotName = string.Empty; // 素材名称（用作目录名）

        #endregion

        #region 方法 / Methods

        /// <summary>
        /// 校验并生成素材模板，成功后定位到新目录
        /// <para>Validate and create the asset template, then reveal the new folder.</para>
        /// </summary>
        private void TryCreate()
        {
            if (!EFMascotConfig.CreateMascotTemplate(_mascotName, out string error))
            {
                EditorUtility.DisplayDialog("EF", error, LC.Combine("Confirm"));
                return;
            }

            EditorUtility.RevealInFinder(EFMascotConfig.GetCustomDirectory(_mascotName));
            Close();
        }

        /// <summary>
        /// 打开自定义素材窗口
        /// <para>Open the create-mascot window.</para>
        /// </summary>
        public static void OpenWindow()
        {
            var window = GetWindow<EFMascotCreateWindow>(true, LC.Combine("Core.MascotCreateWindow"), true);
            window.minSize = new Vector2(420f, 150f);
        }

        /// <summary>
        /// 绘制素材表单与生成按钮
        /// <para>Draws the mascot form and the create button.</para>
        /// </summary>
        protected override void OnSmartGUI()
        {
            EditorGUILayout.HelpBox(
                LC.Tr(
                    $"将在工程 {EFMascotConfig.CustomFolderRelativePath} 下生成素材目录，并附带说明文件。\n" +
                    $"帧数自定：放几张 PNG 就播几帧循环，文件名须以数字结尾并从 0 连续编号（例 {EFMascotConfig.FileNameFormat}）。\n" +
                    $"建议尺寸 {EFMascotConfig.RecommendedSize}×{EFMascotConfig.RecommendedSize} 像素，透明背景。",
                    $"A mascot asset folder will be created under {EFMascotConfig.CustomFolderRelativePath} with a readme file.\n" +
                    $"Frame count is up to you: put N PNGs and it plays N frames in a loop. File names must end with an index starting from 0 (e.g. {EFMascotConfig.FileNameFormat}).\n" +
                    $"Recommended size: {EFMascotConfig.RecommendedSize}x{EFMascotConfig.RecommendedSize} pixels with a transparent background."),
                MessageType.Info);

            _mascotName = EditorGUILayout.TextField(LC.Combine("Core.MascotName"), _mascotName);

            EditorGUILayout.Space(6f);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_mascotName)))
            {
                if (GUILayout.Button(LC.Combine("Core.MascotCreate"), GUILayout.Height(26f)))
                    TryCreate();
            }
        }

        #endregion
    }
}

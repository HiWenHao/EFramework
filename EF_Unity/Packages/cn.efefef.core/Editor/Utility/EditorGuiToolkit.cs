/*
 * ================================================
 * Describe:        通用工具GUI函数放这里，方便引擎开发者直接调用
 * Author:          Alvin8412
 * CreationTime:    2026-07-14 18:26:44
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 18:56:36
 * ScriptVersion:   0.2
 * ================================================
 */

using System.IO;
using EasyFramework.Edit.Windows;
using UnityEngine;
using UnityEditor;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 编辑器下GUI相关工具包
    /// </summary>
    public static class EditorGuiToolkit
    {
        private const float MaxWidth = 220f;
        private const float CopyPathWidth = 40f;
        private const float OpenPathWidth = 40f;
        private const float PathSelectWidth = 140f;
        private const string AssetsRoot = "Assets";

        /// <summary>
        /// 选择 Assets 目录下的文件夹
        /// </summary>
        /// <param name="label">选择框上方的提示文本</param>
        /// <param name="selectPath">选择的地址</param>
        /// <param name="showCopy">显示复制按钮</param>
        /// <param name="showOpen">显示打开按钮</param>
        /// <returns>本次是否成功重新选择了 Assets 内的文件夹</returns>
        public static bool SelectionFolderPathInAssets(string label, ref string selectPath, bool showCopy = true, bool showOpen = true)
        {
            EditorGUILayout.LabelField(label, GUIUtils.ColorText(textColor: Color.white));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(selectPath);

            string initialDir = Application.dataPath;
            if (!string.IsNullOrEmpty(selectPath))
            {
                string rel = selectPath;
                if (rel.StartsWith(AssetsRoot + "/"))
                    rel = rel[(AssetsRoot + "/").Length..];
                else if (rel.Equals(AssetsRoot))
                    rel = "";

                if (!string.IsNullOrEmpty(rel))
                    initialDir = Path.Combine(Application.dataPath, rel.TrimEnd('/', '\\'));
            }

            bool changed = SelectPathButton(initialDir, out string picked, PathSelectWidth);
            if (changed && !string.IsNullOrEmpty(picked))
            {
                string dataPath = Application.dataPath.Replace('\\', '/');
                string normalized = picked.Replace('\\', '/');
                if (!normalized.StartsWith(dataPath, System.StringComparison.OrdinalIgnoreCase))
                {
                    D.Warning($"[ EditorGuiToolkit ] 选择的文件夹不在 Assets 目录下，已忽略：{picked}");
                }
                else
                {
                    selectPath = AssetsRoot + normalized[dataPath.Length..].TrimEnd('/') + "/";
                    EditorCommands.SaveAssets();
                    EditorCommands.Refresh();
                }
            }

            if (showCopy) CopyButton(LC.Combine(Lc.Copy), selectPath);
            if (showOpen) OpenFolderButton(selectPath);

            EditorGUILayout.EndHorizontal();
            return changed;
        }

        /// <summary>
        /// 随意选择任意文件夹，selectPath 使用绝对路径。
        /// </summary>
        /// <param name="label">选择框上方的提示文本</param>
        /// <param name="selectPath">选择的地址</param>
        /// <param name="showCopy">显示复制按钮</param>
        /// <param name="showOpen">显示打开按钮</param>
        /// <returns>本次是否成功重新选择了文件夹</returns>
        public static bool SelectionFolderPath(string label, ref string selectPath, bool showCopy = true, bool showOpen = true)
        {
            EditorGUILayout.LabelField(label, GUIUtils.ColorText(textColor: Color.white));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(selectPath);

            string initialDir = Path.IsPathRooted(selectPath) ? selectPath : "";

            bool changed = SelectPathButton(initialDir, out string picked, showCopy ? PathSelectWidth : MaxWidth);
            if (changed && !string.IsNullOrEmpty(picked))
            {
                selectPath = picked.Replace('\\', '/');
            }

            if (showCopy) CopyButton(LC.Combine(Lc.Copy), selectPath);
            if (showOpen) OpenFolderButton(selectPath);

            EditorGUILayout.EndHorizontal();
            return changed;
        }

        /// <summary>
        /// 在系统文件管理器中打开指定绝对路径的文件夹
        /// </summary>
        /// <param name="absolutePath">要打开的文件夹绝对路径</param>
        /// <param name="width">按钮宽度</param>
        public static void OpenFolderButton(string absolutePath, float width = OpenPathWidth)
        {
            if (GUILayout.Button(LC.Combine(Lc.Open), GUILayout.Width(width)))
            {
                if (string.IsNullOrEmpty(absolutePath))
                {
                    D.Warning("[ EditorGuiToolkit ] 路径为空，无法打开文件夹。");
                }
                else if (!Directory.Exists(absolutePath) && !File.Exists(absolutePath))
                {
                    D.Warning($"[ EditorGuiToolkit ] 路径不存在，无法打开：{absolutePath}");
                }
                else
                {
                    EditorUtility.RevealInFinder(absolutePath);
                }
            }
        }

        /// <summary>
        /// GUI 路径选择按钮
        /// </summary>
        /// <param name="folder">已有路径</param>
        /// <param name="selectPath">被选择的路径</param>
        /// <param name="width">按钮宽度</param>
        /// <returns></returns>
        public static bool SelectPathButton(string folder, out string selectPath, float width)
        {
            if (GUILayout.Button(LC.Combine(Lc.Path, Lc.Select), GUILayout.Width(width)))
            {
                selectPath = EditorUtility.OpenFolderPanel(LC.Combine(Lc.Path, Lc.Select), folder, "");
                return true;
            }

            selectPath = string.Empty;
            return false;
        }

        /// <summary>
        /// GUI 复制按钮
        /// </summary>
        /// <param name="label">按钮文本</param>
        /// <param name="selectPath">要复制的文本</param>
        /// <param name="width">按钮宽度</param>
        public static void CopyButton(string label, string selectPath, float width = CopyPathWidth)
        {
            if (!GUILayout.Button(label, GUILayout.Width(width)))
                return;

            if (!string.IsNullOrEmpty(selectPath))
                EditorGUIUtility.systemCopyBuffer = selectPath;
        }
    }
}
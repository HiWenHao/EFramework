/*
 * ================================================
 * Describe:      This script is used to copy the file info.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-24 20:55:56
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-01 14:50:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NPinyin;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyFramework.Edit.MenuToolkit
{
    /// <summary>
    /// Copy the file info
    /// </summary>
    internal static class FileInformationToolkit
    {
        #region Copy

        [MenuItem("Assets/EF/File/Copy GUID", false, 1)]
        private static void CopySelectedGuid()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\n");
            foreach (var obj in Selection.objects)
            {
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long localId))
                    continue;

                sb.AppendLine(AssetDatabase.IsMainAsset(obj) ? $"{obj}" : $"{guid}-{localId}");
            }

            var te = new TextEditor()
            {
                text = sb.ToString()
            };
            te.SelectAll();
            te.Copy();
        }

        [MenuItem("Assets/EF/File/Copy Relative Path", false, 2)]
        private static void CopyFileAssetsPath()
        {
            var assetNames = Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath);

            var te = new TextEditor()
            {
                text = string.Join("\n", assetNames.Distinct())
            };
            te.SelectAll();
            te.Copy();
        }

        [MenuItem("Assets/EF/File/Copy Absolute Path", false, 3)]
        private static void CopyFileDiskPath()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);

            var assetNames = Selection.assetGUIDs
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(path => path.Replace('/', Path.DirectorySeparatorChar))
                    .Select(path => projectRoot + Path.DirectorySeparatorChar + path)
                ;

            var te = new TextEditor()
            {
                text = string.Join("\n", assetNames.Distinct())
            };
            te.SelectAll();
            te.Copy();
        }

        #endregion

        #region Rename

        /// <summary>
        /// Quickly change Chinese to Pinyin
        /// </summary>
        internal static class FileToolkit
        {
            [MenuItem("Assets/EF/File/Rename To Pinyin", false, 21)]
            private static void RenameAll()
            {
                Object[] selection = Selection.GetFiltered<Object>(SelectionMode.DeepAssets);

                int count = selection.Length;
                for (int i = 0; i < count; i++)
                {
                    Object obj = selection[i];
                    if (!obj.name.IsChinese())
                        continue;

                    string pinyin = Pinyin.GetPinyin(obj.name);
                    pinyin = pinyin.Replace(" ", "");

                    AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(obj), pinyin);
                }

                AssetDatabase.Refresh();
            }
        }

        #endregion

        #region Assets

        /// <summary>
        /// Displays the memory occupied by file contents
        /// </summary>
        internal static class ShowFileSize
        {
            private const string RemoveStr = "Assets";
            private const string Filesize = "FileSize";

            private static readonly int RemoveCount = RemoveStr.Length;
            private static readonly Color ProfessionalColor = new Color(56f / 255, 56f / 255, 56f / 255, 1);
            private static readonly Color PersonalColor = new Color(194f / 255, 194f / 255, 194f / 255, 1);
            private static readonly Dictionary<string, long> DirSizeDictionary = new Dictionary<string, long>();
            private static readonly List<string> DirList = new List<string>();
            private static bool _isShowSize = true;

            /// <summary>
            /// 获取皮肤 —— 首次通过反射读取并缓存，后续直接在 OnGUI 返回
            /// <para>Detect dark skin — caches the reflection result after first call to avoid per-frame overhead</para>
            /// </summary>
            private static bool? _cachedUseDark;

            [MenuItem(MenuItemToolkit.Tools + "💾 Project File Size", false, MenuItemToolkit.ToolsPriority + 10)]
            private static void OpenPlaySize()
            {
                _isShowSize = !_isShowSize;
                EditorPrefs.SetBool(Filesize, _isShowSize);
                GetProjectDirs();
                AssetDatabase.Refresh();
            }

            [InitializeOnLoadMethod]
            private static void InitializeOnLoadMethod()
            {
                EditorApplication.projectChanged += GetProjectDirs;
                EditorApplication.projectWindowItemOnGUI += OnGUI;
                GetProjectDirs();
            }

            private static void GetProjectDirs()
            {
                Init();
                if (!_isShowSize) return;
                GetAllDirectories(Application.dataPath);
                foreach (string path in DirList)
                {
                    string newPath = path.Replace("\\", "/");
                    DirSizeDictionary[newPath] = GetDirectoriesSize(path);
                }
            }

            private static void Init()
            {
                _isShowSize = EditorPrefs.GetBool(Filesize);
                DirSizeDictionary.Clear();
                DirList.Clear();
            }

            private static void OnGUI(string guid, Rect selectionRect)
            {
                if (!_isShowSize || selectionRect.height > 16) return;
                var dataPath = Application.dataPath;
                var startIndex = dataPath.LastIndexOf(RemoveStr, StringComparison.Ordinal);
                var dir = dataPath.Remove(startIndex, RemoveCount);
                var path = dir + AssetDatabase.GUIDToAssetPath(guid);
                string text;

                long fileSize;
                if (DirSizeDictionary.TryGetValue(path, out var value))
                    fileSize = value;
                else if (File.Exists(path))
                    fileSize = new FileInfo(path).Length;
                else return;

                text = GetFormatSizeString((int)fileSize);

                var label = EditorStyles.label;
                var content = new GUIContent(text);
                var width = label.CalcSize(content).x + 10;

                var pos = selectionRect;
                pos.x = pos.xMax - width;
                pos.width = width;

                EditorGUI.DrawRect(pos, UseDark() ? ProfessionalColor : PersonalColor);
                Color defaultC = GUI.color;

                GUI.color = fileSize switch
                {
                    > 1024 * 1024 * 10 => Color.red,
                    > 1024 * 1024 => Color.yellow,
                    _ => GUI.color
                };

                GUI.Label(pos, text);
                GUI.color = defaultC;
            }

            private static bool UseDark()
            {
                if (_cachedUseDark.HasValue)
                    return _cachedUseDark.Value;

                PropertyInfo propertyInfo = typeof(EditorGUIUtility).GetProperty("skinIndex",
                    BindingFlags.Static | BindingFlags.NonPublic);
                _cachedUseDark = propertyInfo != null && (int)propertyInfo.GetValue(null) == 1;
                return _cachedUseDark.Value;
            }

            private static string GetFormatSizeString(int size)
            {
                string[] ns = new[] { "Byte", "KB", "MB", "GB", "TB", "PB" };
                if (size <= 0)
                    return $"{0:F2} {ns[0]}";

                const double baseNum = 1024;
                int pow = Math.Min((int)Math.Floor(Math.Log(size, baseNum)), ns.Length - 1);

                return $"{size / Math.Pow(baseNum, pow):F2} {ns[pow]}";
            }

            private static void GetAllDirectories(string dirPath)
            {
                if (!Directory.Exists(dirPath)) return;

                DirList.Add(dirPath);
                DirectoryInfo[] dirArray = new DirectoryInfo(dirPath).GetDirectories();
                foreach (DirectoryInfo item in dirArray)
                {
                    GetAllDirectories(item.FullName);
                }
            }

            private static long GetDirectoriesSize(string dirPath)
            {
                if (!Directory.Exists(dirPath)) return 0;

                long size = 0;
                DirectoryInfo dir = new DirectoryInfo(dirPath);
                foreach (FileInfo info in dir.GetFiles())
                {
                    size += info.Length;
                }

                DirectoryInfo[] dirBotton = dir.GetDirectories();
                foreach (DirectoryInfo info in dirBotton)
                {
                    size += GetDirectoriesSize(info.FullName);
                }

                return size;
            }
        }

        #endregion
    }
}
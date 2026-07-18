/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-03-31 15:56:54
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-01 15:10:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 工具类
    /// <para>Utility helpers</para>
    /// </summary>
    public class Utility
    {
        /// <summary>
        /// 路径相关的实用函数。
        /// <para>Path-related utility functions</para>
        /// </summary>
        public static class Path
        {
            /// <summary>
            /// 获取规范的路径。
            /// <para>Get a normalized path with forward slashes</para>
            /// </summary>
            public static string GetRegularPath(string path)
            {
                return path?.Replace('\\', '/');
            }

            /// <summary>
            /// 获取远程格式的路径（带有 file:// 或 http:// 前缀）。
            /// <para>Get the path in remote format (with file:// or http:// prefix)</para>
            /// </summary>
            public static string GetRemotePath(string path)
            {
                string regularPath = GetRegularPath(path);
                if (string.IsNullOrEmpty(regularPath)) return null;

                return regularPath.Contains("://")
                    ? regularPath
                    : ("file:///" + regularPath).Replace("file:////", "file:///");
            }

            /// <summary>
            /// 移除空文件夹。
            /// <para>Remove empty directories recursively</para>
            /// </summary>
            public static bool RemoveEmptyDirectory(string directoryName)
            {
                if (string.IsNullOrEmpty(directoryName))
                {
                    throw new System.Exception("Directory name is invalid.");
                }

                try
                {
                    if (!Directory.Exists(directoryName))
                    {
                        return false;
                    }

                    // 不使用 SearchOption.AllDirectories，以便于在可能产生异常的环境下删除尽可能多的目录
                    string[] subDirectoryNames = Directory.GetDirectories(directoryName, "*");
                    int subDirectoryCount = subDirectoryNames.Length;
                    foreach (string subDirectoryName in subDirectoryNames)
                    {
                        if (RemoveEmptyDirectory(subDirectoryName))
                        {
                            subDirectoryCount--;
                        }
                    }

                    if (subDirectoryCount > 0)
                    {
                        return false;
                    }

                    if (Directory.GetFiles(directoryName, "*").Length > 0)
                    {
                        return false;
                    }

                    Directory.Delete(directoryName);
                    return true;
                }
                catch (System.Exception ex)
                {
                    D.Warning($"[Utility] RemoveEmptyDirectory failed for '{directoryName}': {ex.Message}");
                    return false;
                }
            }

            /// <summary>
            /// Gets the framework path
            /// <para>获取框架路径</para>
            /// </summary>
            public static string GetEfPath()
            {
                return "Packages/cn.efefef.core";
            }

            /// <summary>
            /// Gets the asset path associated with the framework
            /// <para>获取框架的相关资产路径</para>
            /// </summary>
            public static string GetEfAssetsPath()
            {
                return "Packages/cn.efefef.core/Editor Resources";
            }

            public static string GetCurrentFolderPath()
            {
                string[] guids = Selection.assetGUIDs;

                if (guids == null || guids.Length == 0) return null;

                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                string folderPath = Directory.Exists(assetPath)
                    ? assetPath
                    : System.IO.Path.GetDirectoryName(assetPath);

                return folderPath;
            }

            /// <summary>
            /// 获取在资源文件夹下的路径
            /// <para>Get the path under the Assets folder</para>
            /// </summary>
            public static string GetPathInAssetsFolder(string path)
            {
                string endPath = Application.dataPath;
                if (string.IsNullOrEmpty(path)) return endPath;
                int index = path.IndexOf("/Assets", System.StringComparison.Ordinal);
                if (index == -1)
                {
                    EditorUtility.DisplayDialog("提示", $"必须在Assets目录下", "确定");
                    return endPath;
                }

                endPath = path[(index + 1)..];
                return endPath;
            }
        }
    }
}

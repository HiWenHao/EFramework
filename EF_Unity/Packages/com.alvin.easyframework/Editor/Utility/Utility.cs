/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-03-31 15:56:54
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-03-31 15:56:54
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework
{
    /// <summary>
    /// 工具类
    /// </summary>
    public class Utility
    {
        /// <summary>
        /// 路径相关的实用函数。
        /// </summary>
        public static class Path
        {
            /// <summary>
            /// 获取规范的路径。
            /// </summary>
            /// <param name="path">要规范的路径。</param>
            /// <returns>规范的路径。</returns>
            public static string GetRegularPath(string path)
            {
                if (path == null)
                {
                    return null;
                }

                return path.Replace('\\', '/');
            }

            /// <summary>
            /// 获取远程格式的路径（带有file:// 或 http:// 前缀）。
            /// </summary>
            /// <param name="path">原始路径。</param>
            /// <returns>远程格式路径。</returns>
            public static string GetRemotePath(string path)
            {
                string regularPath = GetRegularPath(path);
                if (regularPath == null)
                {
                    return null;
                }

                return regularPath.Contains("://")
                    ? regularPath
                    : ("file:///" + regularPath).Replace("file:////", "file:///");
            }

            /// <summary>
            /// 移除空文件夹。
            /// </summary>
            /// <param name="directoryName">要处理的文件夹名称。</param>
            /// <returns>是否移除空文件夹成功。</returns>
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
                catch
                {
                    return false;
                }
            }

            /// <summary>
            /// Gets the framework path
            /// <para>获取框架路径</para>
            /// </summary>
            public static string GetEfPath()
            {
                return @"Packages\com.alvin.easyframework";
            }

            /// <summary>
            /// Gets the asset path associated with the framework
            /// <para>获取框架的相关资产路径</para>
            /// </summary>
            public static string GetEfAssetsPath()
            {
                return @"Packages\com.alvin.easyframework\Editor Resources";
            }

            public static string GetCurrentFolderPath()
            {
                string[] guids = Selection.assetGUIDs;

                // 如果没有选中任何东西，则提示并返回
                if (guids == null || guids.Length == 0)
                {
                    Debug.LogWarning("没有选中任何文件或文件夹。");
                    return null;
                }

                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);

                string folderPath = Directory.Exists(assetPath)
                    ? assetPath
                    : System.IO.Path.GetDirectoryName(assetPath);

                return folderPath;
            }
        }

        /// <summary>
        /// 资产文件下下的路径相关实用函数
        /// </summary>
        public static class AssetPath
        {
            /// <summary>
            /// 获取在资源文件夹下的路径
            /// </summary>
            public static string GetPathInAssetsFolder(string path)
            {
                string endPath = Application.dataPath;
                if (!string.IsNullOrEmpty(path))
                {
                    int index = path.IndexOf("/Assets", System.StringComparison.Ordinal);
                    if (index == -1)
                    {
                        EditorUtility.DisplayDialog("提示", $"必须在Assets目录下", "确定");
                        return endPath;
                    }

                    endPath = path[(index + 1)..];
                }

                return endPath;
            }
        }

        public static class RefreshUtility
        {
            /// <summary>
            /// 执行刷新并在完成后执行回调
            /// </summary>
            public static void RefreshWithCallback(System.Action onComplete)
            {
                if (onComplete == null)
                {
                    AssetDatabase.Refresh();
                    return;
                }

                // 保存资源
                AssetDatabase.SaveAssets();

                // 开始刷新
                AssetDatabase.Refresh();

                // 启动协程式检查
                CheckRefreshCompletion(onComplete);
            }

            static void CheckRefreshCompletion(System.Action onComplete)
            {
                // 创建一个临时编辑器窗口来执行更新检查
                var updater = ScriptableObject.CreateInstance<RefreshCompletionChecker>();
                updater.StartChecking(onComplete);
            }

            // 辅助类：使用 HideAndDontSave 避免污染场景
            class RefreshCompletionChecker : ScriptableObject
            {
                private System.Action callback;
                private float checkInterval = 0.5f;
                private double nextCheckTime;

                public void StartChecking(System.Action onComplete)
                {
                    callback = onComplete;
                    nextCheckTime = EditorApplication.timeSinceStartup + checkInterval;

                    // 订阅更新事件
                    EditorApplication.update += OnEditorUpdate;

                    // 确保这个对象不会被保存
                    hideFlags = HideFlags.HideAndDontSave;
                }

                void OnEditorUpdate()
                {
                    // 控制检查频率
                    if (EditorApplication.timeSinceStartup < nextCheckTime)
                        return;

                    nextCheckTime = EditorApplication.timeSinceStartup + checkInterval;

                    // 检查是否完成
                    if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
                    {
                        // 刷新完成
                        EditorApplication.update -= OnEditorUpdate;

                        // 延迟一帧执行回调，确保一切稳定
                        EditorApplication.delayCall += () =>
                        {
                            callback?.Invoke();
                            DestroyImmediate(this);
                        };
                    }
                }
            }
        }
    }
}
/* 
 * ================================================
 * Describe:        This script is used to .
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2023-04-20 19:57:04
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-06-01 15:40:00
 * ScriptVersion:   0.2
 * ===============================================
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyFramework.Edit
{
    /// <summary>
    /// The editor folder utils.
    /// <para>编辑器工具集</para>
    /// </summary>
    public static class EditorUtils
    {
        #region Load

        /// <summary>
        /// 检查指定类型的 ScriptableObject 资产是否存在
        /// <para>Check whether a ScriptableObject asset of the specified type exists in the project</para>
        /// </summary>
        /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
        /// <param name="assetPath">匹配到的资产路径<para>Matched asset path</para></param>
        /// <returns>是否存在<para>Whether the asset exists</para></returns>
        public static bool CheckAssets<T>(out string assetPath) where T : ScriptableObject
        {
            string assetType = typeof(T).Name;
            string[] globalAssetPaths = AssetDatabase.FindAssets($"t:{assetType}");

            if (globalAssetPaths.Length == 0)
            {
                assetPath = "";
                return false;
            }

            if (globalAssetPaths.Length > 1)
            {
                foreach (var path in globalAssetPaths)
                {
                    D.Error($"Not allow has multi type of {assetType}. Path is [ {AssetDatabase.GUIDToAssetPath(path)} ] ");
                }
            }

            assetPath = AssetDatabase.GUIDToAssetPath(globalAssetPaths[0]);
            return true;
        }

        /// <summary>
        /// 根据类型全局查找并加载 ScriptableObject 面板
        /// <para>Find and load a ScriptableObject asset by type globally</para>
        /// </summary>
        /// <typeparam name="T">面板类型<para>Panel type</para></typeparam>
        /// <returns>资产实例，找不到则返回 null<para>Asset instance, or null if not found</para></returns>
        public static T LoadSettingAtPath<T>() where T : ScriptableObject, new()
        {
            string[] paths = AssetDatabase.FindAssets($"t:{typeof(T)}");
            if (paths.Length == 0)
            {
                D.Error($"Type of [ {typeof(T).Name} ] not exist in your project, please create it manually.");
                return null;
            }

            if (paths.Length > 1)
            {
                D.Error($"{typeof(T).Name} 数量大于1");
            }

            string path = AssetDatabase.GUIDToAssetPath(paths[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        #endregion

        #region Get files path

        /// <summary>
        /// 获取当前选中资源的路径
        /// <para>Get the path of the currently selected asset</para>
        /// </summary>
        /// <returns>资源路径，无选中时返回 "Assets"<para>Asset path, or "Assets" if nothing is selected</para></returns>
        public static string GetSelectFilePath()
        {
            Object selectedObject = Selection.activeObject;
            return selectedObject == null ? "Assets" : AssetDatabase.GetAssetPath(selectedObject);
        }

        /// <summary>
        /// 获取当前所有选中资源的路径列表（已过滤无效 GUID）
        /// <para>Get the paths of all currently selected assets (invalid GUIDs are filtered out)</para>
        /// </summary>
        /// <returns>有效资源路径数组<para>Valid asset path array</para></returns>
        public static string[] GetSelectFilesPath()
        {
            string[] guids = Selection.assetGUIDs;
            var resultList = new List<string>(guids.Length);

            foreach (var t in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(t);
                if (!string.IsNullOrEmpty(path))
                    resultList.Add(path);
            }

            return resultList.ToArray();
        }

        #endregion

        #region String

        private static readonly Regex PunctuationRegex = new(@"[\p{P}\p{S}×――—￥…•`· ]+", RegexOptions.Compiled);

        /// <summary>
        /// 删除标点符号（使用编译缓存的正则）
        /// <para>Remove punctuation using a compiled cached regex</para>
        /// </summary>
        public static string RemovePunctuation(string str)
        {
            return PunctuationRegex.Replace(str, "");
        }

        /// <summary>
        /// 按字符串长度在有序列表中二分查找插入位置
        /// <para>Find an insertion index in a list ordered by string length</para>
        /// </summary>
        /// <param name="nameLength">目标字符串长度<para>Target string length</para></param>
        /// <param name="strList">已排序的字符串列表<para>Sorted string list</para></param>
        /// <param name="startIndex">搜索起始索引<para>Search start index</para></param>
        /// <param name="endIndex">搜索结束索引<para>Search end index</para></param>
        /// <returns>应插入的位置索引<para>Insertion position index</para></returns>
        public static int GetIndexWithLengthSort(int nameLength, List<string> strList, int startIndex, int endIndex)
        {
            if (endIndex < 0)
                return 0;

            int endIdx = Math.Min(endIndex, strList.Count);
            if (endIdx < strList.Count)
            {
                D.Warning("The parameter [ endIndex ] greater than array length, will be limited.");
            }

            for (int i = startIndex; i < endIdx; i++)
            {
                if (nameLength < strList[i].Length)
                    return i;
            }
            return endIdx;
        }

        /// <summary>
        /// 对比版本号（格式: x.x.x，纯数字）
        /// <para>Compare two version strings (format: x.x.x, digits only)</para>
        /// </summary>
        /// <param name="version1">版本号1<para>Version 1</para></param>
        /// <param name="version2">版本号2<para>Version 2</para></param>
        /// <returns>version1 > version2 时返回 true<para>True if version1 is greater than version2</para></returns>
        public static bool CompareVersion(string version1, string version2)
        {
            if (Version.TryParse(version1, out Version ver1) && Version.TryParse(version2, out Version ver2))
            {
                return ver1 > ver2;
            }

            D.Error($"Version info is error, version1 = {version1},  version2 = {version2}");
            return false;
        }

        #endregion

        #region IO

        /// <summary>
        /// Copy the folder to a certain path.
        /// <para>复制文件夹到目标路径</para>
        /// </summary>
        /// <param name="sourceDir">源路径<para>Source directory</para></param>
        /// <param name="destDir">目标路径<para>Destination directory</para></param>
        /// <param name="overwrite">是否覆盖已有文件<para>Overwrite existing files</para></param>
        public static void CopyFolder(string sourceDir, string destDir, bool overwrite = true)
        {
            try
            {
                if (!Directory.Exists(sourceDir))
                {
                    D.Error($"Source directory not found: {sourceDir}");
                    return;
                }

                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                foreach (string file in Directory.GetFiles(sourceDir))
                {
                    string desFile = Path.Combine(destDir, Path.GetFileName(file));
                    File.Copy(file, desFile, overwrite);
                }

                foreach (string subDir in Directory.GetDirectories(sourceDir))
                {
                    string desSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                    CopyFolder(subDir, desSubDir, overwrite);
                }
            }
            catch (Exception ex)
            {
                D.Error($"Copy folder failed: {sourceDir} → {destDir}, {ex.Message}");
            }
        }

        #endregion

        #region Assembly

        /// <summary>
        /// 根据名称获取已加载的程序集
        /// <para>Get a loaded assembly by name</para>
        /// </summary>
        /// <param name="assemblyName">程序集名<para>Assembly name</para></param>
        /// <returns>程序集实例，未找到则返回 null<para>Assembly instance, or null if not found</para></returns>
        public static Assembly GetAssembly(string assemblyName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (assembly.GetName().Name == assemblyName)
                {
                    return assembly;
                }
            }

            D.Error($"Not found this assembly with name: {assemblyName}");
            return null;
        }

        #endregion

        #region Time

        /// <summary>
        /// 获取当前 Unix 时间戳（秒）
        /// <para>Get the current Unix timestamp in seconds</para>
        /// </summary>
        public static long GetCurrentTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// 给定的时间戳是否已经超过当前时间指定秒数
        /// <para>Check whether the given timestamp has exceeded the current time by the specified number of seconds</para>
        /// </summary>
        /// <param name="timestamp">目标时间戳（秒）<para>Target timestamp in seconds</para></param>
        /// <param name="seconds">超时秒数<para>Timeout duration in seconds</para></param>
        /// <returns>当前时间 > timestamp + seconds 返回 true<para>True if current time exceeds timestamp + seconds</para></returns>
        public static bool TimestampIsExceeded(long timestamp, int seconds)
        {
            long now = GetCurrentTimestamp();
            return now > timestamp + seconds;
        }

        #endregion
    }
}

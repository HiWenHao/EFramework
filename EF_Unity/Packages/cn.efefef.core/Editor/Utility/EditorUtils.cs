/* 
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-20 19:57:04
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-01 14:59:28
 * ScriptVersion: 0.1
 * ===============================================
*/

using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyFramework.Edit
{
    /// <summary>
    /// The editor folder utils.
    /// </summary>
    public static class EditorUtils
	{
        #region Load
        /// <summary>
        /// 检查资源资产
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="assetPath">对应资产在编辑器下的路径</param>
        public static bool CheckAssets<T>(out string assetPath) where T : ScriptableObject
        {
            string assetType = typeof(T).Name;
            string[] globalAssetPaths = AssetDatabase.FindAssets($"t:{assetType}");
#if UNITY_EDITOR
            if (globalAssetPaths.Length == 0)
            {
                //D.Warning($"Your need create one ScriptableObject type of [ {assetType} ] in your project...");
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
#endif
            return true;
        }

        /// <summary>
        /// 根据路径加载设置面板
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
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
        /// 获取当前文件路径
        /// </summary>
        /// <returns>文件路径</returns>
        public static string GetSelectFilePath()
        {
            Object selectedObject = Selection.activeObject;
            return selectedObject == null ? "Assets" : AssetDatabase.GetAssetPath(selectedObject);
        }
        
        /// <summary>
        /// 获取当前所选择的文件路径
        /// </summary>
        /// <returns>所选择的文件路径</returns>
        public static string[] GetSelectFilesPath()
        {
            string[] guids = Selection.assetGUIDs;
            string[] paths = new string[guids.Length];
            for (var i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.IsNullOrEmpty(path))
                    paths[i]= path;
            }
            return paths;
        }

        #endregion
        
        #region String
        /// <summary>
        /// 删除标点符号
        /// </summary>
        public static string RemovePunctuation(string str)
        {
            return Regex.Replace(str, "[ \\[ \\] \\^ \\-*×――(^)（）{}/【】$%~!@#$…&%￥—+=<>《》!！??？:：•`·、。，；,.;'\"‘’“”-]", "");
        }

        /// <summary>
        /// 按字符长度排序
        /// </summary>
        /// <param name="nameLength">名字长度</param>
        /// <param name="strList">要对比的名字列表</param>
        /// <param name="startIndex">起始索引</param>
        /// <param name="endIndex">结束索引</param>
        /// <returns>所在位置的索引值</returns>
        public static int GetIndexWithLengthSort(int nameLength, System.Collections.Generic.List<string> strList, int startIndex, int endIndex)
        {
            if (endIndex < 0)
                return 0;

            int endIdx = endIndex;
            if (strList.Count < endIndex)
            {
                endIdx = strList.Count - 1;
                D.Warning("The parameter [ endIndex ] greater than array length, will be limited to array length minus one.");
            }
            for (int i = startIndex; i < endIdx; i++)
            {
                if (nameLength < strList[i].Length)
                {
                    return i;
                }
            }
            return endIdx;
        }

        #endregion

        #region IO
        /// <summary>
        /// Copy the folder to a certain path.
        /// <para>复制文件夹到某一路径</para>
        /// </summary>
        /// <param name="sourceDir">源路径</param>
        /// <param name="destDir">目标路径</param>
        /// <param name="overwrite">覆盖</param>
        public static void CopyFolder(string sourceDir, string destDir, bool overwrite = true)
        {
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
                CopyFolder(subDir, desSubDir);
            }
        }
        #endregion

        #region Assembly

        /// <summary>
        /// 获取程序集
        /// </summary>
        /// <param name="assemblyName">程序集名</param>
        public static System.Reflection.Assembly GetAssembly(string assemblyName)
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (assembly.GetName().Name == assemblyName)
                {
                    return assembly;
                }
            }

            D.Error($"Not found this assembly whit name: {assemblyName}");
            return null;
        }

        #endregion
    }
}

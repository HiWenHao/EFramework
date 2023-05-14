/* 
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-20 19:57:04
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-04-20 19:57:04
 * ScriptVersion: 0.1
 * ===============================================
*/
using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using EasyFramework;

namespace EasyFramework.Edit
{
    /// <summary>
    /// The editor folder utils.
    /// </summary>
    public class EditorUtils
	{
        #region Load
        /// <summary>
        /// 按资源获取单例资产
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="assetsPath">资源路径</param>
        public static T GetSingletonAssetsByResources<T>(string assetsPath) where T : ScriptableObject, new()
        {
            string assetType = typeof(T).Name;
#if UNITY_EDITOR
            string[] globalAssetPaths = UnityEditor.AssetDatabase.FindAssets($"t:{assetType}");
            if (globalAssetPaths.Length > 1)
            {
                foreach (var assetPath in globalAssetPaths)
                {
                    D.Error($"Not allow has multi type. 不能有多个 {assetType}. 路径: {UnityEditor.AssetDatabase.GUIDToAssetPath(assetPath)}");
                }
                throw new Exception($"Not allow has multi type. 不能有多个 {assetType}");
            }
#endif
            T customGlobalSettings = Resources.Load<T>(assetsPath);
            if (customGlobalSettings == null)
            {
                D.Error($"Don`t find asset. 没找到 {assetType} asset，自动创建创建一个:{assetsPath}.");
                return null;
            }

            return customGlobalSettings;
        }

        /// <summary>
        /// 根据路径加载设置面板
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        public static T LoadSettingAtPath<T>() where T : ScriptableObject, new()
        {
            T _setting = default;
            string[] _paths = AssetDatabase.FindAssets($"t:{typeof(T)}");
            if (_paths.Length == 0)
            {
                D.Error($"不存在 {typeof(T).Name}");
                return _setting;
            }
            if (_paths.Length > 1)
            {
                D.Error($"{typeof(T).Name} 数量大于1");
                return _setting;
            }
            string _path = AssetDatabase.GUIDToAssetPath(_paths[0]);
            _setting = AssetDatabase.LoadAssetAtPath<T>(_path);
            return _setting;
        }

        #endregion

        #region String
        /// <summary>
        /// 删除标点符号
        /// </summary>
        public static string RemovePunctuation(string str)
        {
            return Regex.Replace(str, "[ \\[ \\] \\^ \\-_*×――(^)（）{}/【】$%~!@#$…&%￥—+=<>《》!！??？:：•`·、。，；,.;\"‘’“”-]", "");
        }

        #endregion
    }
}

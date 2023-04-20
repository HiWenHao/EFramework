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
using UnityEngine;
using XHTools;

namespace EasyFramework.Edit
{
    /// <summary>
    /// The editor folder utils.
    /// </summary>
    public class EditorUtils
	{
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

    }
}

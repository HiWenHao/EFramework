/* 
 * ================================================
 * Describe:      This script is used to  . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-14 11:51:39
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-14 11:51:39
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.Framework;
using System;
using UnityEngine;
using XHTools;

namespace EasyFramework.Edit.Setting
{
    /// <summary>
    /// Easy framework setting utils.框架设置工具
    /// </summary>
    public class EFProjectSettingsUtils
    {
        private static readonly string m_EFProjectSettingsPath = "Settings/ProjectSetting";
        private static EFProjectSetting m_EFProjectSetting;
        public static FrameworkSettings FrameworkGlobalSetting { get { return EFProjectSettings.FrameworkGlobalSetting; } }

        public static EFProjectSetting EFProjectSettings
        {
            get
            {
                if (m_EFProjectSetting == null)
                {
                    m_EFProjectSetting = GetSingletonAssetsByResources<EFProjectSetting>(m_EFProjectSettingsPath);
                }
                return m_EFProjectSetting;
            }
        }

        private static T GetSingletonAssetsByResources<T>(string assetsPath) where T : ScriptableObject, new()
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

/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-20 20:41:57
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-04-20 20:41:57
 * ScriptVersion: 0.1
 * ===============================================
 */

using EasyFramework.Edit.Windows.ConfigPanel;
using UnityEngine;

namespace EasyFramework.Edit
{
    /// <summary>
    /// Easy framework setting utils.框架设置工具
    /// </summary>
    public static partial class ConfigManager
    {
        public const string ConfigEditPath = "Assets/Editor Resources/Configs";
        public const string ConfigRuntimePath = "Assets/Resources/Configs";

        private static System.Type _creating;
        private static PathConfig _pathConfig;
        private static ProjectConfig _projectConfig;


        public static PathConfig Path => GetOrCreate(ref _pathConfig, ConfigEditPath);
        public static ProjectConfig Project => GetOrCreate(ref _projectConfig, ConfigRuntimePath);

        /// <summary>
        /// 获取或创建配置 —— 先查缓存，再查 AssetDatabase，最后创建
        /// <para>Get or create a config asset — cache first, then AssetDatabase lookup, then create</para>
        /// </summary>
        private static T GetOrCreate<T>(ref T cache, string path) where T : ScriptableObject, new()
        {
            if (cache != null) return cache;
            if (_creating != null && _creating == typeof(T)) return null;

            if (EditorUtils.CheckAssets<T>(out string configPath))
            {
                cache = EditorUtils.LoadSettingAtPath<T>(configPath);
                if (cache != null)
                    return cache;
            }

            _creating = typeof(T);
            cache = Create.CreateSettings.Instance<T>(true, path);
            _creating = null;

            return cache;
        }
    }
}
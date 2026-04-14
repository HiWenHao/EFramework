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
        private const string ConfigEditPath = "Assets/Editor Resources/Configs";
        private const string ConfigRuntimePath = "Assets/Resources/Configs";

        private static PathConfig _pathConfig;
        private static ProjectConfig _projectConfig;
        private static UiBindingConfig _uiBindingConfig;


        public static PathConfig Path => _pathConfig.GetConfig<PathConfig>(ConfigEditPath);
        public static ProjectConfig Project => _projectConfig.GetConfig<ProjectConfig>(ConfigRuntimePath);
        public static UiBindingConfig UiBinding => _uiBindingConfig.GetConfig<UiBindingConfig>(ConfigEditPath);
        
        
        private static T GetConfig<T>(this ScriptableObject target, string path) where T : ScriptableObject, new()
        {
            T config = target as T;
            if (config is null && EditorUtils.CheckAssets<T>(out var pathConfigPath))
                config = EditorUtils.LoadSettingAtPath<T>();
            config ??= Create.CreateSettings.Instance<T>(true, path);
            return config;
        }
    }
}
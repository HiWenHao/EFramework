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

namespace EasyFramework.Edit
{
    /// <summary>
    /// Easy framework setting utils.框架设置工具
    /// </summary>
    public static class ConfigManager
    {
        private static ProjectConfig _projectConfig;
        public static ProjectConfig Project
        {
            get
            {
                if (_projectConfig is null && EditorUtils.CheckAssets<ProjectConfig>(out var pathConfigPath))
                    _projectConfig = EditorUtils.LoadSettingAtPath<ProjectConfig>();
                if (_projectConfig is null)
                    Create.CreateSettings.Instance<ProjectConfig>(true, "Assets/Resources/Configs");
                return _projectConfig;
            }
        }
    

        private static PathConfig _pathConfig;
        public static PathConfig Path
        {
            get
            {
                if (_pathConfig is null && EditorUtils.CheckAssets<PathConfig>(out var pathConfigPath))
                    _pathConfig = EditorUtils.LoadSettingAtPath<PathConfig>();
                return _pathConfig;
            }
        }
        
        private static AutoBindingConfig _uiBindingConfig;
        public static AutoBindingConfig UiBinding
        {
            get
            {
                if (_uiBindingConfig is null && EditorUtils.CheckAssets<AutoBindingConfig>(out var pathConfigPath))
                    _uiBindingConfig = EditorUtils.LoadSettingAtPath<AutoBindingConfig>();
                if (_uiBindingConfig is null)
                    Create.CreateSettings.Instance<AutoBindingConfig>(true, "Assets/Resources/Configs");
                return _uiBindingConfig;
            }
        }
    }
}

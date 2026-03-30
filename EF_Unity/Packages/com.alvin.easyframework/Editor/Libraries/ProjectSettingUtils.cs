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

using EasyFramework.Edit.Setting;
using EasyFramework.Windows.SettingPanel;

namespace EasyFramework.Edit
{
    /// <summary>
    /// Easy framework setting utils.框架设置工具
    /// </summary>
    public static class ProjectUtility
    {
        private static ProjectSetting _projectSetting;
        public static ProjectSetting Project
        {
            get
            {
                if (_projectSetting is null && EditorUtils.CheckAssets<ProjectSetting>(out var pathConfigPath))
                    _projectSetting = EditorUtils.LoadSettingAtPath<ProjectSetting>();
                if (_projectSetting is null)
                    CreateSettings.Instance<ProjectSetting>(true, "Resources/Settings/");
                return _projectSetting;
            }
        }
    

        private static PathConfigSetting _pathConfigSetting;
        public static PathConfigSetting Path
        {
            get
            {
                if (_pathConfigSetting is null && EditorUtils.CheckAssets<PathConfigSetting>(out var pathConfigPath))
                    _pathConfigSetting = EditorUtils.LoadSettingAtPath<PathConfigSetting>();
                return _pathConfigSetting;
            }
        }
    }
}

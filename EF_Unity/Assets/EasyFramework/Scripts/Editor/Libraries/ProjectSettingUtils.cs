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
    public class ProjectUtility
    {
        static ProjectSetting _projectSetting;
        public static ProjectSetting Project
        {
            get
            {
                if (_projectSetting == null)
                {
                    _projectSetting = EditorUtils.GetSingletonAssetsByResources<ProjectSetting>("Settings/ProjectSetting");
                }
                return _projectSetting;
            }
        }
    

        static PathConfigSetting _pathConfigSetting;
        public static PathConfigSetting Path
        {
            get
            {
                if (_pathConfigSetting == null)
                {
                    _pathConfigSetting = EditorUtils.GetSingletonAssetsByResources<PathConfigSetting>("Settings/PathConfigSetting");
                }
                return _pathConfigSetting;
            }
        }
    }
}

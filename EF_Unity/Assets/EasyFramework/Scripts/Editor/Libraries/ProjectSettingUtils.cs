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
        static ProjectSetting m_ProjectSetting;
        public static ProjectSetting Project
        {
            get
            {
                if (m_ProjectSetting == null)
                {
                    m_ProjectSetting = EditorUtils.GetSingletonAssetsByResources<ProjectSetting>("Settings/ProjectSetting");
                }
                return m_ProjectSetting;
            }
        }
    

        static PathConfigSetting m_PathConfigSetting;
        public static PathConfigSetting Path
        {
            get
            {
                if (m_PathConfigSetting == null)
                {
                    m_PathConfigSetting = EditorUtils.GetSingletonAssetsByResources<PathConfigSetting>("Settings/PathConfigSetting");
                }
                return m_PathConfigSetting;
            }
        }
    }
}

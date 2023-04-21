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
using EasyFramework.Edit.Optimal;
using EasyFramework.Edit.Setting;

namespace EasyFramework.Edit
{
    /// <summary>
    /// Easy framework setting utils.框架设置工具
    /// </summary>
    public class ProjectSettingsUtils
    {
        static ProjectSetting m_ProjectSetting;
        public static Settings projectSetting => EFProjectSettings.Setting;
        public static ProjectSetting EFProjectSettings
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
    

        static OptimalSetting m_OptimalSetting;
        public static OptimalSetting Optimal
        {
            get
            {
                if (m_OptimalSetting == null)
                {
                    m_OptimalSetting = EditorUtils.GetSingletonAssetsByResources<OptimalSetting>("Settings/OptimalSetting");
                }
                return m_OptimalSetting;
            }
        }
    }
}

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
using EasyFramework.Framework;

namespace EasyFramework.Edit
{
    /// <summary>
    /// Easy framework setting utils.框架设置工具
    /// </summary>
    public class ProjectSettingsUtils
    {
        private static readonly string m_EFProjectSettingsPath = "Settings/ProjectSetting";
        private static ProjectSetting m_EFProjectSetting;
        public static FrameworkSettings FrameworkGlobalSetting { get { return EFProjectSettings.FrameworkGlobalSetting; } }

        public static ProjectSetting EFProjectSettings
        {
            get
            {
                if (m_EFProjectSetting == null)
                {
                    m_EFProjectSetting = EditorUtils.GetSingletonAssetsByResources<ProjectSetting>(m_EFProjectSettingsPath);
                }
                return m_EFProjectSetting;
            }
        }
    }


    /// <summary>
    /// Easy framework optimal utils.框架优化工具
    /// </summary>
    public class OptimalSettingUtils
    {
        private static readonly string m_EFOptimalSettingPath = "Settings/OptimalSetting";
        private static OptimalSetting m_EFOptimalSetting;
        public static OptimalSettings OptimalSetting => EFOptimalSettingss.FrameworkOptimalSetting;

        public static OptimalSetting EFOptimalSettingss
        {
            get
            {
                if (m_EFOptimalSetting == null)
                {
                    m_EFOptimalSetting = EditorUtils.GetSingletonAssetsByResources<OptimalSetting>(m_EFOptimalSettingPath);
                }
                return m_EFOptimalSetting;
            }
        }
    }
}


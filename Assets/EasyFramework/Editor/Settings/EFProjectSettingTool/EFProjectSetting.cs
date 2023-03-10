/* 
 * ================================================
 * Describe:      This script is used to  . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-14 11:43:10
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-14 11:43:10
 * ScriptVersion: 0.1
 * ===============================================
*/
using EasyFramework.Framework;
using UnityEngine;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 框架设置界面
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectSetting", menuName = "EF/ProjectSetting")]
    public class EFProjectSetting : ScriptableObject
    {
        [Header("Framework 框架设置")]
        [SerializeField]
        private FrameworkSettings m_FrameworkGlobalSettings;
        public FrameworkSettings FrameworkGlobalSetting { get { return m_FrameworkGlobalSettings; } }
    }
}

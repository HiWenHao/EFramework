/* 
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-20 19:32:17
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-04-20 19:32:17
 * ScriptVersion: 0.1
 * ===============================================
*/
using EasyFramework.Framework;
using UnityEngine;

namespace EasyFramework.Edit.Optimal
{
    /// <summary>
    /// 优化设置界面
    /// </summary>
    [CreateAssetMenu(fileName = "OptimalSetting", menuName = "EF/OptimalSetting", order = 100)]
    public class OptimalSetting : ScriptableObject
    {
        [Header("Optimal Setting 优化设置")]
        [SerializeField]
        private OptimalSettings m_FrameworkOptimalSettings;
        public OptimalSettings FrameworkOptimalSetting { get { return m_FrameworkOptimalSettings; } }
    }
}

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
using UnityEngine;

namespace EasyFramework.Edit.Optimal
{
    /// <summary>
    /// 优化设置界面
    /// </summary>
    [CreateAssetMenu(fileName = "OptimalSetting", menuName = "EF/OptimalSetting", order = 100)]
    public class OptimalSetting : ScriptableObject
    {
        [Header("Sublime文件路径")]
        [SerializeField]
        private string m_SublimePath = "";
        public string SublimePath => m_SublimePath;

        [Header("Notepad++文件路径")]
        [SerializeField]
        private string m_NotepadPath = "";
        public string NotepadPath => m_NotepadPath;

        [Header("图集资源存放地")]
        [SerializeField]
        private string m_AtlasFolder = "Assets/";
        public string AtlasFolder => m_AtlasFolder;

        [Tooltip("提取压缩动画的路径")]
        [Header("Extract and compress the animation file to this path")]
        [SerializeField]
        private string m_ExtractPath = "Assets";
        public string ExtractPath => m_ExtractPath;
    }
}

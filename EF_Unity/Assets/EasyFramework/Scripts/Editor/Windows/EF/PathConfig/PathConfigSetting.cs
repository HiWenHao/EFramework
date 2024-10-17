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
using EasyFramework.Edit;
using UnityEngine;

namespace EasyFramework.Windows
{
    namespace SettingPanel
    {
        /// <summary>
        /// 优化设置界面
        /// </summary>
        [CreateAssetMenu(fileName = "PathConfigSetting", menuName = "EF/PathConfigSetting", order = 100)]
        public class PathConfigSetting : ScriptableObject
        {
            [SerializeField, Header(LanguagAttribute.FrameworkPath)]
            private string m_FrameworkPath = "Assets/EasyFramework/";
            public string FrameworkPath => m_FrameworkPath;

            [SerializeField, Header(LanguagAttribute.AtlasFolder)]
            private string m_AtlasFolder = "Assets/";
            public string AtlasFolder => m_AtlasFolder;

            [SerializeField, Header(LanguagAttribute.ExtractPath)]
            private string m_ExtractPath = "Assets/";
            public string ExtractPath => m_ExtractPath;

            [SerializeField, Header(LanguagAttribute.UIPrefabPath)]
            private string m_UIPrefabPath = "Assets/";
            public string UIPrefabPath => m_UIPrefabPath;

            [SerializeField, Header(LanguagAttribute.UICodePath)]
            private string m_UICodePath = "Assets/";
            public string UICodePath => m_UICodePath;

            [SerializeField, Header(LanguagAttribute.SublimePath)]
            private string m_SublimePath = "";
            public string SublimePath => m_SublimePath;

            [SerializeField, Header(LanguagAttribute.NotepadPath)]
            private string m_NotepadPath = "";
            public string NotepadPath => m_NotepadPath;
        }
    }
}
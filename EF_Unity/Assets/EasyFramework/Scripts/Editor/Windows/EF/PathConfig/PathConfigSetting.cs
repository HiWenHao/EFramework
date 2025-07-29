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
            private string _frameworkPath = "Assets/EasyFramework/";
            public string FrameworkPath => _frameworkPath;

            [SerializeField, Header(LanguagAttribute.AtlasFolder)]
            private string _atlasFolder = "Assets/";
            public string AtlasFolder => _atlasFolder;

            [SerializeField, Header(LanguagAttribute.ExtractPath)]
            private string _extractPath = "Assets/";
            public string ExtractPath => _extractPath;

            [SerializeField, Header(LanguagAttribute.UIPrefabPath)]
            private string _uiPrefabPath = "Assets/";
            public string UIPrefabPath => _uiPrefabPath;

            [SerializeField, Header(LanguagAttribute.UICodePath)]
            private string _uiCodePath = "Assets/";
            public string UICodePath => _uiCodePath;

            [SerializeField, Header(LanguagAttribute.SublimePath)]
            private string _sublimePath = "";
            public string SublimePath => _sublimePath;

            [SerializeField, Header(LanguagAttribute.NotepadPath)]
            private string _notepadPath = "";
            public string NotepadPath => _notepadPath;
        }
    }
}
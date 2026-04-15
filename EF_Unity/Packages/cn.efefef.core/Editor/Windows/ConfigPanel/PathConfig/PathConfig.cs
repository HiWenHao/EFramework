/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-20 19:32:17
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-01 16:47:08
 * ScriptVersion: 0.1
 * ===============================================
 */

using UnityEngine;

namespace EasyFramework.Edit.Windows.ConfigPanel
{
    /// <summary>
    /// 优化设置界面
    /// </summary>
    public class PathConfig : ScriptableObject
    {
        [SerializeField, Header(LanguageAttribute.AtlasFolder)]
        private string _atlasFolder = "Assets/";
        public string AtlasFolder => _atlasFolder;

        [SerializeField, Header(LanguageAttribute.ExtractPath)]
        private string _extractPath = "Assets/";
        public string ExtractPath => _extractPath;

        [SerializeField, Header(LanguageAttribute.UIPrefabPath)]
        private string _uiPrefabPath = "Assets/";
        public string UIPrefabPath => _uiPrefabPath;

        [SerializeField, Header(LanguageAttribute.UICodePath)]
        private string _uiCodePath = "Assets/";
        public string UICodePath => _uiCodePath;
        
        [SerializeField, Header("Luban Code Path")]
        private string _lubanCodePath = "Assets/";
        public string LubanCodePath => _lubanCodePath;
        
        [SerializeField, Header("Luban Data Path")]
        private string _lubanDataPath = "Assets/";
        public string LubanDataPath => _lubanDataPath;
        
        

        //  ====================================================================
        
        
        [SerializeField, Header(LanguageAttribute.SublimePath)]
        private string _sublimePath = "";
        public string SublimePath => _sublimePath;

        [SerializeField, Header(LanguageAttribute.NotepadPath)]
        private string _notepadPath = "";
        public string NotepadPath => _notepadPath;
    }
}

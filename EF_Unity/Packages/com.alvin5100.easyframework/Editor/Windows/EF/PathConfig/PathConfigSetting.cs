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
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Windows
{
    namespace SettingPanel
    {
        /// <summary>
        /// 优化设置界面
        /// </summary>
        //[CreateAssetMenu(fileName = "PathConfigSetting", menuName = "EF/PathConfigSetting", order = 100)]
        public class PathConfigSetting : ScriptableObject
        {
            [MenuItem("Assets/Create/EF/PathConfigSetting")]
            static void Created()
            {
                string configPath = Application.dataPath + "../Packages/EF/Editor Resources/Settings/PathConfigSetting.asset";
                PathConfigSetting existingAsset = AssetDatabase.LoadAssetAtPath<PathConfigSetting>(configPath);

                if (existingAsset == null)
                {
                    string[] guids = AssetDatabase.FindAssets("t:PathConfigSetting");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        existingAsset = AssetDatabase.LoadAssetAtPath<PathConfigSetting>(path);
                    }
                }

                if (existingAsset != null)
                {
                    // 自动选中该资源，并高亮显示
                    Selection.activeObject = existingAsset;
                    EditorGUIUtility.PingObject(existingAsset);
                    return;
                }

                PathConfigSetting asset = CreateInstance<PathConfigSetting>();
        
                string folderPath = System.IO.Path.GetDirectoryName(configPath);
                if (!System.IO.Directory.Exists(folderPath))
                    System.IO.Directory.CreateDirectory(folderPath);
        
                AssetDatabase.CreateAsset(asset, configPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
        
                Selection.activeObject = asset;
            }
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
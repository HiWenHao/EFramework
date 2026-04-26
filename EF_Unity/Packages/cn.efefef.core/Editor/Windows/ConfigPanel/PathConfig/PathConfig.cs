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
        [SerializeField, HeaderPro("图集在工程下的路径", "The path of the atlas under Engineering")]
        private string _atlasFolder = "Assets/";

        public string AtlasFolder => _atlasFolder;

        [SerializeField, HeaderPro("动画压缩后在工程下的路径", "Animation path under project after compression")]
        private string _extractPath = "Assets/";

        public string ExtractPath => _extractPath;

        [SerializeField, HeaderPro("UI预制件保存在工程下的路径", "The path where the UI prefab is saved under the project")]
        private string _uiPrefabPath = "Assets/";

        public string UIPrefabPath => _uiPrefabPath;

        private static bool sss;

        [SerializeField, HeaderPro("UI脚本保存在工程下的路径", "The path where the UI scripts will be saved under the project")]
        private string _uiCodePath = "Assets/";

        public string UICodePath => _uiCodePath;

        [SerializeField, HeaderPro("导表工具生成的代码路径", "Luban Code Path")]
        private string _lubanCodePath = "Assets/";

        public string LubanCodePath => _lubanCodePath;

        [SerializeField, HeaderPro("导表工具生成的数据路径", "Luban Data Path")]
        private string _lubanDataPath = "Assets/";

        public string LubanDataPath => _lubanDataPath;


        //  ====================================================================


        [SerializeField, HeaderPro("Sublime在系统中的路径", "The path of Sublime in the system")]
        private string _sublimePath = "";

        public string SublimePath => _sublimePath;

        [SerializeField, HeaderPro("Notepad++在系统中的路径", "The path of Notepad++ in the system")]
        private string _notepadPath = "";

        public string NotepadPath => _notepadPath;
    }
}
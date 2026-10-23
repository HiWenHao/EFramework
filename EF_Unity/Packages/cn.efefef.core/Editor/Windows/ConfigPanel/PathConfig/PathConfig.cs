/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-20 19:32:17
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-10-23 17:20:00
 * ScriptVersion: 0.2
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
        // 注：图集 / UI预制件 / UI代码 三个路径已迁至 cn.efefef.ui 的 UiBindingConfig
        // Note: atlas / UI-prefab / UI-code paths were moved to UiBindingConfig in cn.efefef.ui
        [SerializeField, HeaderPro("动画压缩后在工程下的路径", "Animation path under project after compression")]
        private string _extractPath = "Assets/";

        public string ExtractPath => _extractPath;

        //  ====================================================================


        [SerializeField, HeaderPro("Sublime在系统中的路径", "The path of Sublime in the system")]
        private string _sublimePath = "";

        public string SublimePath => _sublimePath;

        [SerializeField, HeaderPro("Notepad++在系统中的路径", "The path of Notepad++ in the system")]
        private string _notepadPath = "";

        public string NotepadPath => _notepadPath;
    }
}
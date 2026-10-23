/*
 * ================================================
 * Describe:        This script is used to setting auto bind.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2023-02-13 17:36:43
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-23 17:20:00
 * ScriptVersion:   0.2
 * ===============================================
 */

using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Edit.Windows.ConfigPanel
{
    /// <summary>
    /// 自动绑定全局设置
    /// </summary>
    public class UiBindingConfig : ScriptableObject
    {
        private static UiBindingConfig _instance;

        public static UiBindingConfig Instance
        {
            get
            {
                if (_instance is null && EditorUtils.CheckAssets<UiBindingConfig>(out var pathConfigPath))
                    _instance = EditorUtils.LoadSettingAtPath<UiBindingConfig>(pathConfigPath);
                _instance ??= Create.CreateSettings.Instance<UiBindingConfig>(true, ConfigManager.ConfigEditPath);

                return _instance;
            }
        }

        /// <summary> 图集在工程下的路径 <para>The path of the atlas under the project</para> </summary>
        [SerializeField, HeaderPro("图集在工程下的路径", "The path of the atlas under Engineering")]
        private string _atlasFolder = "Assets/";

        public string AtlasFolder => _atlasFolder;

        /// <summary> UI预制件保存在工程下的路径 <para>The path where the UI prefab is saved under the project</para> </summary>
        [SerializeField, HeaderPro("UI预制件保存在工程下的路径", "The path where the UI prefab is saved under the project")]
        private string _uiPrefabPath = "Assets/";

        public string UIPrefabPath => _uiPrefabPath;

        /// <summary> UI脚本保存在工程下的路径 <para>The path where the UI scripts will be saved under the project</para> </summary>
        [SerializeField, HeaderPro("UI脚本保存在工程下的路径", "The path where the UI scripts will be saved under the project")]
        private string _uiCodePath = "Assets/";

        public string UICodePath => _uiCodePath;

        /// <summary>
        /// 组件的缩略名字映射
        /// </summary>
        [SerializeField, HeaderPro("组件的缩略名字映射", "Component rule settings")]
        [Space]
        private List<RulePrefixes> _rulePrefixes = new List<RulePrefixes>()
        {
            new RulePrefixes("Btn", "Button"),
            new RulePrefixes("BtnP", "ButtonPro"),
            new RulePrefixes("Canvas", "Canvas"),
            new RulePrefixes("Drop", "Dropdown"),
            new RulePrefixes("DropTmp", "TMP_Dropdown"),
            new RulePrefixes("Group", "CanvasGroup"),
            new RulePrefixes("GGroup", "GridLayoutGroup"),
            new RulePrefixes("HGroup", "HorizontalLayoutGroup"),
            new RulePrefixes("VGroup", "VerticalLayoutGroup"),
            new RulePrefixes("Img", "Image"),
            new RulePrefixes("ImgP", "ImagePro"),
            new RulePrefixes("Ipt", "InputField"),
            new RulePrefixes("IptTmp", "TMP_InputField"),
            new RulePrefixes("Mask", "Mask"),
            new RulePrefixes("Map", "RadarMap"),
            new RulePrefixes("Mask2D", "RectMask2D"),
            new RulePrefixes("NAnim", "Animator"),
            new RulePrefixes("OAnim", "Animation"),
            new RulePrefixes("RImg", "RawImage"),
            new RulePrefixes("Rect", "RectTransform"),
            new RulePrefixes("Sld", "Slider"),
            new RulePrefixes("Sbar", "Scrollbar"),
            new RulePrefixes("SRct", "ScrollRect"),
            new RulePrefixes("SRctP", "ScrollRectPro"),
            new RulePrefixes("Tran", "Transform"),
            new RulePrefixes("Txt", "Text"),
            new RulePrefixes("Tmp", "TextMeshProUGUI"),
            new RulePrefixes("Tog", "Toggle"),
            new RulePrefixes("TGroup", "ToggleGroup"),
        };

        /// <summary> 组件的缩略名字映射 </summary>
        public List<RulePrefixes> RulePrefixes => _rulePrefixes;
    }
}
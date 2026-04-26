/*
 * ================================================
 * Describe:      This script is used to setting auto bind.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-13 17:36:43
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-01 16:47:08
 * ScriptVersion: 0.1
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
        /// <summary>
        /// 默认命名空间
        /// </summary>
        [SerializeField, HeaderPro("默认命名空间", "Default namespace")]
        private string _namespace = "PleaseChangeTheNamespace";

        /// <summary> 默认命名空间 </summary>
        public string Namespace => _namespace;

        /// <summary>
        /// 组件的缩略名字映射
        /// </summary>
        [SerializeField, HeaderPro("组件的缩略名字映射", "Component rule settings")]
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
            new RulePrefixes("Img", "Image"),
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
            new RulePrefixes("TxtTmp", "TextMeshProUGUI"),
            new RulePrefixes("Tog", "Toggle"),
            new RulePrefixes("TGroup", "ToggleGroup"),
            new RulePrefixes("VGroup", "VerticalLayoutGroup"),
        };

        /// <summary> 组件的缩略名字映射 </summary>
        public List<RulePrefixes> RulePrefixes => _rulePrefixes;
    }
}
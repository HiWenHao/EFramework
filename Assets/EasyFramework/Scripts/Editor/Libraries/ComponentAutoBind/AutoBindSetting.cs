/* 
 * ================================================
 * Describe:      This script is used to setting auto bind. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-13 17:36:43
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-13 17:36:43
 * ScriptVersion: 0.1
 * ===============================================
*/
using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Edit.AutoBind
{
    /// <summary>
    /// 自动绑定全局设置
    /// </summary>
    [CreateAssetMenu(fileName = "AutoBindSetting", menuName = "EF/AutoBindSetting", order = 210)]
    public class AutoBindSetting : ScriptableObject
    {
        /// <summary>
        /// 默认命名空间
        /// </summary>
        [SerializeField, Header(LanguagAttribute.Namespace)]
        private string m_Namespace = "PleaseChangeTheNamespace";
        /// <summary> 默认命名空间 </summary>
        public string Namespace => m_Namespace;

        /// <summary>
        /// 组件的缩略名字映射
        /// </summary>
        [SerializeField, Header(LanguagAttribute.RulePrefixes)]
        private List<RulePrefixe> m_RulePrefixes = new List<RulePrefixe>()
        {
            new RulePrefixe("Btn","Button"),
            new RulePrefixe("BtnP","ButtonPro"),
            new RulePrefixe("Canvas","Canvas"),
            new RulePrefixe("Drop","Dropdown"),
            new RulePrefixe("DropTmp","TMP_Dropdown"),
            new RulePrefixe("Group","CanvasGroup"),
            new RulePrefixe("GGroup","GridLayoutGroup"),
            new RulePrefixe("HGroup","HorizontalLayoutGroup"),
            new RulePrefixe("Img","Image"),
            new RulePrefixe("Ipt","InputField"),
            new RulePrefixe("IptTmp","TMP_InputField"),
            new RulePrefixe("Mask","Mask"),
            new RulePrefixe("Map","RadarMap"),
            new RulePrefixe("Mask2D","RectMask2D"),
            new RulePrefixe("NAnim","Animator"),
            new RulePrefixe("OAnim","Animation"),
            new RulePrefixe("RImg","RawImage"),
            new RulePrefixe("Rect","RectTransform"),
            new RulePrefixe("Sld","Slider"),
            new RulePrefixe("Sbar","Scrollbar"),
            new RulePrefixe("SRct","ScrollRect"),
            new RulePrefixe("SRctP","ScrollRectPro"),
            new RulePrefixe("Tran","Transform"),
            new RulePrefixe("Txt","Text"),
            new RulePrefixe("TxtTmp","TextMeshProUGUI"),
            new RulePrefixe("Tog","Toggle"),
            new RulePrefixe("TGroup","ToggleGroup"),
            new RulePrefixe("VGroup","VerticalLayoutGroup"),
        };
        /// <summary> 组件的缩略名字映射 </summary>
        public List<RulePrefixe> RulePrefixes => m_RulePrefixes;
    }
}

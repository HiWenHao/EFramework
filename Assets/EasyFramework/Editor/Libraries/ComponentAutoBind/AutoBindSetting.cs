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
        [Header("默认命名空间")]
        [SerializeField]
        private string m_Namespace = "PleaseChangeTheNamespace";

        /// <summary>
        /// 默认组件代码保存路径
        /// </summary>
        [Header("默认组件代码保存路径")]
        [SerializeField]
        private string m_ComCodePath;

        /// <summary>
        /// 默认UI预制件保存路径
        /// </summary>
        [Header("默认UI预制件保存路径")]
        [SerializeField]
        private string m_PrefabPath;

        /// <summary>
        /// 组件的缩略名字映射
        /// </summary>
        [SerializeField]
        private List<RulePrefixe> m_RulePrefixes = new List<RulePrefixe>()
        {
            new RulePrefixe("Tran","Transform"),
            new RulePrefixe("OAnim","Animation"),
            new RulePrefixe("NAnim","Animator"),
            new RulePrefixe("Rect","RectTransform"),
            new RulePrefixe("Canvas","Canvas"),
            new RulePrefixe("Group","CanvasGroup"),
            new RulePrefixe("VGroup","VerticalLayoutGroup"),
            new RulePrefixe("HGroup","HorizontalLayoutGroup"),
            new RulePrefixe("GGroup","GridLayoutGroup"),
            new RulePrefixe("TGroup","ToggleGroup"),
            new RulePrefixe("Btn","Button"),
            new RulePrefixe("BtnP","ButtonPro"),
            new RulePrefixe("Img","Image"),
            new RulePrefixe("RImg","RawImage"),
            new RulePrefixe("Txt","Text"),
            new RulePrefixe("TxtM","TextMeshProUGUI"),
            new RulePrefixe("Ipt","TMP_InputField"),
            new RulePrefixe("Sld","Slider"),
            new RulePrefixe("Mask","Mask"),
            new RulePrefixe("Mask2D","RectMask2D"),
            new RulePrefixe("Tog","Toggle"),
            new RulePrefixe("Sbar","Scrollbar"),
            new RulePrefixe("SRct","ScrollRect"),
            new RulePrefixe("SRctP","ScrollRectPro"),
            new RulePrefixe("Drop","Dropdown"),
            new RulePrefixe("Map","RadarMap"),
        };

        /// <summary>
        /// 默认命名空间
        /// </summary>
        public string Namespace => m_Namespace;

        /// <summary>
        /// 默认组件代码保存路径
        /// </summary>
        public string ComCodePath => m_ComCodePath;

        /// <summary>
        /// 默认UI预制件保存路径
        /// </summary>
        public string PrefabPath => m_PrefabPath;

        /// <summary>
        /// 组件的缩略名字映射
        /// </summary>
        public List<RulePrefixe> RulePrefixes => m_RulePrefixes;
    }
}

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
using UnityEditor;
using UnityEngine;
using XHTools;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 自动绑定全局设置
    /// </summary>
    [CreateAssetMenu(fileName = "AutoBindSetting", menuName = "EF/AutoBindSetting")]
    public class AutoBindSetting : ScriptableObject
    {
        /// <summary>
        /// 默认命名空间
        /// </summary>
        [Header("默认命名空间")]
        [SerializeField]
        private string m_Namespace;

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
        private List<EFRulePrefixe> m_RulePrefixes = new List<EFRulePrefixe>()
        {
            new EFRulePrefixe("Tran","Transform"),
            new EFRulePrefixe("OAnim","Animation"),
            new EFRulePrefixe("NAnim","Animator"),
            new EFRulePrefixe("Rect","RectTransform"),
            new EFRulePrefixe("Canvas","Canvas"),
            new EFRulePrefixe("Group","CanvasGroup"),
            new EFRulePrefixe("VGroup","VerticalLayoutGroup"),
            new EFRulePrefixe("HGroup","HorizontalLayoutGroup"),
            new EFRulePrefixe("GGroup","GridLayoutGroup"),
            new EFRulePrefixe("TGroup","ToggleGroup"),
            new EFRulePrefixe("Btn","Button"),
            new EFRulePrefixe("BtnP","ButtonPro"),
            new EFRulePrefixe("Img","Image"),
            new EFRulePrefixe("RImg","RawImage"),
            new EFRulePrefixe("Txt","Text"),
            new EFRulePrefixe("TxtM","TextMeshProUGUI"),
            new EFRulePrefixe("Ipt","TMP_InputField"),
            new EFRulePrefixe("Slider","Slider"),
            new EFRulePrefixe("Mask","Mask"),
            new EFRulePrefixe("Mask2D","RectMask2D"),
            new EFRulePrefixe("Tog","Toggle"),
            new EFRulePrefixe("Sbar","Scrollbar"),
            new EFRulePrefixe("SRect","ScrollRect"),
            new EFRulePrefixe("Drop","Dropdown"),
            new EFRulePrefixe("VGridV","LoopGridView"),
            new EFRulePrefixe("HGridV","LoopGridView"),
            new EFRulePrefixe("VListV","LoopListView2"),
            new EFRulePrefixe("HListV","LoopListView2"),
            new EFRulePrefixe("Map","RadarMap"),
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
        public List<EFRulePrefixe> RulePrefixes => m_RulePrefixes;

        /// <summary>
        /// 查找是否为有效绑定
        /// </summary>
        /// <param name="target">目标</param>
        /// <param name="filedNames">对象名</param>
        /// <param name="componentTypeNames">对象组件</param>
        /// <returns>是否有效</returns>
        public static bool IsValidBind(Transform target, List<string> filedNames, List<string> componentTypeNames)
        {
            string[] strArray = target.name.Split('_');

            if (strArray.Length == 1)
            {
                return false;
            }

            bool isFind = false;
            string filedName = strArray[strArray.Length - 1];
            filedName = RemovePunctuation(filedName);
            filedName.Trim();
            target.name = $"{strArray[0]}_{filedName}";
            for (int i = 0; i < strArray.Length - 1; i++)
            {
                string str = strArray[i].Replace("#", "");
                string comName;
                var _AutoBindGlobalSetting = GetAutoBindSetting();
                var _PrefixesDict = _AutoBindGlobalSetting.RulePrefixes;
                bool isFindComponent = false;
                foreach (var autoBindRulePrefix in _PrefixesDict)
                {
                    if (autoBindRulePrefix.Prefixe.Equals(str))
                    {
                        comName = autoBindRulePrefix.FullContent;
                        filedNames.Add($"{str}_{filedName}");
                        componentTypeNames.Add(comName);
                        isFind = true;
                        isFindComponent = true;
                        break;
                    }
                }
                if (!isFindComponent)
                {
                    D.Warning($"{target.name}的命名中{str}不存在对应的组件类型，绑定失败");
                }
            }
            if (!isFind)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 获取自动绑定的全局设置
        /// </summary>
        public static AutoBindSetting GetAutoBindSetting()
        {
            AutoBindSetting _AutoBindGlobalSetting = null;
            string[] paths = AssetDatabase.FindAssets("t:AutoBindSetting");
            if (paths.Length == 0)
            {
                D.Error("不存在AutoBindSetting");
                return _AutoBindGlobalSetting;
            }
            if (paths.Length > 1)
            {
                D.Error("AutoBindSetting数量大于1");
                return _AutoBindGlobalSetting;
            }
            string path = AssetDatabase.GUIDToAssetPath(paths[0]);
            _AutoBindGlobalSetting = AssetDatabase.LoadAssetAtPath<AutoBindSetting>(path);
            return _AutoBindGlobalSetting;
        }

        /// <summary>
        /// 删除标点符号
        /// </summary>
        static string RemovePunctuation(string str)
        {
            str = str.Replace(",", "")
                .Replace("，", "")
                .Replace(".", "")
                .Replace("。", "")
                .Replace("!", "")
                .Replace("！", "")
                .Replace("?", "")
                .Replace("？", "")
                .Replace(":", "")
                .Replace("：", "")
                .Replace(";", "")
                .Replace("；", "")
                .Replace("～", "")
                .Replace("-", "")
                //.Replace("_", "")
                .Replace("——", "")
                .Replace("—", "")
                .Replace("--", "")
                .Replace("【", "")
                .Replace("】", "")
                .Replace("\\", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("（", "")
                .Replace("）", "")
                .Replace("#", "")
                .Replace("$", "")
                .Replace(" ", "");
            return str;

        }
    }
}

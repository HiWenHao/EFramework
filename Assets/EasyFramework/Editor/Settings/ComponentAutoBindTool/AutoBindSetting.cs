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

namespace EasyFramework.Edit.AutoBind
{
    /// <summary>
    /// 自动绑定全局设置
    /// </summary>
    [CreateAssetMenu(fileName = "AutoBindSetting", menuName = "EF/AutoBindSetting", order = 30)]
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

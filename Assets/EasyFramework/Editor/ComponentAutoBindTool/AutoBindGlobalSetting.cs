/* 
 * ================================================
 * Describe:      This script is used to  . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-13 17:36:43
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-13 17:36:43
 * ScriptVersion: 0.1
 * ===============================================
*/
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XHTools;

namespace EasyFramework.Framework
{
    /// <summary>
    /// 自动绑定规则前缀
    /// </summary>
    [Serializable]
    public class AutoBindRulePrefixe
    {
        /// <summary>
        /// 前缀
        /// </summary>
        public string Prefixe;

        /// <summary>
        /// 全名称
        /// </summary>
        public string FullName;

        /// <summary>
        /// 自动绑定规则前缀
        /// </summary>
        /// <param name="prefixe">前缀</param>
        /// <param name="fullName">全名称</param>
        public AutoBindRulePrefixe(string prefixe, string fullName)
        {
            Prefixe = prefixe;
            FullName = fullName;
        }
    }

    /// <summary>
    /// 自动绑定全局设置
    /// </summary>
    [CreateAssetMenu(fileName = "AutoBindGlobalSetting", menuName = "EF/AutoBindGlobalSetting")]
    public class AutoBindGlobalSetting : ScriptableObject
    {
        /// <summary>
        /// 默认命名空间
        /// </summary>
        [SerializeField]
        private string m_Namespace;

        /// <summary>
        /// 默认组件代码保存路径
        /// </summary>
        [SerializeField]
        private string m_ComCodePath;

        /// <summary>
        /// 组件的缩略名字映射
        /// </summary>
        [SerializeField]
        private List<AutoBindRulePrefixe> m_RulePrefixes = new List<AutoBindRulePrefixe>()
        {
            new AutoBindRulePrefixe("Tran","Transform"),
            new AutoBindRulePrefixe("OAnim","Animation"),
            new AutoBindRulePrefixe("NAnim","Animator"),
            new AutoBindRulePrefixe("Rect","RectTransform"),
            new AutoBindRulePrefixe("Canvas","Canvas"),
            new AutoBindRulePrefixe("Group","CanvasGroup"),
            new AutoBindRulePrefixe("VGroup","VerticalLayoutGroup"),
            new AutoBindRulePrefixe("HGroup","HorizontalLayoutGroup"),
            new AutoBindRulePrefixe("GGroup","GridLayoutGroup"),
            new AutoBindRulePrefixe("TGroup","ToggleGroup"),
            new AutoBindRulePrefixe("Btn","ButtonPro"),
            new AutoBindRulePrefixe("Img","Image"),
            new AutoBindRulePrefixe("RImg","RawImage"),
            new AutoBindRulePrefixe("Txt","Text"),
            new AutoBindRulePrefixe("TxtM","TextMeshProUGUI"),
            new AutoBindRulePrefixe("Ipt","TMP_InputField"),
            new AutoBindRulePrefixe("Slider","Slider"),
            new AutoBindRulePrefixe("Mask","Mask"),
            new AutoBindRulePrefixe("Mask2D","RectMask2D"),
            new AutoBindRulePrefixe("Tog","Toggle"),
            new AutoBindRulePrefixe("Sbar","Scrollbar"),
            new AutoBindRulePrefixe("SRect","ScrollRect"),
            new AutoBindRulePrefixe("Drop","Dropdown"),
            new AutoBindRulePrefixe("VGridV","LoopGridView"),
            new AutoBindRulePrefixe("HGridV","LoopGridView"),
            new AutoBindRulePrefixe("VListV","LoopListView2"),
            new AutoBindRulePrefixe("HListV","LoopListView2"),
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
        /// 组件的缩略名字映射
        /// </summary>
        public List<AutoBindRulePrefixe> RulePrefixes => m_RulePrefixes;

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
                var _AutoBindGlobalSetting = GetAutoBindGlobalSetting();
                var _PrefixesDict = _AutoBindGlobalSetting.RulePrefixes;
                bool isFindComponent = false;
                foreach (var autoBindRulePrefix in _PrefixesDict)
                {
                    if (autoBindRulePrefix.Prefixe.Equals(str))
                    {
                        comName = autoBindRulePrefix.FullName;
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
        public static AutoBindGlobalSetting GetAutoBindGlobalSetting()
        {
            AutoBindGlobalSetting _AutoBindGlobalSetting = null;
            string[] paths = AssetDatabase.FindAssets("t:AutoBindGlobalSetting");
            if (paths.Length == 0)
            {
                D.Error("不存在AutoBindGlobalSetting");
                return _AutoBindGlobalSetting;
            }
            if (paths.Length > 1)
            {
                D.Error("AutoBindGlobalSetting数量大于1");
                return _AutoBindGlobalSetting;
            }
            string path = AssetDatabase.GUIDToAssetPath(paths[0]);
            _AutoBindGlobalSetting = AssetDatabase.LoadAssetAtPath<AutoBindGlobalSetting>(path);
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

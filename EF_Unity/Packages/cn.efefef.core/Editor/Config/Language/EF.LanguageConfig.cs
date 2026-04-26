/*
 * ================================================
 * Describe:      This script is used to set the editor panel language.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-28 16:14:49
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-06-14 18:21
 * ScriptVersion: 0.3
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;

namespace EasyFramework.Edit
{
    public enum ELanguage
    {
        English = 0,
        中文,
    }

    /// <summary>
    /// The language config in editor panel.
    /// <para>编辑器面板下的语言配置</para>
    /// </summary>
    internal static class LC
    {
        private class LcRoot
        {
            /// <summary>
            /// 
            /// </summary>
            public List <LcItem > LcList { get; set; }
        }
        private class LcItem
        {
            /// <summary>
            /// 名字
            /// </summary>
            public string name { get; set; }
            /// <summary>
            /// 描述
            /// </summary>
            public string desc { get; set; }
            /// <summary>
            /// 语言数组
            /// </summary>
            public List <string > array { get; set; }
        }
        
        public static ELanguage DisPlayLanguage
        {
            get
            {
                if (m_currentIndex == -1)
                    m_currentIndex = EditorPrefs.GetInt(ConfigManager.Project.AppConst.AppPrefix + "LanguageIndex", 0);
                return (ELanguage)m_currentIndex;
            }
            set
            {
                if (m_currentIndex.Equals((int)value))
                    return;
                m_currentIndex = (int)value;
                AppConstConfig.LanguageIndex = m_currentIndex;
                EditorPrefs.SetInt(ConfigManager.Project.AppConst.AppPrefix + "LanguageIndex", m_currentIndex);
            }
        }

        static int m_currentIndex = -1;
        static string m_Separator;
        static string m_AassetsPath;
        static Dictionary<Lc, LcItem> m_Dictionary;

        static void LoadLanguage()
        {
            if (m_currentIndex == -1)
                m_currentIndex = EditorPrefs.GetInt(ConfigManager.Project.AppConst.AppPrefix + "LanguageIndex", 0);
            if (null != m_Dictionary && m_Dictionary.Count != 0) 
                return;
 
            m_AassetsPath = Path.Combine(Utility.Path.GetEfAssetsPath(), "Description/Editorlanguages.json");
            
            LcRoot lcRoot = JsonConvert.DeserializeObject<LcRoot>(File.ReadAllText(m_AassetsPath));
            m_Dictionary = new Dictionary<Lc, LcItem>();
            m_Dictionary.Clear();

            foreach (LcItem lcItem in lcRoot.LcList)
            {
                m_Dictionary.Add(Enum.Parse<Lc>(lcItem.name), lcItem);
            }

            m_Separator = Combine(Lc.S);
        }

        #region Combine

        public static string Combine(Lc lc)
        {
            LoadLanguage();
            return m_Dictionary[lc].array[m_currentIndex];
        }

        public static string Combine(Lc[] lc)
        {
            LoadLanguage();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lc.Length; i++)
            {
                sb.Append($"{m_Dictionary[lc[i]].array[m_currentIndex]}{m_Separator}");
            }

            return sb.ToString();
        }

        #endregion

        [MenuItem("EFTools/Utility/Update Edit Language", priority = 10002)]
        private static void UpdateLanguageConfig()
        {
            m_currentIndex = EditorPrefs.GetInt(ConfigManager.Project.AppConst.AppPrefix + "LanguageIndex", 0);
            if (string.IsNullOrEmpty(m_AassetsPath))
                m_AassetsPath = Path.Combine(Utility.Path.GetEfAssetsPath(), "Description/Editorlanguages.json");

            LcRoot lcRoot = JsonConvert.DeserializeObject<LcRoot>(File.ReadAllText(m_AassetsPath));

            StringBuilder _sb = new StringBuilder();
            _sb.AppendLine("namespace EasyFramework.Edit\n{");
            _sb.AppendLine("\t/// <summary> This script is used to set the editor panel language <summary>");
            _sb.AppendLine("\tpublic enum Lc\n\t{");

            foreach (LcItem lcItem in lcRoot.LcList)
            {
                _sb.AppendLine($"\t\t/// <summary> {lcItem.desc} </summary>");
                _sb.AppendLine($"\t\t{lcItem.name},");
            }

            _sb.AppendLine("\t}\n}");

            string path = $"{Utility.Path.GetEfPath()}/Editor/Config/Language";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            File.WriteAllText($"{path}/EF.LanguageEnum.cs", _sb.ToString());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
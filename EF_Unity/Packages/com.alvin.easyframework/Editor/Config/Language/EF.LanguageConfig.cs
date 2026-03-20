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

using LitJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace EasyFramework.Edit
{
    public enum ELanguage
    {
        English,
        Chinese,
    }

    /// <summary>
    /// The language config in editor panel.
    /// <para>编辑器面板下的语言配置</para>
    /// </summary>
    internal static class LC
    {
        public static ELanguage DisPlayLanguage
        {
            get => _disPlayLanguage;

            set
            {
                if (value == _disPlayLanguage)
                    return;
                
                _disPlayLanguage = (ELanguage)Math.Clamp((int)value, 0, 1);
                EditorPrefs.SetInt(ProjectUtility.Project.AppConst.AppPrefix + "LanguageIndex", (int)value);
                ChangeLanguage();
            }
        }

        static int m_currentIndex;
        static string m_Separator;
        static string m_AassetsPath;
        private static ELanguage _disPlayLanguage;
        static Dictionary<string, string> m_Dictionary;

        static void LoadLanguage()
        {
            if (null != m_Dictionary && m_Dictionary.Count != 0) 
                return;
            
            m_AassetsPath = Path.Combine(Utility.Path.GetEfPath(),
                "Editor Resources/Description/Editorlanguages.json");
            m_currentIndex = EditorPrefs.GetInt(ProjectUtility.Project.AppConst.AppPrefix + "LanguageIndex", 0);
            JsonData jd = JsonMapper.ToObject(File.ReadAllText(m_AassetsPath));
            m_Dictionary = new Dictionary<string, string>();
            m_Dictionary.Clear();

            for (int i = 0; i < jd.Count; i++)
            {
                m_Dictionary.Add(jd[i]["name"].ToString(), jd[i]["array"][m_currentIndex].ToString());
            }

            m_Separator = m_Dictionary["S"];
        }

        #region Combine

        public static string Combine(Lc lc)
        {
            LoadLanguage();
            return m_Dictionary[lc.ToString()];
        }

        public static string Combine(Lc[] lc)
        {
            LoadLanguage();

            StringBuilder _sb = new StringBuilder();
            for (int i = 0; i < lc.Length; i++)
            {
                _sb.Append($"{m_Dictionary[lc[i].ToString()]}{m_Separator}");
            }

            return _sb.ToString();
        }

        #endregion

        #region ChangeLanguage

        /*
         * Change the relevant description language under the Settings panel.
         * 改变设置面板下的相关说明语言
         */
        static void ChangeLanguage()
        {
            string lcPath = Path.Combine(Utility.Path.GetEfPath(), "Runtime/Config/");
            try
            {
                File.Delete(Path.Combine(lcPath, $"LanguagAttribute.cs"));
                File.Delete(Path.Combine(lcPath, $"LanguagAttribute.cs.meta"));

                File.Copy(
                    Path.Combine(lcPath, $"{_disPlayLanguage}~/LanguagAttribute.cs"),
                    Path.Combine(lcPath, $"LanguagAttribute.cs"));
            }
            catch (Exception ex)
            {
                D.Exception(ex.Message);
            }
            //Windows.SettingPanel.EFSettingsPanel.focusedWindow.Close();
            
            // Utility.RefreshUtility.RefreshWithCallback(delegate
            // {
            // });

        }

        static void RefreshEfPanel()
        {
            if (EditorApplication.isUpdating) 
            {
                D.Log("编辑器正在刷新资源，请稍后操作...");
                return;
            }
            //Windows.SettingPanel.EFSettingsPanel.Open(0);
        }
        
        #endregion

        [MenuItem("EFTools/Utility/Update Edit Language", priority = 10002)]
        private static void UpdateLanguageConfig()
        {
            m_currentIndex = EditorPrefs.GetInt(ProjectUtility.Project.AppConst.AppPrefix + "LanguageIndex", 0);
            if (string.IsNullOrEmpty(m_AassetsPath))
                m_AassetsPath = Path.Combine(Utility.Path.GetEfPath(),
                    "Editor Resources//Description/Editorlanguages.json");

            JsonData _jd = JsonMapper.ToObject(File.ReadAllText(m_AassetsPath));

            StringBuilder _sb = new StringBuilder();
            _sb.AppendLine("namespace EasyFramework.Edit\n{");
            _sb.AppendLine("\t/// <summary> This script is used to set the editor panel language <summary>");
            _sb.AppendLine("\tpublic enum Lc\n\t{");

            for (int i = 0; i < _jd.Count; i++)
            {
                _sb.AppendLine($"\t\t/// <summary> {_jd[i]["desc"]} </summary>");
                _sb.AppendLine($"\t\t{_jd[i]["name"]},");
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
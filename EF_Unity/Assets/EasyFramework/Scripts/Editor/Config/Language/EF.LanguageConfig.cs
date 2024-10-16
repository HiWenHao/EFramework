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
        中文,
    }
    /// <summary>
    /// The language config in editor panel.
    /// <para>编辑器面板下的语言配置</para>
    /// </summary>
    internal static class LC
    {
        static int m_currentIndex;
        static string m_Separator;
        static string m_AassetsPath;
        static Dictionary<string, string> m_Dictionary;

        static void LoadLanguage()
        {
            if (null == m_Dictionary || m_Dictionary.Count == 0)
            {
                m_AassetsPath = Path.Combine(Utility.Path.GetEFAssetsPath(), "Description/Editorlanguages.json");
                m_currentIndex = EditorPrefs.GetInt(ProjectUtility.Project.AppConst.AppPrefix + "LanguageIndex", 0);
                JsonData _jd = JsonMapper.ToObject(File.ReadAllText(m_AassetsPath));
                m_Dictionary = new Dictionary<string, string>();
                m_Dictionary.Clear();

                for (int i = 0; i < _jd.Count; i++)
                {
                    m_Dictionary.Add(_jd[i]["name"].ToString(), _jd[i]["array"][m_currentIndex].ToString());
                }

                m_Separator = m_Dictionary["S"];
            }

            if (m_currentIndex != ProjectUtility.Project.LanguageIndex)
            {
                if (ProjectUtility.Project.LanguageIndex >= 0 && ProjectUtility.Project.LanguageIndex <= 1)
                {
                    ChangeLanguage();
                    m_currentIndex = ProjectUtility.Project.LanguageIndex;
                }
            }
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
            string _lcPath = Path.Combine(
                UnityEngine.Application.dataPath,
                ProjectUtility.Path.FrameworkPath[7..],
                "Scripts/Runtime/Config/");
            string _sourcePath = Path.Combine(Utility.Path.GetEFAssetsPath(), "Scripts/Config");
            try
            {
                File.Delete(Path.Combine(_lcPath, $"LanguagAttribute.cs"));
                File.Delete(Path.Combine(_lcPath, $"LanguagAttribute.cs.meta"));

                File.Copy(
                    Path.Combine(_sourcePath, GetCurrentName(), $"LanguagAttribute.cs"), 
                    Path.Combine(_lcPath, $"LanguagAttribute.cs"));
            }
            catch (Exception ex)
            {
                D.Exception(ex.Message);
            }
            AssetDatabase.Refresh();
        }
        static string GetCurrentName()
        {
            return ProjectUtility.Project.LanguageIndex switch
            {
                1 => "Chinese",
                _ => "English",
            };
        }
        #endregion

        [MenuItem("EFTools/Utility/Update Edit Language", priority = 10002)]
        static void UpdateLanguageConfig()
        {
            m_currentIndex = EditorPrefs.GetInt(ProjectUtility.Project.AppConst.AppPrefix + "LanguageIndex", 0);
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

            File.WriteAllText($"{ProjectUtility.Path.FrameworkPath}/Scripts/Editor/Config/Language/EF.LanguageEnum.cs", _sb.ToString());
            AssetDatabase.Refresh();
        }
    }
}

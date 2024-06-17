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
        static Dictionary<string, List<string>> m_Dictionary;

        static void LoadLanguage()
        {
            if (m_currentIndex != ProjectUtility.Project.LanguageIndex)
            {
                if (ProjectUtility.Project.LanguageIndex >= 0 && ProjectUtility.Project.LanguageIndex <= 1)
                {
                    ChangeLanguage(m_currentIndex, ProjectUtility.Project.LanguageIndex);
                    m_currentIndex = ProjectUtility.Project.LanguageIndex;
                }
            }

            if (null == m_Dictionary || m_Dictionary.Count == 0)
            {
                m_currentIndex = EditorPrefs.GetInt(ProjectUtility.Project.AppConst.AppPrefix + "LanguageIndex", 0);
                JsonData _jd = JsonMapper.ToObject(File.ReadAllText($"{ProjectUtility.Path.FrameworkPath}/EFAssets/Configs/languages.json"));
                m_Dictionary = new Dictionary<string, List<string>>();
                m_Dictionary.Clear();

                for (int i = 0; i < _jd.Count; i++)
                {
                    m_Dictionary.Add(_jd[i]["name"].ToString(), new List<string>
                    {
                        _jd[i]["array"][0].ToString(),
                        _jd[i]["array"][1].ToString()
                    });
                }

                m_Separator = m_Dictionary["S"][m_currentIndex];
            }
        }

        #region Combine
        public static string Combine(string text1)
        {
            LoadLanguage();
            return m_Dictionary[text1][m_currentIndex];
        }
        public static string Combine(string text1, string text2)
        {
            LoadLanguage();
            return $"{m_Dictionary[text1][m_currentIndex]}{m_Separator}{m_Dictionary[text2][m_currentIndex]}";
        }
        public static string Combine(string text1, string text2, string text3)
        {
            LoadLanguage();
            return $"{m_Dictionary[text1][m_currentIndex]}{m_Separator}{m_Dictionary[text2][m_currentIndex]}{m_Separator}{m_Dictionary[text3][m_currentIndex]}";
        }
        public static string Combine(string text1, string text2, string text3, string text4)
        {
            LoadLanguage();
            return $"{m_Dictionary[text1][m_currentIndex]}{m_Separator}{m_Dictionary[text2][m_currentIndex]}{m_Separator}{m_Dictionary[text3][m_currentIndex]}{m_Separator}{m_Dictionary[text4][m_currentIndex]}";
        }
        public static string Combine(string text1, string text2, string text3, string text4, string text5)
        {
            LoadLanguage();
            return $"{m_Dictionary[text1][m_currentIndex]}{m_Separator}{m_Dictionary[text2][m_currentIndex]}{m_Separator}{m_Dictionary[text3][m_currentIndex]}{m_Separator}{m_Dictionary[text4][m_currentIndex]}{m_Separator}{m_Dictionary[text5][m_currentIndex]}";
        }        
        #endregion

        #region ChangeLanguage
        /*
         * Change the relevant description language under the Settings panel.
         * 改变设置面板下的相关说明语言
         */
        static void ChangeLanguage(int nowIndex, int nextIndex)
        {
            string _lcPath = Path.Combine(ProjectUtility.Path.FrameworkPath[7..], "Scripts/Runtime/Config/");
            string _path = Path.Combine(UnityEngine.Application.dataPath, _lcPath);
            string _nameNow = GetNameWithIndex(nowIndex);
            string _nameNext = GetNameWithIndex(nextIndex);

            try
            {
                File.Delete(Path.Combine(_path, $"{_nameNow}/LanguagAttribute.cs"));
                File.Delete(Path.Combine(_path, $"{_nameNow}/LanguagAttribute.cs.meta"));
                if (!File.Exists(Path.Combine(_path, $"{_nameNext}/LanguagAttribute.cs")))
                {
                    File.Copy(Path.Combine(_path, $"{_nameNext}~/LanguagAttribute.cs"), Path.Combine(_path, $"{_nameNext}/LanguagAttribute.cs"));
                }
            }
            catch (Exception ex)
            {
                D.Exception(ex.Message);
            }
            AssetDatabase.Refresh();
        }
        static string GetNameWithIndex(int index)
        {
            return index switch
            {
                1 => "Chinese",
                _ => "English",
            };
        }
        #endregion
    }
}

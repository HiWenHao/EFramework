/* 
 * ================================================
 * Describe:      This script is used to set the editor panel language.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-28 16:14:49
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-06-07 17:12:32
 * ScriptVersion: 0.1
 * ===============================================
*/

using SimpleJSON;
using System;
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
    public static class LC
    {
        static bool m_init;
        static int m_currentIndex;
        static string m_Separator;
        static Languages m_Languages;

        static void LoadLanguage()
        {
            if (!m_init)
            {
                m_init = true;
                m_currentIndex = EditorPrefs.GetInt(ProjectUtility.Project.AppConst.AppPrefix + "LanguageIndex", 0);

                LoadLanguage();
            }

            if (m_currentIndex != ProjectUtility.Project.LanguageIndex)
            {
                if (ProjectUtility.Project.LanguageIndex >= 0 && ProjectUtility.Project.LanguageIndex <= 1)
                {
                    ChangeLanguage(m_currentIndex, ProjectUtility.Project.LanguageIndex);
                    m_currentIndex = ProjectUtility.Project.LanguageIndex;
                }
            }

            if (null == m_Languages)
            {
                m_Languages = new Lc(file => JSON.Parse(File.ReadAllText($"{UnityEngine.Application.dataPath}/EasyFramework/EFAssets/Configs/{file}.json"))).Languages;
                if (m_currentIndex >= m_Languages["S"].Array.Count)
                {
                    m_Separator = "";
                }
                else
                {
                    m_Separator = m_Languages["S"].Array[m_currentIndex];
                }
            }
        }

        #region Combine
        public static string Combine(string text1)
        {
            LoadLanguage();
            return m_Languages.Get(text1).Array[m_currentIndex];
        }
        public static string Combine(string text1, string text2)
        {
            LoadLanguage();
            return $"{m_Languages[text1].Array[m_currentIndex]}{m_Separator}{m_Languages[text2].Array[m_currentIndex]}";
        }
        public static string Combine(string text1, string text2, string text3)
        {
            LoadLanguage();
            return $"{m_Languages[text1].Array[m_currentIndex]}{m_Separator}{m_Languages[text2].Array[m_currentIndex]}{m_Separator}{m_Languages[text3].Array[m_currentIndex]}";
        }
        public static string Combine(string text1, string text2, string text3, string text4)
        {
            LoadLanguage();
            return $"{m_Languages[text1].Array[m_currentIndex]}{m_Separator}{m_Languages[text2].Array[m_currentIndex]}{m_Separator}{m_Languages[text3].Array[m_currentIndex]}{m_Separator}{m_Languages[text4].Array[m_currentIndex]}";
        }
        public static string Combine(string text1, string text2, string text3, string text4, string text5)
        {
            LoadLanguage();
            return $"{m_Languages[text1].Array[m_currentIndex]}{m_Separator}{m_Languages[text2].Array[m_currentIndex]}{m_Separator}{m_Languages[text3].Array[m_currentIndex]}{m_Separator}{m_Languages[text4].Array[m_currentIndex]}{m_Separator}{m_Languages[text5].Array[m_currentIndex]}";
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
                File.Copy(Path.Combine(_path, $"{_nameNext}~/LanguagAttribute.cs"), Path.Combine(_path, $"{_nameNext}/LanguagAttribute.cs"));
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

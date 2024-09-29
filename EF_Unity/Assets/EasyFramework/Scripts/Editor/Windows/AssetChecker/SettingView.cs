/*
 * ================================================
 * Describe:      This script is used to show the resource detection overview. Let's thank LiangZG!!!!!
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-06-06 15:29:47
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-06-06 15:29:47
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.Edit;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Windows.AssetChecker
{
    internal class SettingView<T> where T : SettingBase, new()
    {
        private string m_FilePath;
        private Vector2 m_ScrollPos = Vector2.zero;

        internal List<T> Settings;
        public SettingView()
        {
            Settings = new List<T>();
        }
        ~SettingView()
        {
            Settings.Clear();
            Settings = null;
        }

        internal void OnGUI()
        {
            EditorGUILayout.Separator();
            GUILayout.Label(LC.Combine(new Lc[] { Lc.Config, Lc.Query, Lc.Settings }));

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Clear, Lc.Config, Lc.File }), "ButtonLeft", GUILayout.Width(120)))
            {
                if (File.Exists(m_FilePath))
                {
                    File.Delete(m_FilePath);
                    File.Delete($"{m_FilePath}.meta");
                    AssetDatabase.Refresh();
                }
            }
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Load, Lc.Config, Lc.File }), "ButtonRight", GUILayout.Width(120)))
            {
                m_FilePath = EditorUtility.OpenFilePanel(LC.Combine(Lc.Open), Application.dataPath, "xml");
                if (File.Exists(m_FilePath))
                {
                    Settings.Clear();
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(File.ReadAllText(m_FilePath));

                    XmlNode rootEle = xmlDoc.SelectSingleNode("Configs");
                    if (null != rootEle)
                    {
                        foreach (XmlNode childNode in rootEle.ChildNodes)
                        {
                            if (childNode is not XmlElement childEle) continue;

                            T msb = new T();
                            msb.Read(childEle);
                            Settings.Add(msb);
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();

            if (Settings != null)
            {
                m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos, "PopupCurveEditorBackground");
                for (int i = 0; i < Settings.Count; i++)
                {
                    GUILayout.Space(5);
                    DrawSetting(Settings[i]);
                    EditorGUILayout.Separator();
                }
                GUILayout.EndScrollView();
            }

            EditorGUILayout.Separator();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Save, Lc.Config, Lc.File })))
            {
                if (string.IsNullOrEmpty(m_FilePath))
                {
                    string _filePath = EditorUtility.SaveFilePanel(LC.Combine(Lc.Save), Application.dataPath, "new checker file", "xml");
                    m_FilePath = _filePath.Replace(Application.dataPath, "Assets");
                }
                if (!string.IsNullOrEmpty(m_FilePath))
                    SaveSettingRule(m_FilePath);
                AssetDatabase.Refresh();
            }
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Add, Lc.Config })))
            {
                T msb = new T();
                msb.Folder.Add("Assets");
                Settings.Add(msb);
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 保存设置配置
        /// </summary>
        private void SaveSettingRule(string filePath)
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlElement rootEle = xmlDoc.CreateElement("Configs");
            for (int i = 0; i < Settings.Count; i++)
            {
                T msb = Settings[i];
                XmlElement ele = xmlDoc.CreateElement("Setting");
                msb.Write(xmlDoc, ele);
                rootEle.AppendChild(ele);
            }
            xmlDoc.AppendChild(rootEle);
            xmlDoc.Save(filePath);
        }

        /// <summary>
        /// 绘制配置设置信息
        /// </summary>
        private void DrawSetting(T modelSetting)
        {
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                modelSetting.IsUnfold = EditorGUILayout.Foldout(modelSetting.IsUnfold, modelSetting.AssetDesc);

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("X", GUILayout.Width(30)))
                {
                    Settings.Remove(modelSetting);
                }
                GUILayout.EndHorizontal();

                if (modelSetting.IsUnfold)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(30);
                    GUILayout.Label(LC.Combine(new Lc[] { Lc.Assets, Lc.Description }), GUILayout.Width(120F));
                    modelSetting.AssetDesc = GUILayout.TextField(modelSetting.AssetDesc);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(30);
                        if (GUILayout.Button(LC.Combine(new Lc[] { Lc.File, Lc.Catalogue }), GUILayout.Width(120F)))
                            modelSetting.Folder.Add(string.Empty);

                        GUILayout.BeginVertical();
                        for (int i = modelSetting.Folder.Count - 1; i >= 0; i--)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.TextField(modelSetting.Folder[i]);
                            if (GUILayout.Button("...", GUILayout.Width(30f)))
                            {
                                string path = EditorUtility.OpenFolderPanel(LC.Combine(new Lc[] { Lc.Path, Lc.Select }), Application.dataPath, "");
                                string _rep = path.Replace(Application.dataPath, "Assets");
                                if (!modelSetting.Folder.Contains(_rep))
                                    modelSetting.Folder[i] = _rep;
                            }
                            if (GUILayout.Button("X", GUILayout.Width(30f)))
                            {
                                modelSetting.Folder.RemoveAt(i);
                            }
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(30);
                        modelSetting.OnGUI();
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
        }
    }
}

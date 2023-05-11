/* 
 * ================================================
 * Describe:      This script is used to find the object with missing scripts.   Possible reference: --> plyoung <-- Thanks in advance. ^_^
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-11 16:07:09
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-05-11 16:07:09
 * ScriptVersion: 0.1
 * ===============================================
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XHTools;

namespace EasyFramework.Windows
{
    /// <summary>
    /// Find the object with missing scripts
    /// </summary>
    public class MissingScriptsListWindow : EditorWindow
    {
        private class Info
        {
            public Object obj;
            public GUIContent path;
        }

        int m_Opt = 0;
        int m_TempOpt = 0;
        int m_MaxCount = 0;
        int m_TempCount = 0;
        Vector2 m_Scroll;
        List<Info> m_Entries = new List<Info>();

        private static GUIStyle PingButtonStyle;
        static MissingScriptsListWindow _window;

        private static readonly GUIContent[] m_OptContents = new[] 
        {
            new GUIContent("In Active Scenes", "在活动场景中"), 
            new GUIContent("On Prefabs", "为预制件")
        };

        [MenuItem("EFTools/Asset/Missing Scripts Find", priority = 100)]
        private static void ShowWindow()
        {
            _window = GetWindow<MissingScriptsListWindow>(true, "Missing Scripts List", true);
            _window.Show();
            _window.minSize = new Vector2(230f, 300f);
            _window.Find();
        }

        private void OnGUI()
        {
            PingButtonStyle ??= new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleLeft };

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Find Again", "查找")))
            {
                Find();
            }
            m_Opt = EditorGUILayout.Popup(m_Opt, m_OptContents);
            if (m_Opt != m_TempOpt)
            {
                m_TempOpt = m_Opt;
                Find();
            }
            EditorGUILayout.EndHorizontal();
            if (m_MaxCount != 0)
                EditorGUILayout.LabelField(new GUIContent($"Missing Count:  [ {m_MaxCount} ] ", "丢失数量"));
            EditorGUILayout.Space();

            ShowListInfo();
        }

        void ShowListInfo()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            for (int i = 0; i < m_MaxCount - 1; i++)
            {
                Info _info = m_Entries[i];
                if (!_info.obj)
                {
                    m_TempCount--;
                    continue;
                }
                EditorGUILayout.BeginHorizontal(EditorStyles.boldLabel);
                if (GUILayout.Button(_info.obj.name, PingButtonStyle))
                {
                    EditorGUIUtility.PingObject(_info.obj);
                    Selection.activeObject = _info.obj;
                }
                if (GUILayout.Button("Path", GUILayout.Width(50f)))
                {
                    D.Log(_info.path.text);
                }
                EditorGUILayout.EndHorizontal();
            }
            if (m_TempCount != m_MaxCount)
            {
                Find();
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("----- The End -----", new GUIStyle(EditorStyles.whiteLabel) { alignment = TextAnchor.MiddleCenter });
            EditorGUILayout.Space(18f);
            EditorGUILayout.EndScrollView();
        }

        void Find()
        {
            for (int i = m_MaxCount - 1; i >= 0; i--)
                m_Entries.RemoveAt(i);
            m_Entries.Clear();

            GameObject[] gos = Resources.FindObjectsOfTypeAll<GameObject>();
            m_MaxCount = gos.Length;

            for (int i = 0; i < m_MaxCount; i++)
            {
                GameObject _go = gos[i];


                if ((m_Opt == 0 && !_go.scene.IsValid()) ||
                    (m_Opt == 1 && _go.scene.IsValid())) continue;

                bool _hasLost = false;
                Component[] cos = _go.GetComponents<Component>();
                foreach (var co in cos)
                {
                    if (co == null)
                    {
                        _hasLost = true;
                        break;
                    }
                }
                if (!_hasLost) continue;

                Transform tr = _go.transform.parent;
                Info nfo = new Info()
                {
                    path = new GUIContent(_go.name),
                    obj = _go
                };
                m_Entries.Add(nfo);
                while (tr != null)
                {
                    nfo.path.text = $"{tr.name} / {nfo.path.text}";
                    tr = tr.parent;
                }
            }

            m_Entries.Sort((a, b) => a.path.text.CompareTo(b.path.text));
            m_MaxCount = m_Entries.Count;
            m_TempCount = m_MaxCount;
        }
    }
}

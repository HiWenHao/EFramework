/* 
 * ================================================
 * Describe:      This script is used to compare two GameObjects or two Components and return the corresponding difference tree.
 * Author:        罐子（Lawliet）
 * CreationTime:  2023-05-16 14:21:29
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-05-09 10:28:29
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.Edit;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Windows
{
    namespace PrefabsCompare
    {
        /// <summary>
        /// Compare two GameObjects or two Components.
        /// </summary>
        public class CompareEditorWindow : EditorWindow
        {
            int m_oldMissIndex;

            GUIStyle m_MissingLabel;
            GUIStyle m_ToolButtonStyle;
            StringBuilder m_MissLeft = new StringBuilder();
            StringBuilder m_MissRight = new StringBuilder();

            [System.NonSerialized]
            private bool m_Initialized;
            private bool m_MissComponent;

            private CompareView m_LeftView;
            private CompareView m_RightView;

            [MenuItem("Assets/EF/Prefabs Compare", false, 20)]
            static void PrefabsCompares()
            {
                var gameObjects = Selection.gameObjects;

                if (gameObjects.Length == 1)
                    ComparePrefab(gameObjects[0], null);
                else
                    ComparePrefab(gameObjects[0], gameObjects[1]);
            }

            [MenuItem("EFTools/Assets/Prefabs Compare")]
            static CompareEditorWindow OpenWindow() => ComparePrefab(null, null);
            static CompareEditorWindow ComparePrefab(GameObject left, GameObject right)
            {
                CompareEditorWindow window = GetWindow<CompareEditorWindow>(false, "Prefabs Compare");
                window.Focus();
                window.Repaint();
                window.Compare(left, right);
                return window;
            }

            private void OnDisable()
            {
                OnDisableView();
            }

            private void OnDestroy()
            {
                CompareData.CompareCall -= Compare;

                OnDisableView();
            }

            private void InitIfNeeded()
            {
                if (!m_Initialized)
                {
                    m_LeftView ??= new CompareView(true);
                    m_RightView ??= new CompareView(false);

                    InitView(m_LeftView);
                    InitView(m_RightView);

                    CompareData.CompareCall += Compare;

                    m_LeftView.onClickItem += OnClickItem;
                    m_RightView.onClickItem += OnClickItem;

                    m_Initialized = true;
                }

                m_MissingLabel = new GUIStyle("label")
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal =
                    {
                        textColor = Color.yellow,
                    }
                };

                m_ToolButtonStyle = new GUIStyle("ToolBarButton");
            }

            private void InitView(CompareView view)
            {
                view.Init();
                view.onGOTreeExpandedStateChanged += OnExpandedStateChanged;
            }

            private void OnDisableView()
            {
                if (CompareData.LeftPrefabContent != null)
                {
                    PrefabUtility.UnloadPrefabContents(CompareData.LeftPrefabContent);
                    CompareData.LeftPrefabContent = null;
                    CompareData.LeftPrefabPath = "";
                }

                if (CompareData.RightPrefabContent != null)
                {
                    PrefabUtility.UnloadPrefabContents(CompareData.RightPrefabContent);
                    CompareData.RightPrefabContent = null;
                    CompareData.RightPrefabPath = "";
                }
                if (CompareData.RootInfo != null)
                {
                    CompareData.RootInfo.Children.Clear();
                    CompareData.RootInfo.Components.Clear();
                    CompareData.RootInfo.Children = null;
                    CompareData.RootInfo.Components = null;
                    CompareData.RootInfo = null;
                }

                if (null != m_LeftView)
                {
                    m_LeftView.onClickItem -= OnClickItem;
                    m_LeftView.onGOTreeExpandedStateChanged -= OnExpandedStateChanged;
                    m_LeftView.Destroy();
                    m_LeftView = null;
                }
                if (null != m_RightView)
                {
                    m_RightView.onClickItem -= OnClickItem;
                    m_RightView.onGOTreeExpandedStateChanged -= OnExpandedStateChanged;
                    m_RightView.Destroy();
                    m_RightView = null;
                }
            }

            private void OnClickItem(GameObjectCompareInfo info)
            {
                m_MissComponent = info.AllEqual();
                if (m_MissComponent || m_oldMissIndex == info.ID)
                    return;

                m_oldMissIndex = info.ID;

                m_MissLeft.Clear();
                m_MissRight.Clear();
                switch (info.MissType)
                {
                    case MissType.missLeft:
                        m_MissRight.AppendLine(LC.Language.Pc_MissObjectLeft);
                        break;
                    case MissType.missRight:
                        m_MissLeft.AppendLine(LC.Language.Pc_MissObjectRight);
                        break;
                    default:
                        break;
                }

                foreach (var item in info.Components)
                {
                    if (item.MissType == MissType.allExist || m_oldMissIndex == item.ID)
                        continue;

                    switch (item.MissType)
                    {
                        case MissType.missLeft:
                            if (m_MissLeft.Length == 0)
                            {
                                m_MissLeft.AppendLine($"{LC.Language.Lost}:\n");
                            }
                            m_MissLeft.AppendLine(item.Name);
                            break;
                        case MissType.missRight:
                            if (m_MissRight.Length == 0)
                            {
                                m_MissRight.AppendLine($"{LC.Language.Lost}:\n");
                            }
                            m_MissRight.AppendLine(item.Name);
                            break;
                        default:
                            break;
                    }
                }
            }

            private void OnExpandedStateChanged(int id, bool isLeft, bool expanded)
            {
                if (isLeft)
                {
                    m_RightView.SetExpanded(id, expanded);
                }
                else
                {
                    m_LeftView.SetExpanded(id, expanded);
                }
            }

            private void OnGUI()
            {
                InitIfNeeded();

                OnToolBar();

                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();

                m_LeftView.OnGUI();

                m_RightView.OnGUI();

                EditorGUILayout.EndHorizontal();

                MissComponents();
            }

            private void OnToolBar()
            {
                EditorGUILayout.BeginHorizontal(new GUIStyle("ToolBar"));

                if (GUILayout.Button(LC.Language.Compare, m_ToolButtonStyle, GUILayout.Width(80.0f)))
                {
                    Compare();
                }

                GUILayout.FlexibleSpace();

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    CompareData.ShowEqual = GUILayout.Toggle(CompareData.ShowEqual, new GUIContent(PrefabsCompareStyle.successImg, LC.Language.Pc_ShowEqual), m_ToolButtonStyle, GUILayout.Width(30.0f));

                    CompareData.ShowMiss = GUILayout.Toggle(CompareData.ShowMiss, new GUIContent(PrefabsCompareStyle.inconclusiveImg, LC.Language.Pc_ShowMiss), m_ToolButtonStyle, GUILayout.Width(30.0f));

                    if (check.changed)
                    {
                        //刷新列表
                        CompareData.onShowStateChange?.Invoke();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            private void Compare()
            {
                if (m_LeftView.GameObjects != null && m_RightView.GameObjects != null)
                {
                    CompareImplement();
                }
                else
                {
                    CompareData.RootInfo = null;

                    m_LeftView.Reload();
                    m_RightView.Reload();
                }
            }

            private void Compare(GameObject left, GameObject right)
            {
                InitIfNeeded();

                m_LeftView.GameObjects = left;
                m_RightView.GameObjects = right;
                if (m_LeftView.GameObjects && m_RightView.GameObjects)
                {
                    CompareImplement();
                }
            }

            private void MissComponents()
            {
                if (m_MissComponent)
                {
                    GUILayout.Space(EditorGUIUtility.singleLineHeight);
                    return;
                }
                EditorGUILayout.BeginHorizontal("box");
                GUILayout.Label(new GUIContent(m_MissLeft.ToString(), LC.Language.Pc_MissContentsLeft), ChangedLabel(true), GUILayout.ExpandWidth(true));
                GUILayout.Label(new GUIContent(m_MissRight.ToString(), LC.Language.Pc_MissContentsRight), ChangedLabel(false), GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();
            }

            private void CompareImplement()
            {
                CompareData.LeftPrefabPath = AssetDatabase.GetAssetPath(m_LeftView.GameObjects);
                CompareData.RightPrefabPath = AssetDatabase.GetAssetPath(m_RightView.GameObjects);

                CompareData.LeftPrefabContent = PrefabUtility.LoadPrefabContents(CompareData.LeftPrefabPath);
                CompareData.RightPrefabContent = PrefabUtility.LoadPrefabContents(CompareData.RightPrefabPath);

                CompareData.RootInfo = CompareUtility.ComparePrefab(CompareData.LeftPrefabContent, CompareData.RightPrefabContent);


                m_LeftView.Reload();
                m_RightView.Reload();
            }

            GUIStyle ChangedLabel(bool isLeft)
            {
                if (isLeft)
                    m_MissingLabel.alignment = TextAnchor.MiddleLeft;
                else
                    m_MissingLabel.alignment = TextAnchor.MiddleRight;
                return m_MissingLabel;
            }
        }
    }
}

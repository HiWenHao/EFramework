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
            int _oldMissIndex;

            GUIStyle _missingLabel;
            GUIStyle _toolButtonStyle;
            StringBuilder _missLeft = new StringBuilder();
            StringBuilder _missRight = new StringBuilder();

            [System.NonSerialized]
            private bool _initialized;
            private bool _missComponent;

            private CompareView _leftView;
            private CompareView _rightView;

            [MenuItem("Assets/EF/Prefabs Compare", false, 20)]
            static void PrefabsCompares()
            {
                if (Selection.count > 2)
                {
                    D.Warning("The prefabs comparison tool can only compare two at a time.."); 
                    return;
                }
                var gameObjects = Selection.gameObjects;
                if (Selection.count != 0 && gameObjects.Length == 0)
                {
                    D.Warning("Need select the type of gameObject.");
                    return;
                }
                if (gameObjects.Length == 1)
                    ComparePrefab(gameObjects[0], null);
                else
                    ComparePrefab(gameObjects[0], gameObjects[1]);
            }

            [MenuItem("EFTools/Tools/Prefabs Compare", priority = 201)]
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
                if (!_initialized)
                {
                    _leftView ??= new CompareView(true);
                    _rightView ??= new CompareView(false);

                    InitView(_leftView);
                    InitView(_rightView);

                    CompareData.CompareCall += Compare;

                    _leftView.onClickItem += OnClickItem;
                    _rightView.onClickItem += OnClickItem;

                    _initialized = true;
                }

                _missingLabel = new GUIStyle("label")
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal =
                    {
                        textColor = Color.yellow,
                    }
                };

                _toolButtonStyle = new GUIStyle("ToolBarButton");
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

                if (null != _leftView)
                {
                    _leftView.onClickItem -= OnClickItem;
                    _leftView.onGOTreeExpandedStateChanged -= OnExpandedStateChanged;
                    _leftView.Destroy();
                    _leftView = null;
                }
                if (null != _rightView)
                {
                    _rightView.onClickItem -= OnClickItem;
                    _rightView.onGOTreeExpandedStateChanged -= OnExpandedStateChanged;
                    _rightView.Destroy();
                    _rightView = null;
                }
            }

            private void OnClickItem(GameObjectCompareInfo info)
            {
                _missComponent = info.AllEqual();
                if (_missComponent || _oldMissIndex == info.ID)
                    return;

                _oldMissIndex = info.ID;

                _missLeft.Clear();
                _missRight.Clear();
                switch (info.MissType)
                {
                    case MissType.missLeft:
                        _missRight.AppendLine(LC.Combine(new Lc[] { Lc.Left, Lc.Missing, Lc.Object }) + ", " + LC.Combine(new Lc[] { Lc.Or, Lc.Position, Lc.Different }));
                        break;
                    case MissType.missRight:
                        _missLeft.AppendLine(LC.Combine(new Lc[] { Lc.Right, Lc.Missing, Lc.Object }) + ", " + LC.Combine(new Lc[] { Lc.Or, Lc.Position, Lc.Different }));
                        break;
                    default:
                        break;
                }

                foreach (var item in info.Components)
                {
                    if (item.MissType == MissType.allExist || _oldMissIndex == item.ID)
                        continue;

                    switch (item.MissType)
                    {
                        case MissType.missLeft:
                            if (_missLeft.Length == 0)
                            {
                                _missLeft.AppendLine($"{LC.Combine(Lc.Lost)}:\n");
                            }
                            _missLeft.AppendLine(item.Name);
                            break;
                        case MissType.missRight:
                            if (_missRight.Length == 0)
                            {
                                _missRight.AppendLine($"{LC.Combine(Lc.Lost)}:\n");
                            }
                            _missRight.AppendLine(item.Name);
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
                    _rightView.SetExpanded(id, expanded);
                }
                else
                {
                    _leftView.SetExpanded(id, expanded);
                }
            }

            private void OnGUI()
            {
                InitIfNeeded();

                OnToolBar();

                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();

                _leftView.OnGUI();

                _rightView.OnGUI();

                EditorGUILayout.EndHorizontal();

                MissComponents();
            }

            private void OnToolBar()
            {
                EditorGUILayout.BeginHorizontal(new GUIStyle("ToolBar"));

                if (GUILayout.Button(LC.Combine(Lc.Compare), _toolButtonStyle, GUILayout.Width(80.0f)))
                {
                    Compare();
                }

                GUILayout.FlexibleSpace();

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    CompareData.ShowEqual = GUILayout.Toggle(CompareData.ShowEqual, new GUIContent(PrefabsCompareStyle.successImg, LC.Combine(new Lc[] { Lc.Display, Lc.Consistency })), _toolButtonStyle, GUILayout.Width(30.0f));

                    CompareData.ShowMiss = GUILayout.Toggle(CompareData.ShowMiss, new GUIContent(PrefabsCompareStyle.inconclusiveImg, LC.Combine(new Lc[] { Lc.Display, Lc.Simplex })), _toolButtonStyle, GUILayout.Width(30.0f));

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
                if (_leftView.GameObjects != null && _rightView.GameObjects != null)
                {
                    CompareImplement();
                }
                else
                {
                    CompareData.RootInfo = null;

                    _leftView.Reload();
                    _rightView.Reload();
                }
            }

            private void Compare(GameObject left, GameObject right)
            {
                InitIfNeeded();

                _leftView.GameObjects = left;
                _rightView.GameObjects = right;
                if (_leftView.GameObjects && _rightView.GameObjects)
                {
                    CompareImplement();
                }
            }

            private void MissComponents()
            {
                if (_missComponent)
                {
                    GUILayout.Space(EditorGUIUtility.singleLineHeight);
                    return;
                }
                EditorGUILayout.BeginHorizontal("box");
                GUILayout.Label(new GUIContent(_missLeft.ToString(), LC.Combine(new Lc[] { Lc.Left, Lc.Lost, Lc.Content })), ChangedLabel(true), GUILayout.ExpandWidth(true));
                GUILayout.Label(new GUIContent(_missRight.ToString(), LC.Combine(new Lc[] { Lc.Right, Lc.Lost, Lc.Content })), ChangedLabel(false), GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();
            }

            private void CompareImplement()
            {
                CompareData.LeftPrefabPath = AssetDatabase.GetAssetPath(_leftView.GameObjects);
                CompareData.RightPrefabPath = AssetDatabase.GetAssetPath(_rightView.GameObjects);

                CompareData.LeftPrefabContent = PrefabUtility.LoadPrefabContents(CompareData.LeftPrefabPath);
                CompareData.RightPrefabContent = PrefabUtility.LoadPrefabContents(CompareData.RightPrefabPath);

                CompareData.RootInfo = CompareUtility.ComparePrefab(CompareData.LeftPrefabContent, CompareData.RightPrefabContent);


                _leftView.Reload();
                _rightView.Reload();
            }

            GUIStyle ChangedLabel(bool isLeft)
            {
                if (isLeft)
                    _missingLabel.alignment = TextAnchor.MiddleLeft;
                else
                    _missingLabel.alignment = TextAnchor.MiddleRight;
                return _missingLabel;
            }
        }
    }
}

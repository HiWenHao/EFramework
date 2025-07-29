using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace EasyFramework.Windows
{
    namespace PrefabsCompare
    {
        public class CompareView
        {
            /// <summary> 需要对比的GameObject </summary>
            public GameObject GameObjects { get; set; }

            /// <summary> GameObject树视图的状态 </summary>
            private TreeViewState _goTreeState;

            /// <summary> GameObject树视图 </summary>
            private GameObjectTreeView m_goTree;

            /// <summary> 左边还是右边的视图 </summary>
            private bool _isLeft;

            /// <summary> GameObject树结构展开状态变更回调 </summary>
            public Action<int, bool, bool> onGOTreeExpandedStateChanged
            {
                get { return m_goTree.onExpandedStateChanged; }
                set { m_goTree.onExpandedStateChanged = value; }
            }

            /// <summary>
            /// 单击GameObject树回调
            /// </summary>
            public Action<GameObjectCompareInfo> onClickItem
            {
                get { return m_goTree.onClickItem; }
                set { m_goTree.onClickItem = value; }
            }

            public CompareView(bool isLeft)
            {
                _isLeft = isLeft;

                if (GameObjects == null)
                {
                    string prefabPath = "";

                    if (_isLeft)
                    {
                        prefabPath = CompareData.LeftPrefabPath;
                    }
                    else
                    {
                        prefabPath = CompareData.RightPrefabPath;
                    }

                    if (!string.IsNullOrWhiteSpace(prefabPath))
                    {
                        GameObjects = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    }
                }
            }

            public void Init()
            {
                _goTreeState ??= new TreeViewState();

                m_goTree = new GameObjectTreeView(_goTreeState, CompareData.RootInfo, _isLeft);

                CompareData.onShowStateChange += OnShowStateChange;
            }

            public void Destroy()
            {
                CompareData.onShowStateChange -= OnShowStateChange;
                _goTreeState = null;
                m_goTree = null;
            }

            public void OnGUI()
            {
                EditorGUILayout.BeginVertical();

                OnToolBar();

                OnTreeView();

                EditorGUILayout.EndVertical();
            }

            private void OnToolBar()
            {
                using var check = new EditorGUI.ChangeCheckScope();
                GameObjects = EditorGUILayout.ObjectField(GameObjects, typeof(GameObject), false) as GameObject;
            }

            private void OnTreeView()
            {
                if (_goTreeState.scrollPos != CompareData.GameObjectTreeScroll)
                {
                    _goTreeState.scrollPos = CompareData.GameObjectTreeScroll;
                }

                m_goTree.OnGUI(GUILayoutUtility.GetRect(0, 100000, 0, 100000));

                if (_goTreeState.scrollPos != CompareData.GameObjectTreeScroll)
                {
                    CompareData.GameObjectTreeScroll = _goTreeState.scrollPos;
                }
            }

            /// <summary>
            /// 展开对应ID的节点
            /// </summary>
            /// <param name="id"></param>
            /// <param name="expanded"></param>
            public void SetExpanded(int id, bool expanded)
            {
                m_goTree.SetExpanded(id, expanded);
            }

            /// <summary>
            /// 重刷
            /// </summary>
            public void Reload()
            {
                m_goTree.Reload(CompareData.RootInfo);
            }

            /// <summary>
            /// 显示类型状态改变
            /// </summary>
            private void OnShowStateChange()
            {
                m_goTree.Reload(CompareData.RootInfo);
            }
        }
    }
}
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
            private TreeViewState m_GOTreeState;

            /// <summary> GameObject树视图 </summary>
            private GameObjectTreeView m_GOTree;

            /// <summary> 左边还是右边的视图 </summary>
            private bool m_IsLeft;

            /// <summary> GameObject树结构展开状态变更回调 </summary>
            public Action<int, bool, bool> onGOTreeExpandedStateChanged
            {
                get { return m_GOTree.onExpandedStateChanged; }
                set { m_GOTree.onExpandedStateChanged = value; }
            }

            /// <summary>
            /// 单击GameObject树回调
            /// </summary>
            public Action<GameObjectCompareInfo> onClickItem
            {
                get { return m_GOTree.onClickItem; }
                set { m_GOTree.onClickItem = value; }
            }

            public CompareView(bool isLeft)
            {
                m_IsLeft = isLeft;

                if (GameObjects == null)
                {
                    string prefabPath = "";

                    if (m_IsLeft)
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
                m_GOTreeState ??= new TreeViewState();

                m_GOTree = new GameObjectTreeView(m_GOTreeState, CompareData.RootInfo, m_IsLeft);

                CompareData.onShowStateChange += OnShowStateChange;
            }

            public void Destroy()
            {
                CompareData.onShowStateChange -= OnShowStateChange;
                m_GOTreeState = null;
                m_GOTree = null;
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
                if (m_GOTreeState.scrollPos != CompareData.GameObjectTreeScroll)
                {
                    m_GOTreeState.scrollPos = CompareData.GameObjectTreeScroll;
                }

                m_GOTree.OnGUI(GUILayoutUtility.GetRect(0, 100000, 0, 100000));

                if (m_GOTreeState.scrollPos != CompareData.GameObjectTreeScroll)
                {
                    CompareData.GameObjectTreeScroll = m_GOTreeState.scrollPos;
                }
            }

            /// <summary>
            /// 展开对应ID的节点
            /// </summary>
            /// <param name="id"></param>
            /// <param name="expanded"></param>
            public void SetExpanded(int id, bool expanded)
            {
                m_GOTree.SetExpanded(id, expanded);
            }

            /// <summary>
            /// 重刷
            /// </summary>
            public void Reload()
            {
                m_GOTree.Reload(CompareData.RootInfo);
            }

            /// <summary>
            /// 显示类型状态改变
            /// </summary>
            private void OnShowStateChange()
            {
                m_GOTree.Reload(CompareData.RootInfo);
            }
        }
    }
}
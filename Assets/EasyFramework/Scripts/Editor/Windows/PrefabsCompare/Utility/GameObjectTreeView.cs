﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace EasyFramework.Windows
{
    namespace PrefabsCompare
    {
        public class GameObjectTreeView : TreeView
        {
            /// <summary>
            /// 选中的ID列表
            /// </summary>
            private readonly List<int> m_SelectIDs = new List<int>();

            /// <summary>
            /// GameObject对比信息
            /// </summary>
            private GameObjectCompareInfo m_Info;

            /// <summary>
            /// 左边还是右边
            /// </summary>
            private readonly bool m_IsLeft;

            /// <summary>
            /// 展开回调
            /// </summary>
            public Action<int, bool, bool> onExpandedStateChanged;

            /// <summary>
            /// 单击回调
            /// </summary>
            public Action<GameObjectCompareInfo> onClickItem;

            /// <summary>
            /// 保存展开的信息
            /// </summary>
            private HashSet<int> m_ExpandedSet = new HashSet<int>();

            /// <summary>
            /// 树的根节点
            /// </summary>
            private TreeViewItem m_Root;

            public GameObjectTreeView(TreeViewState state, GameObjectCompareInfo info, bool isLeft) : base(state)
            {
                m_Info = info;
                m_IsLeft = isLeft;

                Reload();

                ExpandAll();

                m_ExpandedSet = new HashSet<int>(GetExpanded());
            }

            protected override TreeViewItem BuildRoot()
            {
                m_Root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };

                var allItems = new List<TreeViewItem>();

                if (m_Info != null)
                {
                    var item = new CompareTreeViewItem<GameObjectCompareInfo> { Info = m_Info, id = m_Info.ID, depth = m_Info.Depth, displayName = m_Info.Name };
                    allItems.Add(item);

                    AddChildItem(allItems, m_Info);
                }

                SetupParentsAndChildrenFromDepths(m_Root, allItems);

                return m_Root;
            }

            public void Reload(GameObjectCompareInfo info)
            {
                m_Info = info;

                Reload();

                ExpandAll();

                m_ExpandedSet = new HashSet<int>(GetExpanded());
            }

            public override void OnGUI(Rect rect)
            {
                if (CompareData.SelectedGameObjectID != -1)
                {
                    var ids = this.GetSelection();

                    if (ids.Count == 0 || ids[0] != CompareData.SelectedGameObjectID)
                    {
                        m_SelectIDs.Clear();
                        m_SelectIDs.Add(CompareData.SelectedGameObjectID);
                        this.SetSelection(m_SelectIDs);
                        m_SelectIDs.Clear();
                    }
                }

                base.OnGUI(rect);
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                var item = args.item as CompareTreeViewItem<GameObjectCompareInfo>;

                var info = item.Info;

                Rect rect = args.rowRect;

                var interval = 2;

                Rect stateIconRect = new Rect(rect.x + GetContentIndent(item), rect.y, rect.height, rect.height);

                Rect goIconRect = new Rect(stateIconRect.x + stateIconRect.width + interval, stateIconRect.y, stateIconRect.width, stateIconRect.height);

                if (info.MissType == MissType.allExist && !info.AllEqual())
                {
                    GUI.DrawTexture(stateIconRect, PrefabsCompareStyle.failImg, ScaleMode.ScaleToFit);
                }
                else if (info.MissType == MissType.missRight && m_IsLeft)
                {
                    GUI.DrawTexture(stateIconRect, PrefabsCompareStyle.inconclusiveImg, ScaleMode.ScaleToFit);
                }
                else if (info.MissType == MissType.missLeft && !m_IsLeft)
                {
                    GUI.DrawTexture(stateIconRect, PrefabsCompareStyle.inconclusiveImg, ScaleMode.ScaleToFit);
                }
                else if (!string.IsNullOrWhiteSpace(item.displayName))
                {
                    GUI.DrawTexture(stateIconRect, PrefabsCompareStyle.successImg, ScaleMode.ScaleToFit);
                }

                if (m_IsLeft)
                {
                    if (info.MissType != MissType.missLeft && info.LeftGameObject != null)
                    {
                        Texture2D gameObjectIcon = PrefabUtility.GetIconForGameObject(info.LeftGameObject);

                        GUI.DrawTexture(goIconRect, gameObjectIcon, ScaleMode.ScaleToFit);
                    }
                }
                else
                {
                    if (info.MissType != MissType.missRight && info.LeftGameObject != null)
                    {
                        Texture2D gameObjectIcon = PrefabUtility.GetIconForGameObject(info.RightGameObject);

                        GUI.DrawTexture(goIconRect, gameObjectIcon, ScaleMode.ScaleToFit);
                    }
                }

                rect.width -= stateIconRect.width + goIconRect.width + interval;
                rect.x += stateIconRect.width + goIconRect.width + interval;
                args.rowRect = rect;

                base.RowGUI(args);
            }

            protected override void SingleClickedItem(int id)
            {
                base.SingleClickedItem(id);
                CompareData.SelectedGameObjectID = id;

                if (onClickItem != null)
                {
                    var item = FindItem(id, m_Root) as CompareTreeViewItem<GameObjectCompareInfo>;

                    onClickItem.Invoke(item.Info);
                }
            }

            protected override void ExpandedStateChanged()
            {
                base.ExpandedStateChanged();

                var list = GetExpanded();

                //TODO: 优化堆内存
                var tempSet = new HashSet<int>();

                var removeList = new List<int>();

                for (int i = 0; i < list.Count; i++)
                {
                    int id = list[i];

                    tempSet.Add(id);

                    if (!m_ExpandedSet.Contains(id))
                    {
                        m_ExpandedSet.Add(id);
                        onExpandedStateChanged?.Invoke(id, m_IsLeft, true);
                    }
                }

                foreach (var id in m_ExpandedSet)
                {
                    if (!tempSet.Contains(id))
                    {
                        removeList.Add(id);
                        onExpandedStateChanged?.Invoke(id, m_IsLeft, false);
                    }
                }

                for (int i = 0; i < removeList.Count; i++)
                    m_ExpandedSet.Remove(removeList[i]);
            }

            private void AddChildItem(List<TreeViewItem> items, GameObjectCompareInfo info)
            {
                if (info.Children == null)
                {
                    return;
                }

                for (int i = 0; i < info.Children.Count; i++)
                {
                    var child = info.Children[i];

                    if (child == null)
                    {
                        continue;
                    }

                    if (!CompareData.ShowMiss && child.MissType != MissType.allExist)
                    {
                        continue;
                    }

                    if (!CompareData.ShowEqual && child.AllEqual())
                    {
                        continue;
                    }

                    string displayName;

                    if (child.MissType == MissType.missLeft && m_IsLeft)
                    {
                        displayName = "";
                    }
                    else if (child.MissType == MissType.missRight && !m_IsLeft)
                    {
                        displayName = "";
                    }
                    else
                    {
                        displayName = child.Name;
                    }

                    var item = new CompareTreeViewItem<GameObjectCompareInfo> { Info = child, id = child.ID, depth = child.Depth, displayName = displayName };

                    items.Add(item);

                    AddChildItem(items, child);
                }
            }
        }
    }
}

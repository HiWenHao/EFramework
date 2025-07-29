/*
 * ================================================
 * Describe:      This script is used to show the resource detection overview. Let's thank LiangZG!!!!!
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-06-06 15:28:55
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-06-06 15:28:55
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.Edit;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Windows.AssetChecker
{
    /// <summary>
    /// 面板基类
    /// </summary>
    internal abstract class OverviewPanelBase
    {
        /// <summary>
        /// 需要绘制列表的长度
        /// </summary>
        protected int ListCount;

        /// <summary>
        /// 筛选器当前索引
        /// </summary>
        protected int FiltrateIndex;

        /// <summary>
        /// 标签
        /// </summary>
        protected string[] Tabs = new string[] { LC.Combine(Lc.Overview), LC.Combine(Lc.Settings), };

        /// <summary>
        /// 筛选器
        /// </summary>
        protected string[] Filtrates = new string[] { "ALL" };

        /// <summary>
        /// 信息列表
        /// </summary>
        protected string[] ObjectInfoList;

        int _tabsCount;
        int _oldTabIndex;
        int _curTabIndex;
        int _oldFiltrateIndex;
        int _objectInfoListCount;

        private Vector2 _scrollPos;

        /// <summary>
        /// 初始化
        /// </summary>
        internal virtual void Initialize()
        {
            _tabsCount = Tabs.Length;
            _objectInfoListCount = ObjectInfoList.Length;
        }

        /// <summary>
        /// 绘制面板
        /// </summary>
        internal virtual void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(Screen.width * (0.5f / _tabsCount));
            for (int i = 0; i < _tabsCount; i++)
            {
                string style;
                if (i == 0) style = "ButtonLeft";
                else if (i == _tabsCount - 1) style = "ButtonRight";
                else style = "ButtonMid";

                if (GUILayout.Toggle(_curTabIndex == i, Tabs[i], style))
                {
                    _curTabIndex = i;
                }
            }
            GUILayout.Space(Screen.width * (0.5f / _tabsCount));
            GUILayout.EndHorizontal();

            if (_curTabIndex != _oldTabIndex)
            {
                _oldTabIndex = _curTabIndex;
                if (_curTabIndex == 0)
                {
                    FiltrateChanged(FiltrateIndex, true);
                }
            }
            if (_curTabIndex != 0)
            {
                RuleViewOnGUI();
                return;
            }

            EditorGUILayout.Separator();
            GUILayout.Label(LC.Combine(new Lc[] { Lc.File, Lc.Details }));
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label($"{LC.Combine(Lc.Filtrate)}: ");
                FiltrateIndex = EditorGUILayout.Popup(FiltrateIndex, Filtrates);
                if (_oldFiltrateIndex != FiltrateIndex)
                {
                    _oldFiltrateIndex = FiltrateIndex;
                    FiltrateChanged(FiltrateIndex, false);
                }

                if (0 != ListCount)
                    GUILayout.Label($"{LC.Combine(Lc.Count)}: {ListCount}");

                GUILayout.FlexibleSpace();

                BeforeTheRefreshButton();

                if (GUILayout.Button(LC.Combine(Lc.Refresh), GUILayout.Width(80)))
                {
                    _scrollPos = Vector2.zero;
                    Refresh();
                }
                GUILayout.Space(8.5f);
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            //Info Tabs
            GUILayout.BeginHorizontal(GUILayout.MinHeight(20f));
            if (_objectInfoListCount > 2)
            {
                if (GUILayout.Toggle(false, ObjectInfoList[0], "ButtonLeft", GUILayout.Width(150f)))
                {
                    OnClickInfoList(0);
                }
                for (int i = 1; i < _objectInfoListCount - 1; i++)
                {
                    if (GUILayout.Toggle(false, ObjectInfoList[i], "ButtonMid", GUILayout.Width((Screen.width - 150f) / (_objectInfoListCount - 1))))
                    {
                        OnClickInfoList(i);
                    }
                }
                if (GUILayout.Toggle(false, ObjectInfoList[^1], "ButtonRight", GUILayout.Width((Screen.width - 150f) / (_objectInfoListCount - 1))))
                {
                    OnClickInfoList(_objectInfoListCount - 1);
                }
            }
            //GUILayout.Space(10f);
            GUILayout.EndHorizontal();

            if (ListCount != 0)
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);
                int index = 0;
                for (int i = 0; i < ListCount; i++)
                {
                    GUI.backgroundColor = index % 2 == 1 ? Color.white : new Color(0.8f, 0.8f, 0.8f);
                    GUILayout.BeginHorizontal("TextArea", GUILayout.MinHeight(23f));
                    GUI.backgroundColor = Color.white;

                    DrawOne(index);

                    GUILayout.EndHorizontal();
                    index++;
                }
                GUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// 销毁
        /// </summary>
        internal virtual void OnDestroy()
        {
            if (Tabs != null)
                Tabs = null;
            if (ObjectInfoList != null)
                ObjectInfoList = null;
            if (Filtrates != null)
                Filtrates = null;
        }

        /// <summary>
        /// 设置GUI颜色
        /// </summary>
        protected void SetGUIColor(float score)
        {
            GUI.color = AssetsCheckerConfig.ScoreColors[AssetsCheckerConfig.CalScoreLevel(score)];
        }

        /// <summary>
        /// Draw content before the refresh button, you can draw very little content
        /// <para>在刷新按钮之前的绘制内容，可以绘制极少的内容</para>
        /// </summary>
        protected virtual void BeforeTheRefreshButton() { }

        /// <summary>
        /// 刷新
        /// </summary>
        protected abstract void Refresh();

        /// <summary>
        /// Switch filter category
        /// <para>切换筛选类别</para>
        /// </summary>
        /// <param name="index">被点击的筛选索引</param>
        /// <param name="fflush">是否强制刷新</param>
        protected virtual void FiltrateChanged(int index, bool fflush) { }

        /// <summary>
        /// Switch labels sorted by specific information
        /// <para>按照具体信息排序</para>
        /// </summary>
        protected virtual void OnClickInfoList(int index) { }

        /// <summary>
        /// Draws individual file information
        /// <para>绘制单个文件信息</para>
        /// </summary>
        protected virtual void DrawOne(int index) { }

        /// <summary>
        /// Draws the rule setting panel
        /// <para>绘制规则设置面板</para>
        /// </summary>
        protected virtual void RuleViewOnGUI() { }
    }
}

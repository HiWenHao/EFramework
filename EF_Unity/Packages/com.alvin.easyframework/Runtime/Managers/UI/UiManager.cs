/*
 * ================================================
 * Describe:      承载UI管理器的数据收集 .
 * Author:        Alvin8412
 * CreationTime:  2026-04-03 22:11:13
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-03 22:11:13
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;
using EasyFramework.Manager.UI.Tips;
using EasyFramework.UI.Popup;
using UnityEngine;

namespace EasyFramework.Manager.UI
{
    /// <summary>
    /// 用户界面管理器
    /// </summary>
    public partial class UiManager : Singleton<UiManager>, IManager, IUpdate
    {
        /// <summary>
        /// UI相机
        /// </summary>
        public Camera UICamera { get; private set; }

        private uint _serialId; //	页面序列号
        private IUiView _tipsView; //	通用提示窗
        private Transform _target; //	UI根节点
        private IUiView _currentPageView; //	当前页面视窗

        #region Popup

        private const int PopupViewMax = 5; //	弹窗最大数量
        private int _popupIndex; //	弹窗当前被使用的索引
        private GameObject _popupGameObject; //	弹窗预制件
        private List<IUiView> _popupViewsList; //	弹窗列表

        #endregion

        private Dictionary<uint, IUiView> _allUsedViewsDict; //	正在被使用的全部UI视窗
        private Dictionary<string, IUiView> _allCachedViewsDict; //  已经关闭，但还在缓存中的UI视窗
        private Dictionary<UIViewType, Transform> _viewParentDic; //	不同视窗类型的父节点存储字典
    }
}
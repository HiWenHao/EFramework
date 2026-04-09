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
using EasyFramework.Manager.UI;
using UnityEngine;

namespace EasyFramework.Manager
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
        
        private Transform _target; //	UI根节点
        
        private IUiView _tipsView; //	通用提示窗
        private IUiView _currentPageView; //    当前页面视窗

        private int _popupIndex; //	弹窗当前被使用的索引
        private const int PopupViewMax = 5; //	弹窗最大数量
        
        /// <summary> 自动销毁计时 </summary>
        private Dictionary<IUiView, float> _autoDestroyDic;
        
        /// <summary> 不同视窗类型的父节点存储字典 </summary>
        private Dictionary<UIViewType, Transform> _viewParentDic;
        
        /// <summary> 内存中存在的全部UI视窗 </summary>
        private Dictionary<UIViewType, List<IUiView>> _viewStackDic;
        
    }
}
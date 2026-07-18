/*
 * ================================================
 * Describe:      承载UI系统的数据收集 .
 * Author:        Alvin8412
 * CreationTime:  2026-04-03 22:11:13
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-03 22:11:13
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;
using EasyFramework.Edit;
using EasyFramework.Managers.Assets;
using UnityEngine;

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// 用户界面系统
    /// </summary>
    [Manager(Order = 99400)]
    [Dependency(typeof(AssetsManager))]
    public partial class UiSystem : MonoSingleton<UiSystem>, ISingleton, IUpdate
    {
        /// <summary>
        /// UI相机
        /// </summary>
        public Camera UICamera { get; private set; }

        public bool IsPaused { get; private set; }

        private uint _serialId; // 页面序列号

        /// <summary> 自动销毁计时 </summary>
        private Dictionary<IUiView, float> _autoDestroyDic;

        /// <summary> 不同视窗类型的父节点存储字典 </summary>
        private Dictionary<UIViewType, Transform> _viewParentDic;

        /// <summary> 内存中存在的全部UI视窗 </summary>
        private Dictionary<UIViewType, List<IUiView>> _viewStackDic;

        /// <summary>
        /// 视窗动画目录
        /// </summary>
        [SerializeField, HeaderPro("UI视窗开关动画", "UI view switching animation")]
        private UiAnimationConfig animationConfig;
    }
}
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

using System;
using System.Collections.Generic;
using EasyFramework.Managers.Assets;
using UnityEngine;

namespace EasyFramework.Ui
{
    /// <summary>
    /// 用户界面系统
    /// </summary>
    [Manager(Order = 99400)]
    [Dependency(typeof(AssetsManager))]
    public partial class UiManager : MonoSingleton<UiManager>, ISingleton, IUpdate
    {
        /// <summary>
        /// UI相机
        /// </summary>
        public Camera UICamera { get; private set; }

        public bool IsPaused { get; private set; }

        /// <summary>
        /// 是否开启调试日志
        /// <para>Whether to enable debug logs</para>
        /// </summary>
        public bool OpenDebug { get; set; } = true;

        /// <summary>
        /// 是否启用安全区适配（开启后 UIRoot 自动避开刘海与手势条）
        /// <para>Whether to fit UIRoot into the device safe area</para>
        /// </summary>
        private bool _enableSafeArea = true; //  是否启用安全区适配（代码属性 EnableSafeArea 可运行时切换）

        /// <summary>
        /// 是否启用安全区适配；运行期修改会立即生效
        /// <para>Whether safe area fitting is enabled; changes take effect immediately at runtime</para>
        /// </summary>
        public bool EnableSafeArea
        {
            get => _enableSafeArea;
            set
            {
                if (_enableSafeArea == value)
                    return;

                _enableSafeArea = value;
                ApplySafeArea();
            }
        }

        private uint _serialId; // 页面序列号

        /// <summary> 自动销毁计时 </summary>
        private Dictionary<IUiView, float> _autoDestroyDic;

        /// <summary> 不同视窗类型的父节点存储字典 </summary>
        private Dictionary<UIViewType, Transform> _viewParentDic;

        /// <summary> 内存中存在的全部UI视窗 </summary>
        private Dictionary<UIViewType, List<IUiView>> _viewStackDic;

        /// <summary> 父 View → 其全部子面板（含按类型索引） </summary>
        private Dictionary<IUiView, ChildViewCollection> _childViewDic;

        /// <summary> 子 View 类型 → 可复用实例缓存 </summary>
        private Dictionary<Type, List<IUiView>> _childCacheDic;

        /// <summary> 子 View → 挂载它的容器 Transform </summary>
        private Dictionary<IUiView, RectTransform> _childContainerDic;

        /// <summary> 子 View → 其父 View（反向引用，支持子面板自管） </summary>
        private Dictionary<IUiView, IUiView> _childParentDic;

        /// <summary>
        /// 视窗动画目录
        /// </summary>
        [SerializeField, HeaderPro("UI视窗开关动画", "UI view switching animation")]
        private UiAnimationConfig animationConfig;

        /// <summary>
        /// 单个父面板下的子面板集合：同时维护保序列表与按类型索引
        /// <para>Child views under one parent: keeps an ordered list and a by-type index.</para>
        /// </summary>
        private sealed class ChildViewCollection
        {
            // 全部子面板（按添加顺序）
            public readonly List<IUiView> All = new List<IUiView>();
            // 按类型分组索引
            public readonly Dictionary<Type, List<IUiView>> ByType = new Dictionary<Type, List<IUiView>>();

            // 添加一个子面板
            public void Add(IUiView child)
            {
                All.Add(child);
                Type type = child.GetType();
                if (!ByType.TryGetValue(type, out var list))
                    ByType[type] = list = new List<IUiView>();
                list.Add(child);
            }

            // 移除一个子面板
            public void Remove(IUiView child)
            {
                All.Remove(child);
                Type type = child.GetType();
                if (!ByType.TryGetValue(type, out var list)) return;
                list.Remove(child);
                if (list.Count == 0)
                    ByType.Remove(type);
            }
        }
    }
}
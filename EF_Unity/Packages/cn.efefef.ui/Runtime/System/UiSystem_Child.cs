/*
 * ================================================
 * Describe:         UI子面板相关公开方法（AddChild / 查询 / 删除 / 刷新）
 *                   子面板复用 UiBinding 强类型绑定，生命周期由父面板托管。
 * Author:          Alvin8412
 * CreationTime:    2026-11-20
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// UI 系统子面板扩展：动态创建/查询/移除子面板，复用 UiBinding 强类型绑定
    /// <para>Child-view extension: create/query/remove child views reusing UiBinding strong-typed binding.</para>
    /// </summary>
    public partial class UiSystem
    {
        // 父面板下默认的子面板容器节点名
        private const string ChildViewsNodeName = "ChildViews";

        #region AddChild - 添加子面板

        /// <summary>
        /// 在父面板下添加子面板（无参）
        /// <para>Add a child view under the parent (no arguments).</para>
        /// </summary>
        public UniTask<TChild> AddChild<TParent, TChild>()
            where TParent : IUiView
            where TChild : IUiChildView, new()
        {
            return AddChildCore<TParent, TChild>(null);
        }

        /// <summary>
        /// 在父面板下添加子面板，并传递参数
        /// <para>Add a child view under the parent with one argument.</para>
        /// </summary>
        public UniTask<TChild> AddChild<TParent, TChild, TArgs1>(TArgs1 a1)
            where TParent : IUiView
            where TChild : IUiChildView, IUiEnable<TArgs1>, new()
        {
            return AddChildCore<TParent, TChild>(view => view.Enable(a1));
        }

        /// <inheritdoc cref="AddChild{TParent, TChild, TArgs1}(TArgs1)"/>
        public UniTask<TChild> AddChild<TParent, TChild, TArgs1, TArgs2>(TArgs1 a1, TArgs2 a2)
            where TParent : IUiView
            where TChild : IUiChildView, IUiEnable<TArgs1, TArgs2>, new()
        {
            return AddChildCore<TParent, TChild>(view => view.Enable(a1, a2));
        }

        /// <inheritdoc cref="AddChild{TParent, TChild, TArgs1}(TArgs1)"/>
        public UniTask<TChild> AddChild<TParent, TChild, TArgs1, TArgs2, TArgs3>(TArgs1 a1, TArgs2 a2, TArgs3 a3)
            where TParent : IUiView
            where TChild : IUiChildView, IUiEnable<TArgs1, TArgs2, TArgs3>, new()
        {
            return AddChildCore<TParent, TChild>(view => view.Enable(a1, a2, a3));
        }

        /// <inheritdoc cref="AddChild{TParent, TChild, TArgs1}(TArgs1)"/>
        public UniTask<TChild> AddChild<TParent, TChild, TArgs1, TArgs2, TArgs3, TArgs4>(TArgs1 a1, TArgs2 a2,
            TArgs3 a3,
            TArgs4 a4)
            where TParent : IUiView
            where TChild : IUiChildView, IUiEnable<TArgs1, TArgs2, TArgs3, TArgs4>, new()
        {
            return AddChildCore<TParent, TChild>(view => view.Enable(a1, a2, a3, a4));
        }

        /// <inheritdoc cref="AddChild{TParent, TChild, TArgs1}(TArgs1)"/>
        public UniTask<TChild> AddChild<TParent, TChild, TArgs1, TArgs2, TArgs3, TArgs4, TArgs5>(TArgs1 a1, TArgs2 a2,
            TArgs3 a3, TArgs4 a4, TArgs5 a5)
            where TParent : IUiView
            where TChild : IUiChildView, IUiEnable<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5>, new()
        {
            return AddChildCore<TParent, TChild>(view => view.Enable(a1, a2, a3, a4, a5));
        }

        /// <summary>
        /// 添加子面板核心流程：定位父级与容器 → 复用或创建 → 挂载登记 → 启用
        /// <para>Locate parent/container → reuse or create → attach/register → enable.</para>
        /// </summary>
        private async UniTask<TChild> AddChildCore<TParent, TChild>(Action<TChild> enableAction)
            where TParent : IUiView
            where TChild : IUiChildView, new()
        {
            TParent parent = GetView<TParent>();
            if (parent == null || parent.View == null)
            {
                D.Error(
                    $"[ UiSystem ] AddChild<{typeof(TParent).Name}, {typeof(TChild).Name}> failed: no active parent view.");
                return default;
            }

            RectTransform container = FindChildContainer(parent.View);
            IUiView childView = RentChildView<TChild>();
            if (childView == null)
            {
                D.Error(
                    $"[ UiSystem ] AddChild<{typeof(TParent).Name}, {typeof(TChild).Name}> failed: could not create child view.");
                return default;
            }

            childView.View.SetParent(container, false);
            StretchToParent(childView.View);

            if (!_childViewDic.TryGetValue(parent, out var collection))
                _childViewDic[parent] = collection = new ChildViewCollection();
            collection.Add(childView);
            _childContainerDic[childView] = container;
            _childParentDic[childView] = parent;

            var target = (TChild)childView;
            await ViewEnableWithAnim(childView, () => enableAction?.Invoke(target), true);
            return target;
        }

        /// <summary>
        /// 查找父面板下的 ChildViews 容器，缺失则回退到父根节点并警告
        /// <para>Find the ChildViews container; fall back to the parent root with a warning if missing.</para>
        /// </summary>
        private RectTransform FindChildContainer(RectTransform parentView)
        {
            Transform node = parentView.Find(ChildViewsNodeName);
            if (node is RectTransform rect)
                return rect;
            D.Warning(
                $"[ UiSystem ] ChildViews node not found under [{parentView.name}]; child views will attach to the root.");
            return parentView;
        }

        /// <summary>
        /// 将 RectTransform 拉伸铺满父容器
        /// <para>Stretch the RectTransform to fill its parent.</para>
        /// </summary>
        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 复用缓存子面板或新建一个
        /// <para>Reuse a cached child view or create a new one.</para>
        /// </summary>
        private IUiView RentChildView<TChild>() where TChild : IUiChildView, new()
        {
            Type type = typeof(TChild);
            if (_childCacheDic.TryGetValue(type, out var cache) && cache.Count > 0)
            {
                IUiView cached = cache[^1];
                cache.RemoveAt(cache.Count - 1);
                if (cache.Count == 0)
                    _childCacheDic.Remove(type);
                return cached;
            }

            return ViewCreate<TChild>();
        }

        #endregion

        #region Query - 查询子面板

        /// <summary> 按类型取父面板下第一个子面板 </summary>
        /// <para>Get the first child of a given type under the parent.</para>
        public TChild GetChild<TParent, TChild>()
            where TParent : IUiView
            where TChild : IUiChildView
        {
            return GetChild<TParent, TChild>(0);
        }

        /// <summary> 按类型与索引取父面板下的子面板 </summary>
        /// <para>Get a child of a given type by index under the parent.</para>
        public TChild GetChild<TParent, TChild>(int index)
            where TParent : IUiView
            where TChild : IUiChildView
        {
            TParent parent = GetView<TParent>();
            if (parent == null || !_childViewDic.TryGetValue(parent, out var collection))
                return default;
            if (!collection.ByType.TryGetValue(typeof(TChild), out var list))
                return default;
            if (index < 0 || index >= list.Count)
                return default;
            return (TChild)list[index];
        }

        /// <summary> 按类型取父面板下全部子面板（返回副本） </summary>
        /// <para>Get all children of a given type (returns a copy).</para>
        public List<TChild> GetChildren<TParent, TChild>()
            where TParent : IUiView
            where TChild : IUiChildView
        {
            TParent parent = GetView<TParent>();
            var result = new List<TChild>();
            if (parent == null || !_childViewDic.TryGetValue(parent, out var collection) ||
                !collection.ByType.TryGetValue(typeof(TChild), out var list))
                return result;
            for (int i = 0; i < list.Count; i++)
            {
                result.Add((TChild)list[i]);
            }

            return result;
        }

        /// <summary> 取父面板下全部子面板（按添加顺序，返回副本） </summary>
        /// <para>Get all children under the parent in add order (returns a copy).</para>
        public List<IUiView> GetAllChildren<TParent>() where TParent : IUiView
        {
            TParent parent = GetView<TParent>();
            if (parent == null || !_childViewDic.TryGetValue(parent, out var collection))
                return new List<IUiView>();
            return new List<IUiView>(collection.All);
        }

        /// <summary> 按类型分组取父面板下全部子面板（返回只读视图） </summary>
        /// <para>Get all children grouped by type (returns a read-only view).</para>
        public IReadOnlyDictionary<Type, List<IUiView>> GetChildrenByType<TParent>() where TParent : IUiView
        {
            TParent parent = GetView<TParent>();
            if (parent == null || !_childViewDic.TryGetValue(parent, out var collection))
                return new Dictionary<Type, List<IUiView>>();
            return collection.ByType;
        }

        /// <summary> 取子面板的父 View </summary>
        /// <para>Get the parent view of a child.</para>
        public TParent GetParentOf<TParent>(IUiView child) where TParent : IUiView
        {
            if (child != null && _childParentDic.TryGetValue(child, out var parent))
                return (TParent)parent;
            return default;
        }

        #endregion

        #region Remove / Refresh - 移除与刷新

        /// <summary> 移除指定子面板（按 AutoDestroy 决定回缓存或销毁） </summary>
        /// <para>Remove a child view (cache or destroy based on AutoDestroy).</para>
        public UniTask<bool> RemoveChild<TParent, TChild>(TChild child)
            where TParent : IUiView
            where TChild : IUiChildView
        {
            TParent parent = GetView<TParent>();
            if (parent == null || child == null ||
                !_childViewDic.TryGetValue(parent, out var collection) ||
                !collection.All.Contains(child))
                return UniTask.FromResult(false);

            CloseChildView(child);
            return UniTask.FromResult(true);
        }

        /// <summary> 移除父面板下某类型的全部子面板 </summary>
        /// <para>Remove all children of a given type under the parent.</para>
        public UniTask RemoveChildren<TParent, TChild>()
            where TParent : IUiView
            where TChild : IUiChildView
        {
            TParent parent = GetView<TParent>();
            if (parent != null && _childViewDic.TryGetValue(parent, out var collection) &&
                collection.ByType.TryGetValue(typeof(TChild), out var list))
            {
                var snapshot = new List<IUiView>(list);
                for (int i = 0; i < snapshot.Count; i++)
                {
                    CloseChildView(snapshot[i]);
                }
            }

            return UniTask.CompletedTask;
        }

        /// <summary> 移除父面板下全部子面板 </summary>
        /// <para>Remove all children under the parent.</para>
        public UniTask RemoveAllChild<TParent>() where TParent : IUiView
        {
            TParent parent = GetView<TParent>();
            if (parent != null && _childViewDic.TryGetValue(parent, out var collection))
            {
                var snapshot = new List<IUiView>(collection.All);
                for (int i = 0; i < snapshot.Count; i++)
                {
                    CloseChildView(snapshot[i]);
                }
            }

            return UniTask.CompletedTask;
        }

        /// <summary> 子面板自行关闭（子面板 Logic 中调用） </summary>
        /// <para>Let a child view close itself (call from child Logic).</para>
        public bool RemoveSelf(IUiView child)
        {
            if (child == null || !_childParentDic.TryGetValue(child, out var parent))
                return false;
            if (!_childViewDic.TryGetValue(parent, out var collection) || !collection.All.Contains(child))
                return false;
            CloseChildView(child);
            return true;
        }

        /// <summary> 主动刷新子面板数据（复用 IUiEnable 契约） </summary>
        /// <para>Refresh child view data (reuses the IUiEnable contract).</para>
        public void RefreshChild<TParent, TChild, TArgs1>(TChild child, TArgs1 a1)
            where TParent : IUiView
            where TChild : IUiChildView, IUiEnable<TArgs1>
        {
            TParent parent = GetView<TParent>();
            if (parent == null || child == null || !_childViewDic.TryGetValue(parent, out var collection))
                return;
            if (!collection.All.Contains(child))
                return;
            child.Enable(a1);
        }

        /// <summary>
        /// 播放退场动画后关闭子面板（供 CloseView(instance) 走子面板路径时调用）
        /// <para>Close a child after playing the exit animation (used by CloseView(instance) for child views).</para>
        /// </summary>
        private async UniTask CloseChildWithAnim(IUiView child)
        {
            if (child == null || child.Binding == null)
                return;
            var type = child.Binding.ViewAnimationType;
            if (type != UiViewAnimationType.None && child.Binding.CloseViewReverseAnimation &&
                animationConfig is not null && child.View is not null && child.View.gameObject.activeSelf)
            {
                animationConfig.GetDurationAndCurve(child.Binding.ViewType, out float duration, out var curve);
                if (duration > 0f)
                    await AnimateClose(child.View, type, duration, curve);
            }
            CloseChildView(child);
        }

        /// <summary>
        /// 关闭子面板：按 AutoDestroy 决定回缓存复用或彻底销毁，并清理登记
        /// <para>Close a child: cache for reuse or destroy per AutoDestroy, then clean up registrations.</para>
        /// </summary>
        private void CloseChildView(IUiView child)
        {
            if (child == null) return;

            if (child.View != null && child.View.gameObject != null && child.View.gameObject.activeSelf)
            {
                if (child is IUiDisable disable)
                    disable.Disable();
                child.View.gameObject.SetActive(false);
            }

            if (_childParentDic.TryGetValue(child, out var parent) &&
                _childViewDic.TryGetValue(parent, out var collection))
                collection.Remove(child);
            _childContainerDic.Remove(child);
            _childParentDic.Remove(child);

            if (!child.Binding.AutoDestroy)
            {
                DestroyChildView(child);
                return;
            }

            // 回缓存：挂到 Cache 容器，等待下次 AddChild 复用
            Type type = child.GetType();
            if (!_childCacheDic.TryGetValue(type, out var cache))
                _childCacheDic[type] = cache = new List<IUiView>();
            cache.Add(child);
            if (child.View != null)
                child.View.SetParent(_viewParentDic[UIViewType.Cache], false);
        }

        #endregion
    }
}
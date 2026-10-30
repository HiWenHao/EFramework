/*
 * ================================================
 * Describe:         UI视窗打开相关公开方法
 * Author:           Alvin8412
 * CreationTime:     2026-07-01 18:35:20
 * ModifyAuthor:     Alvin8412
 * ModifyTime:       2026-10-30
 * ScriptVersion:    0.3
 * ================================================
 */

using System;
using Cysharp.Threading.Tasks;

namespace EasyFramework.Managers.Ui
{
    public partial class UiSystem
    {
        /// <summary>
        /// 打开视窗, 通过<see cref="UIViewType"/>区分展示逻辑
        /// <para>Open the view and distinguish the display logic through <see cref="UIViewType"/>.</para>
        /// </summary>
        public UniTask<TView> OpenView<TView>() where TView : IUiView, new()
        {
            return OpenViewCore<TView>(null);
        }

        /// <summary>
        /// 打开视窗，并传递参数, 通过<see cref="UIViewType"/>区分展示逻辑
        /// <para>Open the view and pass the parameters, and distinguish the display logic through <see cref="UIViewType"/>.</para>
        /// <para>参数刷新(Enable)在视窗启用后、入场动画开始前执行, 面板出现首帧即为最新数据</para>
        /// </summary>
        public UniTask<TView> OpenView<TView, Targs1>(Targs1 args1)
            where TView : IUiView, IUiEnable<Targs1>, new()
        {
            return OpenViewCore<TView>(view => view.Enable(args1));
        }

        /// <inheritdoc cref="OpenView{TView, Targs1}(Targs1)"/>
        public UniTask<TView> OpenView<TView, Targs1, Targs2>(Targs1 args1, Targs2 args2)
            where TView : IUiView, IUiEnable<Targs1, Targs2>, new()
        {
            return OpenViewCore<TView>(view => view.Enable(args1, args2));
        }

        /// <inheritdoc cref="OpenView{TView, Targs1}(Targs1)"/>
        public UniTask<TView> OpenView<TView, Targs1, Targs2, Targs3>(Targs1 args1, Targs2 args2, Targs3 args3)
            where TView : IUiView, IUiEnable<Targs1, Targs2, Targs3>, new()
        {
            return OpenViewCore<TView>(view => view.Enable(args1, args2, args3));
        }

        /// <inheritdoc cref="OpenView{TView, Targs1}(Targs1)"/>
        public UniTask<TView> OpenView<TView, Targs1, Targs2, Targs3, Targs4>(Targs1 args1, Targs2 args2, Targs3 args3,
            Targs4 args4)
            where TView : IUiView, IUiEnable<Targs1, Targs2, Targs3, Targs4>, new()
        {
            return OpenViewCore<TView>(view => view.Enable(args1, args2, args3, args4));
        }

        /// <inheritdoc cref="OpenView{TView, Targs1}(Targs1)"/>
        public UniTask<TView> OpenView<TView, Targs1, Targs2, Targs3, Targs4, Targs5>(Targs1 args1, Targs2 args2,
            Targs3 args3, Targs4 args4, Targs5 args5)
            where TView : IUiView, IUiEnable<Targs1, Targs2, Targs3, Targs4, Targs5>, new()
        {
            return OpenViewCore<TView>(view => view.Enable(args1, args2, args3, args4, args5));
        }

        /// <summary>
        /// 打开已有视窗实例
        /// <para>Open an existing view instance</para>
        /// </summary>
        public async UniTask<bool> OpenView(IUiView uiView)
        {
            if (uiView == null || !IsExclusiveViewType(uiView.Binding.ViewType))
                return false;

            if (InViewList(uiView, UIViewType.Cache))
                _viewStackDic[UIViewType.Cache].Remove(uiView);
            else if (!InViewList(uiView, uiView.Binding.ViewType))
                return false;

            await ViewEnableWithAnim(uiView);
            ViewCloseByTypeExcept(uiView.Binding.ViewType, uiView);
            return true;
        }

        /// <summary>
        /// 视窗叠加显示方法。不关闭同类视窗，支持多实例叠加。
        /// <para>Overlay display method. Does not close similar windows, supports multiple instances.</para>
        /// </summary>
        public UniTask<TView> OpenViewOverlay<TView>() where TView : IUiView, new()
        {
            return OpenViewOverlayCore<TView>(null);
        }

        /// <summary>
        /// 视窗叠加显示方法，并传递参数。不关闭同类视窗，支持多实例叠加。
        /// <para>Overlay display method, and pass the parameters. Does not close similar windows, supports multiple instances.</para>
        /// <para>参数刷新(Enable)在视窗启用后、入场动画开始前执行, 面板出现首帧即为最新数据</para>
        /// </summary>
        public UniTask<TView> OpenViewOverlay<TView, Targs1>(Targs1 args1)
            where TView : IUiView, IUiEnable<Targs1>, new()
        {
            return OpenViewOverlayCore<TView>(view => view.Enable(args1));
        }

        /// <inheritdoc cref="OpenViewOverlay{TView, Targs1}(Targs1)"/>
        public UniTask<TView> OpenViewOverlay<TView, Targs1, Targs2>(Targs1 args1, Targs2 args2)
            where TView : IUiView, IUiEnable<Targs1, Targs2>, new()
        {
            return OpenViewOverlayCore<TView>(view => view.Enable(args1, args2));
        }

        /// <inheritdoc cref="OpenViewOverlay{TView, Targs1}(Targs1)"/>
        public UniTask<TView> OpenViewOverlay<TView, Targs1, Targs2, Targs3>(Targs1 args1, Targs2 args2, Targs3 args3)
            where TView : IUiView, IUiEnable<Targs1, Targs2, Targs3>, new()
        {
            return OpenViewOverlayCore<TView>(view => view.Enable(args1, args2, args3));
        }

        /// <inheritdoc cref="OpenViewOverlay{TView, Targs1}(Targs1)"/>
        public UniTask<TView> OpenViewOverlay<TView, Targs1, Targs2, Targs3, Targs4>(Targs1 args1, Targs2 args2,
            Targs3 args3, Targs4 args4)
            where TView : IUiView, IUiEnable<Targs1, Targs2, Targs3, Targs4>, new()
        {
            return OpenViewOverlayCore<TView>(view => view.Enable(args1, args2, args3, args4));
        }

        /// <inheritdoc cref="OpenViewOverlay{TView, Targs1}(Targs1)"/>
        public UniTask<TView> OpenViewOverlay<TView, Targs1, Targs2, Targs3, Targs4, Targs5>(Targs1 args1, Targs2 args2,
            Targs3 args3, Targs4 args4, Targs5 args5)
            where TView : IUiView, IUiEnable<Targs1, Targs2, Targs3, Targs4, Targs5>, new()
        {
            return OpenViewOverlayCore<TView>(view => view.Enable(args1, args2, args3, args4, args5));
        }

        #region Open Core - 打开流程核心

        /// <summary>
        /// 打开视窗核心流程: 查活跃/恢复缓存/创建 → 启用并播放入场动画 → 排他收尾
        /// <para><paramref name="enableAction"/> 在视窗启用后、入场动画开始前执行(用于传参刷新数据), 保证面板首帧即为最新数据</para>
        /// </summary>
        private async UniTask<TView> OpenViewCore<TView>(Action<TView> enableAction) where TView : IUiView, new()
        {
            IUiView openView;

            if (TryFindActive<TView>(out var activeView, out var foundType) && IsExclusiveViewType(foundType))
            {
                var casted = (TView)activeView;
                await ViewEnableWithAnim(activeView, () => enableAction?.Invoke(casted));
                ViewCloseByTypeExcept(foundType, activeView);
                return (TView)activeView;
            }

            if (!TryRecoverFromCache<TView>(out openView))
                openView = ViewCreate<TView>();

            if (openView == null)
            {
                D.Error($"[ UiSystem ] OpenView<{typeof(TView).Name}> failed: ViewCreate returned null");
                return default;
            }

            var target = (TView)openView;
            await ViewEnableWithAnim(openView, () => enableAction?.Invoke(target));

            if (IsExclusiveViewType(openView.Binding.ViewType))
                ViewCloseByTypeExcept(openView.Binding.ViewType, openView);

            return (TView)openView;
        }

        /// <summary>
        /// 叠加打开视窗核心流程: 恢复缓存/创建 → 启用并播放入场动画
        /// <para><paramref name="enableAction"/> 在视窗启用后、入场动画开始前执行(用于传参刷新数据)</para>
        /// </summary>
        private async UniTask<TView> OpenViewOverlayCore<TView>(Action<TView> enableAction) where TView : IUiView, new()
        {
            if (!TryRecoverFromCache<TView>(out var uiView))
                uiView = ViewCreate<TView>();

            if (uiView == null)
            {
                D.Error($"[ UiSystem ] OpenViewOverlay<{typeof(TView).Name}> failed: ViewCreate returned null");
                return default;
            }

            var target = (TView)uiView;
            await ViewEnableWithAnim(uiView, () => enableAction?.Invoke(target));
            return (TView)uiView;
        }

        #endregion
    }
}

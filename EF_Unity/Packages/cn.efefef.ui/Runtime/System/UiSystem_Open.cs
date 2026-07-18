/*
 * ================================================
 * Describe:         UI视窗打开相关公开方法
 * Author:           Alvin8412
 * CreationTime:     2026-07-01 18:35:20
 * ModifyAuthor:     Alvin8412
 * ModifyTime:       2026-07-02
 * ScriptVersion:    0.2
 * ================================================
 */

using Cysharp.Threading.Tasks;

namespace EasyFramework.Managers.Ui
{
    public partial class UiSystem
    {
        /// <summary>
        /// 打开视窗, 通过<see cref="UIViewType"/>区分展示逻辑
        /// <para>Open the view and distinguish the display logic through <see cref="UIViewType"/>.</para>
        /// </summary>
        public async UniTask<TView> OpenView<TView>() where TView : IUiView, new()
        {
            IUiView openView;

            if (TryFindActive<TView>(out var activeView, out var foundType) && IsExclusiveViewType(foundType))
            {
                await ViewEnableWithAnim(activeView);
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

            await ViewEnableWithAnim(openView);

            if (IsExclusiveViewType(openView.Binding.ViewType))
                ViewCloseByTypeExcept(openView.Binding.ViewType, openView);

            return (TView)openView;
        }

        /// <summary>
        /// 打开视窗，并传递参数, 通过<see cref="UIViewType"/>区分展示逻辑
        /// <para>Open the view and pass the parameters, and distinguish the display logic through <see cref="UIViewType"/>.</para>
        /// </summary>
        /// <returns></returns>
        public async UniTask<TView> OpenView<TView, Targs1>(Targs1 args1)
            where TView : IUiView, IUiEnable<Targs1>, new()
        {
            TView view = await OpenView<TView>();
            view.Enable(args1);
            return view;
        }

        /// <inheritdoc cref="OpenView{TView, Targs1}(Targs1)"/>
        public async UniTask<TView> OpenView<TView, Targs1, Targs2>(Targs1 args1, Targs2 args2)
            where TView : IUiView, IUiEnable<Targs1, Targs2>, new()
        {
            TView view = await OpenView<TView>();
            view.Enable(args1, args2);
            return view;
        }

        /// <inheritdoc cref="OpenView{TView, Targs1}(Targs1)"/>
        public async UniTask<TView> OpenView<TView, Targs1, Targs2, Targs3>(Targs1 args1, Targs2 args2, Targs3 args3)
            where TView : IUiView, IUiEnable<Targs1, Targs2, Targs3>, new()
        {
            TView view = await OpenView<TView>();
            view.Enable(args1, args2, args3);
            return view;
        }

        /// <inheritdoc cref="OpenView{TView, Targs1}(Targs1)"/>
        public async UniTask<TView> OpenView<TView, Targs1, Targs2, Targs3, Targs4>(Targs1 args1, Targs2 args2, Targs3 args3, Targs4 args4)
            where TView : IUiView, IUiEnable<Targs1, Targs2, Targs3, Targs4>, new()
        {
            TView view = await OpenView<TView>();
            view.Enable(args1, args2, args3, args4);
            return view;
        }

        /// <inheritdoc cref="OpenView{TView, Targs1}(Targs1)"/>
        public async UniTask<TView> OpenView<TView, Targs1, Targs2, Targs3, Targs4, Targs5>(Targs1 args1, Targs2 args2, Targs3 args3, Targs4 args4, Targs5 args5)
            where TView : IUiView, IUiEnable<Targs1, Targs2, Targs3, Targs4, Targs5>, new()
        {
            TView view = await OpenView<TView>();
            view.Enable(args1, args2, args3, args4, args5);
            return view;
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
        public async UniTask<TView> OpenViewOverlay<TView>() where TView : IUiView, new()
        {
            if (!TryRecoverFromCache<TView>(out var uiView))
                uiView = ViewCreate<TView>();

            if (uiView == null)
            {
                D.Error($"[UiSystem] OpenViewOverlay<{typeof(TView).Name}> failed: ViewCreate returned null");
                return default;
            }

            await ViewEnableWithAnim(uiView);
            return (TView)uiView;
        }

        /// <summary>
        /// 视窗叠加显示方法，并传递参数。不关闭同类视窗，支持多实例叠加。
        /// <para>Overlay display method, and pass the parameters. Does not close similar windows, supports multiple instances.</para>
        /// </summary>
        public async UniTask<TView> OpenViewOverlay<TView, Targs1>(Targs1 args1)
            where TView : IUiView, IUiEnable<Targs1>, new()
        {
            TView view = await OpenViewOverlay<TView>();
            view.Enable(args1);
            return view;
        }

        /// <inheritdoc cref="OpenViewOverlay{TView, Targs1}(Targs1)"/>
        public async UniTask<TView> OpenViewOverlay<TView, Targs1, Targs2>(Targs1 args1, Targs2 args2)
            where TView : IUiView, IUiEnable<Targs1, Targs2>, new()
        {
            TView view = await OpenViewOverlay<TView>();
            view.Enable(args1, args2);
            return view;
        }

        /// <inheritdoc cref="OpenViewOverlay{TView, Targs1}(Targs1)"/>
        public async UniTask<TView> OpenViewOverlay<TView, Targs1, Targs2, Targs3>(Targs1 args1, Targs2 args2, Targs3 args3)
            where TView : IUiView, IUiEnable<Targs1, Targs2, Targs3>, new()
        {
            TView view = await OpenViewOverlay<TView>();
            view.Enable(args1, args2, args3);
            return view;
        }

        /// <inheritdoc cref="OpenViewOverlay{TView, Targs1}(Targs1)"/>
        public async UniTask<TView> OpenViewOverlay<TView, Targs1, Targs2, Targs3, Targs4>(Targs1 args1, Targs2 args2, Targs3 args3, Targs4 args4)
            where TView : IUiView, IUiEnable<Targs1, Targs2, Targs3, Targs4>, new()
        {
            TView view = await OpenViewOverlay<TView>();
            view.Enable(args1, args2, args3, args4);
            return view;
        }

        /// <inheritdoc cref="OpenViewOverlay{TView, Targs1}(Targs1)"/>
        public async UniTask<TView> OpenViewOverlay<TView, Targs1, Targs2, Targs3, Targs4, Targs5>(Targs1 args1, Targs2 args2, Targs3 args3, Targs4 args4, Targs5 args5)
            where TView : IUiView, IUiEnable<Targs1, Targs2, Targs3, Targs4, Targs5>, new()
        {
            TView view = await OpenViewOverlay<TView>();
            view.Enable(args1, args2, args3, args4, args5);
            return view;
        }
    }
}
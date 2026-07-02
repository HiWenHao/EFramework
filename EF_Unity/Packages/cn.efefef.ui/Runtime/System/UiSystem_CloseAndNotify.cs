/*
 * ================================================
 * Describe:         关闭视窗并向前驱页面传递参数。
 * Author:           Alvin5100
 * CreationTime:     2026-07-02 16:10:00
 * ModifyAuthor:     Alvin5100
 * ModifyTime:       2026-07-02
 * ScriptVersion:    0.7
 * ================================================
 */

using System;
using Cysharp.Threading.Tasks;

namespace EasyFramework.Managers.Ui
{
    public partial class UiSystem
    {
        /// <summary>
        /// 关闭 TCloseView 视窗，当 TCloseView 处于 Page 栈顶时，才会同时向实现对应的 <see cref="IUiEnable"/> 接口的 TEnableView 传递参数。
        /// <para>Close the TCloseView window. Only when TCloseView is at the top of the Page stack will
        /// <br/>parameters be passed simultaneously to the corresponding TEnableView that implements the <see cref="IUiEnable"/> interface.</para>
        /// </summary>
        public UniTask CloseViewAndNotify<TCloseView, TEnableView, TArgs>(TArgs args)
            where TCloseView : IUiView
            where TEnableView : IUiView, IUiEnable<TArgs>
            => CloseAndNotifyCore<TCloseView, TEnableView>(p => ((TEnableView)p).Enable(args));

        /// <inheritdoc cref="CloseViewAndNotify{TCloseView, TEnableView, TArgs}(TArgs)"/>
        public UniTask CloseViewAndNotify<TCloseView, TEnableView, TArgs1, TArgs2>(TArgs1 a1, TArgs2 a2)
            where TCloseView : IUiView
            where TEnableView : IUiView, IUiEnable<TArgs1, TArgs2>
            => CloseAndNotifyCore<TCloseView, TEnableView>(p => ((TEnableView)p).Enable(a1, a2));

        /// <inheritdoc cref="CloseViewAndNotify{TCloseView, TEnableView, TArgs}(TArgs)"/>
        public UniTask CloseViewAndNotify<TCloseView, TEnableView, TArgs1, TArgs2, TArgs3>(TArgs1 a1, TArgs2 a2, TArgs3 a3)
            where TCloseView : IUiView
            where TEnableView : IUiView, IUiEnable<TArgs1, TArgs2, TArgs3>
            => CloseAndNotifyCore<TCloseView, TEnableView>(p => ((TEnableView)p).Enable(a1, a2, a3));

        /// <inheritdoc cref="CloseViewAndNotify{TCloseView, TEnableView, TArgs}(TArgs)"/>
        public UniTask CloseViewAndNotify<TCloseView, TEnableView, TArgs1, TArgs2, TArgs3, TArgs4>(
            TArgs1 a1, TArgs2 a2, TArgs3 a3, TArgs4 a4)
            where TCloseView : IUiView
            where TEnableView : IUiView, IUiEnable<TArgs1, TArgs2, TArgs3, TArgs4>
            => CloseAndNotifyCore<TCloseView, TEnableView>(p => ((TEnableView)p).Enable(a1, a2, a3, a4));

        /// <inheritdoc cref="CloseViewAndNotify{TCloseView, TEnableView, TArgs}(TArgs)"/>
        public UniTask CloseViewAndNotify<TCloseView, TEnableView, TArgs1, TArgs2, TArgs3, TArgs4, TArgs5>(
            TArgs1 a1, TArgs2 a2, TArgs3 a3, TArgs4 a4, TArgs5 a5)
            where TCloseView : IUiView
            where TEnableView : IUiView, IUiEnable<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5>
            => CloseAndNotifyCore<TCloseView, TEnableView>(p => ((TEnableView)p).Enable(a1, a2, a3, a4, a5));

        /// <summary>
        /// 关闭 TCloseView 并对前驱执行 typed 注入的统一流程。
        /// </summary>
        private async UniTask CloseAndNotifyCore<TCloseView, TEnableView>(Action<IUiView> inject)
            where TCloseView : IUiView
            where TEnableView : IUiView
        {
            if (!TryFindActiveView<TCloseView>(out var uiView))
                return;

            if (TryGetPredecessor(uiView, out var predecessor))
            {
                ViewEnable(predecessor);
                if (predecessor is TEnableView)
                    inject(predecessor);
                else
                {
                    string viewName = typeof(TEnableView).Name;
                    D.Error(
                        $"[ UiSystem ] CloseViewAndNotify<{typeof(TCloseView).Name}, {viewName}>: " +
                        $"View [ {viewName} ] is null or empty or has not been implemented the IUiEnable interface.");
                }
            }

            await ViewCloseWithAnim(uiView, false, false);
        }
    }
}
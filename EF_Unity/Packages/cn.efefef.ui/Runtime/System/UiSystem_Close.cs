/*
 * ================================================
 * Describe:         UI视窗关闭相关公开方法
 * Author:           Alvin8412
 * CreationTime:     2026-07-02 13:39:11
 * ModifyAuthor:     Alvin8412
 * ModifyTime:       2026-07-02
 * ScriptVersion:    0.4
 * ================================================
 */

using Cysharp.Threading.Tasks;

namespace EasyFramework.Managers.Ui
{
    public partial class UiSystem
    {
        /// <summary>
        /// 关闭UI视窗
        /// <para>Close the view.</para>
        /// </summary>
        public async UniTask<bool> CloseView<T>()
            where T : IUiView
        {
            if (!TryFindActiveView<T>(out var uiView))
                return false;

            return await CloseView(uiView);
        }

        /// <summary>
        /// 关闭UI视窗，并传递参数
        /// <para>Close the view and pass the parameters.</para>
        /// </summary>
        public async UniTask<bool> CloseView<T, TArgs1>(TArgs1 args1)
            where T : IUiView, IUiDisable<TArgs1>
        {
            if (!TryFindActiveView<T>(out var uiView))
                return false;

            (uiView as IUiDisable<TArgs1>).Disable(args1);
            return await CloseView(uiView);
        }

        /// <inheritdoc cref="CloseView{T, TArgs1}(TArgs1)"/>
        public async UniTask<bool> CloseView<T, TArgs1, TArgs2>(TArgs1 args1, TArgs2 args2)
            where T : IUiView, IUiDisable<TArgs1, TArgs2>
        {
            if (!TryFindActiveView<T>(out var uiView))
                return false;

            (uiView as IUiDisable<TArgs1, TArgs2>).Disable(args1, args2);
            return await CloseView(uiView);
        }

        /// <inheritdoc cref="CloseView{T, TArgs1}(TArgs1)"/>
        public async UniTask<bool> CloseView<T, TArgs1, TArgs2, TArgs3>(TArgs1 args1, TArgs2 args2, TArgs3 args3)
            where T : IUiView, IUiDisable<TArgs1, TArgs2, TArgs3>
        {
            if (!TryFindActiveView<T>(out var uiView))
                return false;

            (uiView as IUiDisable<TArgs1, TArgs2, TArgs3>).Disable(args1, args2, args3);
            return await CloseView(uiView);
        }

        /// <inheritdoc cref="CloseView{T, TArgs1}(TArgs1)"/>
        public async UniTask<bool> CloseView<T, TArgs1, TArgs2, TArgs3, TArgs4>(TArgs1 args1, TArgs2 args2, TArgs3 args3, TArgs4 args4)
            where T : IUiView, IUiDisable<TArgs1, TArgs2, TArgs3, TArgs4>
        {
            if (!TryFindActiveView<T>(out var uiView))
                return false;

            (uiView as IUiDisable<TArgs1, TArgs2, TArgs3, TArgs4>).Disable(args1, args2, args3, args4);
            return await CloseView(uiView);
        }

        /// <inheritdoc cref="CloseView{T, TArgs1}(TArgs1)"/>
        public async UniTask<bool> CloseView<T, TArgs1, TArgs2, TArgs3, TArgs4, TArgs5>(TArgs1 args1, TArgs2 args2, TArgs3 args3, TArgs4 args4, TArgs5 args5)
            where T : IUiView, IUiDisable<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5>
        {
            if (!TryFindActiveView<T>(out var uiView))
                return false;

            (uiView as IUiDisable<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5>).Disable(args1, args2, args3, args4, args5);
            return await CloseView(uiView);
        }

        /// <summary>
        /// 关闭视窗实例
        /// <para>Close the view instance.</para>
        /// </summary>
        /// <param name="uiView">要被关闭的视窗</param>
        public async UniTask<bool> CloseView(IUiView uiView)
        {
            if (null == uiView)
                return false;

            if (TryGetPredecessor(uiView, out var predecessor))
                ViewEnable(predecessor);

            await ViewCloseWithAnim(uiView, false, false);
            return true;
        }

        /// <summary>
        /// 关闭全部视窗
        /// <para>Close all views.</para>
        /// </summary>
        /// <param name="immediateDestroy">立即销毁被关闭的视窗</param>
        public async UniTask CloseAllView(bool immediateDestroy = false)
        {
            foreach (var uiViews in _viewStackDic)
                ViewCloseAllWithType(uiViews.Key, immediateDestroy, false);

            await UniTask.CompletedTask;
        }

        /// <summary>
        /// 某一类型视窗返回到首页
        /// <para>Returns the first view of the specified type.</para>
        /// </summary>
        /// <param name="uiViewType">视窗类型</param>
        public async UniTask BackToFirstViewWithType(UIViewType uiViewType)
        {
            ViewCloseAllWithType(uiViewType, false, true);
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// 在所有活跃视窗栈中按类型查找实例
        /// <para>Searches for an instance by type across all active view stacks.</para>
        /// </summary>
        private bool TryFindActiveView<T>(out IUiView uiView) where T : IUiView
        {
            var allViewTypes = System.Enum.GetValues(typeof(UIViewType));
            for (int i = allViewTypes.Length - 1; i >= 0; i--)
            {
                UIViewType viewType = (UIViewType)allViewTypes.GetValue(i);
                if (viewType == UIViewType.Cache || !InViewList<T>(out uiView, viewType))
                    continue;
                return true;
            }

            uiView = null;
            return false;
        }
    }
}
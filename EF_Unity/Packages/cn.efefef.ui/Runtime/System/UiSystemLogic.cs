/*
 * ================================================
 * Describe:      承载UI系统的逻辑编写.
 * Author:        Alvin8412
 * CreationTime:  2026-04-03 22:11:25
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-28 17:00:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers.Assets;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EasyFramework.Managers.Ui
{
    public partial class UiSystem
    {
        void ISingleton.Init()
        {
            //_target = new GameObject("UI").transform;
            //_target.SetParent(EFRoot.Managers);
            _target = transform;
            UICamera = new GameObject("UICamera").AddComponent<Camera>();
            UICamera.orthographic = true;
            UICamera.orthographicSize = Screen.height / 2.0f;
            UICamera.farClipPlane = 200.0f;
            UICamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
            UICamera.clearFlags = CameraClearFlags.Depth;
            UICamera.transform.SetParent(_target, false);

            UniversalAdditionalCameraData ucd = UICamera.GetUniversalAdditionalCameraData();
            ucd.renderType = CameraRenderType.Overlay;
            if (null != Camera.main)
            {
                UniversalAdditionalCameraData ucdMain = Camera.main.GetUniversalAdditionalCameraData();
                if (null != ucdMain.scriptableRenderer)
                    ucdMain.cameraStack.Add(UICamera);
            }

            GameObject root = new GameObject("UIRoot");
            root.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
            root.GetComponent<Canvas>().worldCamera = UICamera;
            root.layer = 5;
            CanvasScaler cs = root.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(Screen.width, Screen.height);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root.transform.SetParent(_target, false);

            int typeCount = System.Enum.GetValues(typeof(UIViewType)).Length;
            _viewStackDic = new Dictionary<UIViewType, List<IUiView>>();
            _viewParentDic = new Dictionary<UIViewType, Transform>(typeCount);
            for (int i = 0; i < typeCount; i++)
            {
                UIViewType viewType = (UIViewType)i;
                Transform trans = new GameObject($"ViewType{i}-{viewType}").transform;
                trans.SetParent(root.transform);
                trans.gameObject.SetActive(i != 0);
                trans.gameObject.layer = LayerMask.NameToLayer("UI");
                RectTransform rect = trans.gameObject.AddComponent<RectTransform>();
                rect.sizeDelta = Vector2.zero;
                rect.anchorMax = Vector3.one;
                rect.anchorMin = Vector3.zero;
                rect.localPosition = Vector3.zero;

                _viewParentDic.Add(viewType, rect);
                _viewStackDic.Add(viewType, new List<IUiView>());

                if (i == 0) continue;
                Canvas cv = rect.gameObject.AddComponent<Canvas>();
                rect.gameObject.AddComponent<CanvasGroup>();
                cv.overrideSorting = true;
                cv.sortingOrder = i * 1000;
                if (i != typeCount - 1)
                    rect.gameObject.AddComponent<GraphicRaycaster>();
            }

            GameObject eventSystem = UnityEngine.EventSystems.EventSystem.current?.gameObject;
            if (!eventSystem)
            {
                eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            eventSystem.transform.parent = _target;

            _autoDestroyDic = new Dictionary<IUiView, float>();
        }

        void IUpdate.Update(float elapse, float realElapse)
        {
            foreach (var uiViews in _viewStackDic)
            {
                for (int i = uiViews.Value.Count - 1; i >= 0; i--)
                {
                    var uiView = uiViews.Value[i];
                    if (uiView.View == null || uiView.View.gameObject == null)
                    {
                        _viewStackDic[uiViews.Key].RemoveAt(i);
                        _autoDestroyDic.Remove(uiView);
                        continue;
                    }

                    if (uiViews.Key != UIViewType.Cache)
                    {
                        uiView.Update(elapse, realElapse);
                        continue;
                    }

                    if (_autoDestroyDic.TryGetValue(uiView, out float remaining))
                    {
                        remaining -= elapse;
                        _autoDestroyDic[uiView] = remaining;
                        if (remaining > 0.0f)
                            continue;
                        ViewDestroy(uiView, uiViews.Value);
                    }
                    else
                    {
                        ViewDestroy(uiView, uiViews.Value);
                    }
                }
            }
        }

        void ISingleton.Quit()
        {
            CloseAllView(true).Forget();

            _viewStackDic.Clear();
            _viewStackDic = null;
            _viewParentDic.Clear();
            _viewParentDic = null;

            _autoDestroyDic.Clear();
            _autoDestroyDic = null;

            DestroyObj(UICamera.gameObject);
            UICamera = null;

            DestroyObj(_target.gameObject);
            _target = null;
        }

        private static string GetAssetPath(string viewName)
        {
            return AssetsManager.Instance.CurrentSystemType == AssetsSystemType.Default
            ? EFC.Projects.AppConst.UIPrefabsPath + viewName
            : viewName;
        }

        private static void DestroyObj(Object obj)
        {
            Destroy(obj);
        }

        #region Animation - 动画相关

        /// <summary>
        /// 依据父节点计算滑入起始偏移量
        /// </summary>
        /// <param name="type">动画类那个</param>
        /// <param name="rect">对应的UI面板实例对象</param>
        /// <returns>起始偏移量</returns>
        private static Vector2 GetSlideOffset(UiViewAnimationType type, RectTransform rect)
        {
            Vector2 parentSize = ((RectTransform)rect.parent).rect.size;
            return type switch
            {
                UiViewAnimationType.SlideFromLeft => new Vector2(-parentSize.x, 0),
                UiViewAnimationType.SlideFromRight => new Vector2(parentSize.x, 0),
                UiViewAnimationType.SlideFromTop => new Vector2(0, parentSize.y),
                UiViewAnimationType.SlideFromBottom => new Vector2(0, -parentSize.y),
                _ => Vector2.zero,
            };
        }

        /// <summary>
        /// 播放入场动画（从起始位置/缩放 → 归位）
        /// </summary>
        private static async UniTask AnimateOpen(RectTransform rect, UiViewAnimationType type, float duration, AnimationCurve curve)
        {
            if (duration <= 0f) return;

            float elapsed = 0f;
            Vector2 startPos = rect.anchoredPosition;
            Vector3 startScale = rect.localScale;
            Vector2 endPos = Vector2.zero;
            Vector3 endScale = Vector3.one;

            while (elapsed < duration)
            {
                if (rect == null) return;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveValue = curve?.Evaluate(t) ?? t;

                if (type == UiViewAnimationType.Scale)
                    rect.localScale = Vector3.Lerp(startScale, endScale, curveValue);
                else
                    rect.anchoredPosition = Vector2.Lerp(startPos, endPos, curveValue);

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (type == UiViewAnimationType.Scale)
                rect.localScale = Vector3.one;
            else
                rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 播放退场动画（从归位 → 滑出/缩小）
        /// </summary>
        private static async UniTask AnimateClose(RectTransform rect, UiViewAnimationType type, float duration, AnimationCurve curve)
        {
            if (duration <= 0f) return;

            float elapsed = 0f;
            Vector2 startPos = Vector2.zero;
            Vector3 startScale = Vector3.one;
            Vector2 endPos;
            Vector3 endScale;

            if (type == UiViewAnimationType.Scale)
            {
                endPos = Vector2.zero;
                endScale = Vector3.zero;
            }
            else
            {
                endPos = GetSlideOffset(type, rect);
                endScale = Vector3.one;
            }

            while (elapsed < duration)
            {
                if (rect == null) return;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveValue = curve?.Evaluate(t) ?? t;

                if (type == UiViewAnimationType.Scale)
                    rect.localScale = Vector3.Lerp(startScale, endScale, curveValue);
                else
                    rect.anchoredPosition = Vector2.Lerp(startPos, endPos, curveValue);

                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }

        /// <summary>
        /// 启用视窗并播放入场动画
        /// </summary>
        private async UniTask ViewEnableWithAnim(IUiView uiView, params object[] args)
        {
            if (animationConfig != null
                && animationConfig.TryGetPreset(uiView.ViewType, out var type, out var duration, out var curve, out _)
                && type != UiViewAnimationType.None
                && duration > 0f)
            {
                if (type == UiViewAnimationType.Scale)
                    uiView.View.localScale = Vector3.zero;
                else
                    uiView.View.anchoredPosition = GetSlideOffset(type, uiView.View);

                ViewEnable(uiView, args);
                await AnimateOpen(uiView.View, type, duration, curve);
            }
            else
            {
                ViewEnable(uiView, args);
            }
        }

        /// <summary>
        /// 播放退场动画后关闭视窗（仅在 reverseOnClose 为 true 时播放）
        /// </summary>
        private async UniTask ViewCloseWithAnim(IUiView uiView, bool immediateDestroy, bool onlyDisable, params object[] args)
        {
            if (uiView.View != null
                && uiView.View.gameObject.activeSelf
                && animationConfig != null
                && animationConfig.TryGetPreset(uiView.ViewType, out var type, out var duration
                    , out var curve, out var reverseOnClose)
                && reverseOnClose
                && type != UiViewAnimationType.None
                && duration > 0f)
            {
                await AnimateClose(uiView.View, type, duration, curve);
            }

            ViewClose(uiView, immediateDestroy, onlyDisable, args);
        }

        #endregion

        #region View Life Cycle - 视窗生命周期

        private IUiView ViewCreate<T>() where T : IUiView, new()
        {
            IUiView uiView = new T();

            string viewName = uiView.GetType().Name;

            GameObject prefab = AssetsManager.Instance.Load<GameObject>(GetAssetPath(viewName));
            if (!prefab)
            {
                D.Exception($"[ UiSystem ] UI Prefab [ {viewName} ] not found in YooAsset or Resources.");
                return null;
            }

            GameObject uiObj = Object.Instantiate(prefab, _viewParentDic[uiView.ViewType], false);
            RectTransform rect = uiObj.GetComponent<RectTransform>();
            uiObj.name = viewName;
            rect.anchorMax = Vector2.one;
            rect.anchorMin = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localPosition = Vector3.zero;

            uiView.Bind(rect);
            uiView.SerialId = ++_serialId;
            uiView.Awake();
            return uiView;
        }

        private void ViewDestroy(IUiView uiView, List<IUiView> viewList)
        {
            if (uiView == null) return;

            viewList?.Remove(uiView);
            _autoDestroyDic.Remove(uiView);

            if (null == uiView.View || null == uiView.View.gameObject) return;
            uiView.Quit();
            uiView.Dispose();
            DestroyObj(uiView.View.gameObject);

            AssetsManager.Instance.Release(GetAssetPath(uiView.View.name)).Forget();
        }

        private bool ViewEnable(IUiView uiView, params object[] args)
        {
            if (null == uiView)
                return false;

            if (!_viewStackDic[uiView.ViewType].Contains(uiView))
                _viewStackDic[uiView.ViewType].Add(uiView);

            if (!uiView.View.parent.Equals(_viewParentDic[uiView.ViewType]))
                uiView.View.transform.SetParent(_viewParentDic[uiView.ViewType], false);

            _autoDestroyDic.Remove(uiView);

            uiView.View.gameObject.SetActive(true);
            uiView.Enable(args);

            return true;
        }

        private bool ViewClose(IUiView uiView, bool immediateDestroy, bool onlyDisable, params object[] args)
        {
            if (null == uiView || null == uiView.View.gameObject)
                return false;

            if (uiView.View.gameObject.activeSelf)
            {
                uiView.Disable(args);
                uiView.View.gameObject.SetActive(false);
            }

            if (onlyDisable)
                return true;

            if (!uiView.AutoDestroy || immediateDestroy || _viewStackDic[UIViewType.Cache].Contains(uiView))
            {
                ViewDestroy(uiView, _viewStackDic[uiView.ViewType]);
                return true;
            }

            _autoDestroyDic[uiView] = uiView.AutoDestroyCountdown;
            _viewStackDic[UIViewType.Cache].Add(uiView);
            _viewStackDic[uiView.ViewType].Remove(uiView);
            uiView.View.transform.SetParent(_viewParentDic[UIViewType.Cache], false);

            return true;
        }

        #endregion

        #region Internal helper - 内部助手

        /// <summary>
        /// 关闭指定类型栈顶的非 <paramref name="exceptView"/> 视窗, 用于新面板入场动画完成后清理紧邻的旧面板，不影响下方页面栈。
        /// </summary>
        private void ViewCloseByTypeExcept(UIViewType uiViewType, IUiView exceptView, params object[] args)
        {
            List<IUiView> views = _viewStackDic[uiViewType];
            int index = views.Count - 1;
            if (index < 0) return;
            if (views[index] == exceptView) index--;
            if (index < 0) return;
            var closeView = views[index];
            if (closeView == exceptView) return;

            bool needCache = closeView is { ViewType: UIViewType.TopPermanent or UIViewType.BottomPermanent };
            ViewClose(closeView, false, !needCache, args);
        }

        /// <summary>
        /// 关闭某个类型的全部视窗
        /// </summary>
        private void ViewCloseAllWithType(UIViewType uiViewType, bool immediateDestroy, bool keepFirstView, params object[] args)
        {
            var uiViews = _viewStackDic[uiViewType];
            for (int i = uiViews.Count - 1; i >= 0; i--)
            {
                if (i == 0 && keepFirstView)
                {
                    ViewEnable(uiViews[i], args);
                    continue;
                }

                ViewClose(uiViews[i], immediateDestroy, false, args);
            }
        }

        /// <summary>
        /// 判断视窗是否存在列表中
        /// </summary>
        private bool InViewList<T>(out IUiView uiView, UIViewType viewType)
        {
            var targetType = typeof(T);
            foreach (IUiView view in _viewStackDic[viewType])
            {
                if (view.GetType() != targetType)
                    continue;

                uiView = view;
                return true;
            }

            uiView = null;
            return false;
        }

        /// <summary>
        /// 判断视窗是否存在列表中
        /// </summary>
        private bool InViewList(IUiView uiView, UIViewType viewType)
        {
            var targetType = uiView.GetType();
            foreach (IUiView view in _viewStackDic[viewType])
            {
                if (view.GetType() == targetType)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 尝试从 Cache 中恢复同类型视窗，恢复成功则从 Cache 列表移除。
        /// </summary>
        private bool TryRecoverFromCache<T>(out IUiView uiView) where T : IUiView
        {
            if (InViewList<T>(out uiView, UIViewType.Cache))
            {
                _viewStackDic[UIViewType.Cache].Remove(uiView);
                return true;
            }

            uiView = null;
            return false;
        }

        /// <summary>
        /// 遍历所有活跃类型，查找指定类型的视窗
        /// </summary>
        private bool TryFindActive<T>(out IUiView uiView, out UIViewType foundType) where T : IUiView
        {
            foreach (var type in _viewStackDic.Keys)
            {
                if (type == UIViewType.Cache) continue;
                if (!InViewList<T>(out uiView, type)) continue;

                foundType = type;
                return true;
            }

            uiView = null;
            foundType = default;
            return false;
        }

        #endregion

        /// <summary>
        /// 设置UI动画配置
        /// <para>Set UI animation config</para>
        /// </summary>
        /// <param name="config">动画配置</param>
        public void SetUiAnimationConfig(UiAnimationConfig config)
        {
            animationConfig = config;
        }

        /// <summary>
        /// 打开视窗, 通过<see cref="UIViewType"/>区分展示逻辑<para>Open the view, Distinguish the display logic through <see cref="UIViewType"/></para><br/>
        /// - 单例型：关闭相同类型的视图，然后显示新的视图
        /// <para>Singleton type:Close the view of the same type, and then display the new view.</para><br/>
        /// - 多实例型：直接叠加，不关闭已有
        /// <para>Multi-instance: Directly superimpose without closing the existing one</para>
        /// </summary>
        /// <param name="args">该参数将推送给即将打开的视窗</param>
        public async UniTask<T> OpenView<T>(params object[] args) where T : IUiView, new()
        {
            await UniTask.CompletedTask;
            IUiView openView;

            if (TryFindActive<T>(out openView, out var foundType)
                && foundType is UIViewType.Page or UIViewType.BottomPermanent or UIViewType.TopPermanent)
            {
                await ViewEnableWithAnim(openView, args);
                ViewCloseByTypeExcept(foundType, openView, args);
                return (T)openView;
            }

            if (openView == null && !TryRecoverFromCache<T>(out openView))
                openView = ViewCreate<T>();

            if (openView == null)
            {
                D.Error($"[ UiSystem ] OpenView<{typeof(T).Name}> failed: ViewCreate returned null");
                return default;
            }

            await ViewEnableWithAnim(openView, args);

            if (openView.ViewType is UIViewType.Page or UIViewType.BottomPermanent or UIViewType.TopPermanent)
                ViewCloseByTypeExcept(openView.ViewType, openView, args);
            return (T)openView;
        }

        /// <summary>
        /// [已废弃] 请使用 <see cref="OpenView{T}"/> 代替。
        /// <para>[Obsolete] Use <see cref="OpenView{T}"/> instead.</para>
        /// </summary>
        [System.Obsolete("Use OpenView<T> instead.")]
        public async UniTask<T> OpenPageView<T>(params object[] args) where T : IUiView, new()
        {
            return await OpenView<T>(args);
        }

        /// <summary>
        /// 打开页面
        /// </summary>
        /// <param name="uiView">要被打开的页面</param>
        /// <param name="args">This parameter will be sent to both the UI page that is about to be opened and the UI page that has been closed.
        /// <para>该参数将推送给即将打开的UI页面 和 被关闭的UI页面</para></param>
        public async UniTask<bool> OpenView(IUiView uiView, params object[] args)
        {
            await UniTask.CompletedTask;
            if (uiView is not { ViewType: (UIViewType.Page or UIViewType.BottomPermanent or UIViewType.TopPermanent) })
                return false;

            if (InViewList(uiView, UIViewType.Cache))
                _viewStackDic[UIViewType.Cache].Remove(uiView);
            else if (!InViewList(uiView, uiView.ViewType))
                return false;

            await ViewEnableWithAnim(uiView, args);
            ViewCloseByTypeExcept(uiView.ViewType, uiView, args);
            return true;
        }

        /// <summary>
        /// 获取已打开的视窗
        /// </summary>
        /// <typeparam name="T">View type. <para>视窗类型</para></typeparam>
        public T GetView<T>() where T : IUiView
        {
            foreach (var kvp in _viewStackDic)
            {
                if (kvp.Key == UIViewType.Cache) continue;
                if (InViewList<T>(out var uiView, kvp.Key))
                    return (T)uiView;
            }

            return default;
        }

        /// <summary>
        /// 视窗叠加显示方法。不关闭同类视窗，支持多实例叠加。
        /// <para>General window display method. Does not close similar windows, supports multiple instances to be stacked.</para>
        /// </summary>
        /// <param name="args">This parameter will be sent to both the UI page that is about to be opened and the UI page that has been closed.
        /// <para>该参数将推送给即将打开的UI页面 和 被关闭的UI页面</para></param>
        public async UniTask<T> OpenViewOverlay<T>(params object[] args) where T : IUiView, new()
        {
            await UniTask.CompletedTask;

            if (!TryRecoverFromCache<T>(out var uiView))
                uiView = ViewCreate<T>();
            if (uiView == null)
            {
                D.Error($"[ UiSystem ] OpenViewOverlay<{typeof(T).Name}> failed: ViewCreate returned null");
                return default;
            }

            await ViewEnableWithAnim(uiView, args);
            return (T)uiView;
        }

        /// <summary>
        /// 某一类型视窗返回到首页
        /// </summary>
        /// <param name="uiViewType">视窗类型</param>
        /// <param name="args">This parameter will be sent to both the UI page that is about to be opened and the UI page that has been closed.
        /// <para>该参数将推送给即将打开的UI页面 和 被关闭的UI页面</para></param>
        public async UniTask BackToFirstViewWithType(UIViewType uiViewType, params object[] args)
        {
            ViewCloseAllWithType(uiViewType, false, true, args);
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// 关闭UI视窗
        /// </summary>
        /// <param name="args">This parameter will be sent to both the UI page that is about to be opened and the UI page that has been closed.
        /// <para>该参数将推送给即将打开的UI页面 和 被关闭的UI页面</para></param>
        public async UniTask<bool> CloseView<T>(params object[] args) where T : IUiView
        {
            var allViewTypes = System.Enum.GetValues(typeof(UIViewType));
            for (int i = allViewTypes.Length - 1; i >= 0; i--)
            {
                UIViewType viewType = (UIViewType)allViewTypes.GetValue(i);
                if (viewType == UIViewType.Cache || !InViewList<T>(out IUiView uiView, viewType))
                    continue;

                return await CloseView(uiView, args);
            }

            return false;
        }

        /// <summary>
        /// 关闭视窗
        /// </summary>
        /// <param name="uiView">要被关闭的视窗</param>
        /// <param name="args">This parameter will be sent to both the UI page that is about to be opened and the UI page that has been closed.
        /// <para>该参数将推送给即将打开的UI页面 和 被关闭的UI页面</para></param>
        public async UniTask<bool> CloseView(IUiView uiView, params object[] args)
        {
            if (null == uiView)
                return false;

            if (uiView.ViewType == UIViewType.Page)
            {
                var pageStack = _viewStackDic[UIViewType.Page];
                int idx = pageStack.IndexOf(uiView);
                // 仅当被关闭的是栈顶页面时，才唤起其前驱页面
                if (idx == pageStack.Count - 1 && idx > 0)
                    ViewEnable(pageStack[idx - 1], args);
            }

            await ViewCloseWithAnim(uiView, false, false, args);
            return true;
        }

        /// <summary>
        /// 关闭全部视窗
        /// </summary>
        /// <param name="immediateDestroy">立即销毁被关闭的视窗</param>
        /// <param name="args">This parameter will be sent to both the UI page that is about to be opened and the UI page that has been closed.
        /// <para>该参数将推送给即将打开的UI页面 和 被关闭的UI页面</para></param>
        public async UniTask CloseAllView(bool immediateDestroy = false, params object[] args)
        {
            foreach (var uiViews in _viewStackDic)
            {
                ViewCloseAllWithType(uiViews.Key, immediateDestroy, false, args);
            }

            await UniTask.CompletedTask;
        }
    }
}
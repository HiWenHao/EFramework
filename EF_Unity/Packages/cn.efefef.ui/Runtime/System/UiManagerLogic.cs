/*
 * ================================================
 * Describe:         承载UI系统的逻辑编写.
 * Author:           Alvin8412
 * CreationTime:     2026-04-03 22:11:25
 * ModifyAuthor:     Alvin8412
 * ModifyTime:       2026-07-02
 * ScriptVersion:    0.3
 * ===============================================
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers.Assets;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EasyFramework.Ui
{
    public partial class UiManager
    {
        private SafeAreaFitter _safeAreaFitter;  //  SafeArea 容器上的安全区适配器
        private RectTransform _uiRootRect;   //  UIRoot 下 SafeArea 容器的 RectTransform

        void ISingleton.Init()
        {
            UICamera = new GameObject("UICamera").AddComponent<Camera>();
            UICamera.orthographic = true;
            UICamera.orthographicSize = Screen.height / 2.0f;
            UICamera.farClipPlane = 200.0f;
            UICamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
            UICamera.clearFlags = CameraClearFlags.Depth;
            UICamera.transform.SetParent(transform, false);

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
            root.transform.SetParent(transform, false);

            //  安全区容器：作为 UIRoot 的子节点，所有视窗挂在它下面，随安全区收缩
            GameObject safeAreaRoot = new GameObject("SafeArea");
            RectTransform safeRect = safeAreaRoot.AddComponent<RectTransform>();
            safeRect.SetParent(root.transform, false);
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = Vector2.zero;
            safeRect.offsetMax = Vector2.zero;
            _uiRootRect = safeRect;
            ApplySafeArea();

            int typeCount = System.Enum.GetValues(typeof(UIViewType)).Length;
            _viewStackDic = new Dictionary<UIViewType, List<IUiView>>();
            _viewParentDic = new Dictionary<UIViewType, Transform>(typeCount);
            for (int i = 0; i < typeCount; i++)
            {
                UIViewType viewType = (UIViewType)i;
                Transform trans = new GameObject($"ViewType{i}-{viewType}").transform;
                trans.SetParent(safeRect);
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

            eventSystem.transform.parent = transform;

            _autoDestroyDic = new Dictionary<IUiView, float>();
            _childViewDic = new Dictionary<IUiView, ChildViewCollection>();
            _childCacheDic = new Dictionary<Type, List<IUiView>>();
            _childContainerDic = new Dictionary<IUiView, RectTransform>();
            _childParentDic = new Dictionary<IUiView, IUiView>();
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

            // 子面板不进全局栈，单独轮询其 Update
            foreach (var collection in _childViewDic.Values)
            {
                // 快照遍历：child.Update 内部可能移除子面板（DestroyChildView 会改 collection.All），
                // 直接按原集合倒序遍历会索引错位/跳过；与同文件 :157/:398 的做法保持一致
                var children = new List<IUiView>(collection.All);
                for (int i = children.Count - 1; i >= 0; i--)
                {
                    var child = children[i];
                    if (child.View == null || child.View.gameObject == null)
                        continue;
                    child.Update(elapse, realElapse);
                }
            }
        }

        void ISingleton.Quit()
        {
            CloseAllView(true).Forget();

            // 全局 View 销毁时会级联销毁其子面板；此处兜底清理可能残留的游离子面板（父已不在栈中）
            if (_childViewDic != null)
            {
                foreach (var collection in _childViewDic.Values)
                {
                    // 用快照遍历：DestroyChildView 会修改 child 相关字典与 collection.All
                    var children = new List<IUiView>(collection.All);
                    for (int i = children.Count - 1; i >= 0; i--)
                    {
                        DestroyChildView(children[i]);
                    }
                }
            }

            _viewStackDic?.Clear();
            _viewStackDic = null;
            _viewParentDic?.Clear();
            _viewParentDic = null;
            _autoDestroyDic?.Clear();
            _autoDestroyDic = null;

            _childViewDic?.Clear();
            _childViewDic = null;
            _childCacheDic?.Clear();
            _childCacheDic = null;
            _childContainerDic?.Clear();
            _childContainerDic = null;
            _childParentDic?.Clear();
            _childParentDic = null;

            //  先恢复 SafeArea 容器锚点并移除适配器，再销毁相机，避免当帧 Canvas 引用已销毁相机
            if (_safeAreaFitter != null)
            {
                _safeAreaFitter.Restore();
                DestroyObj(_safeAreaFitter);
                _safeAreaFitter = null;
            }
            _uiRootRect = null;

            if (UICamera != null)
            {
                DestroyObj(UICamera.gameObject);
                UICamera = null;
            }
        }

        //  按当前开关挂载或卸载 SafeArea 容器上的安全区适配器
        private void ApplySafeArea()
        {
            if (_uiRootRect == null)
                return;

            if (_enableSafeArea)
            {
                //  AddComponent 会同步触发 Awake/OnEnable，二者已调用 Apply，无需重复
                if (_safeAreaFitter == null)
                    _safeAreaFitter = _uiRootRect.gameObject.AddComponent<SafeAreaFitter>();
                else
                    _safeAreaFitter.enabled = true;
            }
            else if (_safeAreaFitter != null)
            {
                //  OnDisable 内部会 Restore，无需显式再调
                _safeAreaFitter.enabled = false;
            }
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
        private static async UniTask AnimateOpen(RectTransform rect, UiViewAnimationType type, float duration,
            AnimationCurve curve)
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
        private static async UniTask AnimateClose(RectTransform rect, UiViewAnimationType type, float duration,
            AnimationCurve curve)
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
        /// <param name="uiView">目标视窗</param>
        /// <param name="beforeAnim">在视窗启用后、入场动画开始前执行的回调(用于刷新面板数据), 可为null</param>
        private async UniTask ViewEnableWithAnim(IUiView uiView, Action beforeAnim = null, bool isChild = false)
        {
            var type = uiView.Binding.ViewAnimationType;
            if (type == UiViewAnimationType.None || animationConfig is null)
            {
                ViewEnable(uiView, isChild);
                beforeAnim?.Invoke();
                return;
            }

            animationConfig.GetDurationAndCurve(uiView.Binding.ViewType, out var duration, out var curve);
            if (duration > 0f)
            {
                if (type == UiViewAnimationType.Scale)
                    uiView.View.localScale = Vector3.zero;
                else
                    uiView.View.anchoredPosition = GetSlideOffset(type, uiView.View);

                ViewEnable(uiView, isChild);
                beforeAnim?.Invoke();
                await AnimateOpen(uiView.View, type, duration, curve);
            }
            else
            {
                ViewEnable(uiView, isChild);
                beforeAnim?.Invoke();
            }
        }

        /// <summary>
        /// 播放退场动画后关闭视窗（仅在 reverseOnClose 为 true 时播放）
        /// </summary>
        private async UniTask ViewCloseWithAnim(IUiView uiView, bool immediateDestroy, bool onlyDisable)
        {
            var type = uiView.Binding.ViewAnimationType;
            if (type == UiViewAnimationType.None || !uiView.Binding.CloseViewReverseAnimation || animationConfig is null)
            {
                ViewClose(uiView, immediateDestroy, onlyDisable);
                return;
            }

            animationConfig.GetDurationAndCurve(uiView.Binding.ViewType, out float duration, out AnimationCurve curve);
            if (uiView.View is not null && uiView.View.gameObject.activeSelf && duration > 0f)
                await AnimateClose(uiView.View, type, duration, curve);
            ViewClose(uiView, immediateDestroy, onlyDisable);
        }

        #endregion

        #region View Life Cycle - 视窗生命周期

        private IUiView ViewCreate<T>() where T : IUiView, new()
        {
            IUiView uiView = new T();

            string viewName = uiView.GetType().Name;

            AssetHandle<GameObject> prefabHandle = AssetsManager.Instance.Load<GameObject>(GetAssetPath(viewName));
            if (prefabHandle is not { IsValid: true })
            {
                prefabHandle?.Dispose();
                D.Exception($"[ UiManager ] UI Prefab [ {viewName} ] not found in YooAsset or Resources.");
                return null;
            }

            GameObject uiObj = Instantiate(prefabHandle.Asset);
            prefabHandle.Dispose();
            UiBinding prefabBinding = uiObj.GetComponent<UiBinding>();
            if (prefabBinding == null)
            {
                D.Error($"[ UiManager ] UI Prefab [ {viewName} ] not have component < UiBinding >, Please add a UiBinding component.");
                DestroyObj(uiObj);
                return null;
            }

            RectTransform rect = uiObj.GetComponent<RectTransform>();
            rect.SetParent(_viewParentDic[prefabBinding.ViewType]);
            uiObj.name = viewName;
            rect.anchorMax = Vector2.one;
            rect.anchorMin = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localPosition = Vector3.zero;

            uiView.Bind(rect, prefabBinding);
            uiView.SerialId = ++_serialId;
            uiView.Awake();
            return uiView;
        }

        private void ViewDestroy(IUiView uiView, List<IUiView> viewList)
        {
            if (uiView == null) return;

            // 父面板销毁前，级联销毁其名下所有子面板
            if (_childViewDic.TryGetValue(uiView, out var collection))
            {
                var children = new List<IUiView>(collection.All);
                for (int i = 0; i < children.Count; i++)
                    DestroyChildView(children[i]);
                _childViewDic.Remove(uiView);
            }

            viewList?.Remove(uiView);
            _autoDestroyDic.Remove(uiView);

            if (null == uiView.View || null == uiView.View.gameObject) return;
            uiView.Quit();
            uiView.Dispose();
            DestroyObj(uiView.View.gameObject);
        }

        /// <summary>
        /// 彻底销毁一个子面板（不走缓存，用于父级联销毁）
        /// </summary>
        private void DestroyChildView(IUiView child)
        {
            if (child == null) return;
            _childContainerDic.Remove(child);
            _childParentDic.Remove(child);
            RemoveFromChildCache(child);
            if (null == child.View || null == child.View.gameObject) return;
            child.Quit();
            child.Dispose();
            DestroyObj(child.View.gameObject);
        }

        /// <summary>
        /// 从子面板缓存池中移除指定实例
        /// </summary>
        private void RemoveFromChildCache(IUiView child)
        {
            Type type = child.GetType();
            if (!_childCacheDic.TryGetValue(type, out var cache)) return;
            cache.Remove(child);
            if (cache.Count == 0)
                _childCacheDic.Remove(type);
        }

        /// <summary>
        /// 启用视窗：加入栈、设 parent、SetActive、调用 IUiEnable.Enable()（如实现）
        /// <para>子面板模式（isChild=true）跳过入栈与类型容器 SetParent，避免与全局生命周期双重管理。</para>
        /// </summary>
        private bool ViewEnable(IUiView uiView, bool isChild = false)
        {
            if (null == uiView || null == uiView.View)
                return false;

            if (!isChild)
            {
                if (!_viewStackDic[uiView.Binding.ViewType].Contains(uiView))
                    _viewStackDic[uiView.Binding.ViewType].Add(uiView);

                if (!uiView.View.parent.Equals(_viewParentDic[uiView.Binding.ViewType]))
                    uiView.View.transform.SetParent(_viewParentDic[uiView.Binding.ViewType], false);
            }

            _autoDestroyDic.Remove(uiView);

            uiView.View.gameObject.SetActive(true);
            if (uiView is IUiEnable enable)
                enable.Enable();

            return true;
        }

        /// <summary>
        /// 关闭视窗：调用 IUiDisable.Disable()（如实现）、SetActive false、缓存或销毁
        /// </summary>
        private bool ViewClose(IUiView uiView, bool immediateDestroy, bool onlyDisable)
        {
            if (null == uiView || null == uiView.View || null == uiView.View.gameObject)
                return false;

            if (uiView.View.gameObject.activeSelf)
            {
                if (uiView is IUiDisable disable)
                    disable.Disable();
                uiView.View.gameObject.SetActive(false);
            }

            if (onlyDisable)
                return true;

            if (!uiView.Binding.AutoDestroy || immediateDestroy || _viewStackDic[UIViewType.Cache].Contains(uiView))
            {
                List<IUiView> ownerList = _viewStackDic[UIViewType.Cache].Contains(uiView)
                    ? _viewStackDic[UIViewType.Cache]
                    : _viewStackDic[uiView.Binding.ViewType];
                ViewDestroy(uiView, ownerList);
                return true;
            }

            _autoDestroyDic[uiView] = uiView.Binding.AutoDestroyCountdown;
            _viewStackDic[UIViewType.Cache].Add(uiView);
            _viewStackDic[uiView.Binding.ViewType].Remove(uiView);
            uiView.View.transform.SetParent(_viewParentDic[UIViewType.Cache], false);

            return true;
        }

        #endregion

        #region Internal helper - 内部助手

        /// <summary>
        /// 判断是否为"排他型"视窗类型（同类型仅允许一个活跃实例）
        /// </summary>
        private static bool IsExclusiveViewType(UIViewType type) =>
            type is UIViewType.Page or UIViewType.BottomPermanent or UIViewType.TopPermanent;

        /// <summary>
        /// 获取 Page 栈中 <paramref name="uiView"/> 的前驱页面（仅当 uiView 是栈顶且存在前驱时返回）
        /// </summary>
        private bool TryGetPredecessor(IUiView uiView, out IUiView predecessor)
        {
            predecessor = null;
            if (uiView.Binding.ViewType != UIViewType.Page)
                return false;

            var pageStack = _viewStackDic[UIViewType.Page];
            int idx = pageStack.IndexOf(uiView);
            if (idx != pageStack.Count - 1 || idx <= 0)
                return false;

            predecessor = pageStack[idx - 1];
            return true;
        }

        /// <summary>
        /// 关闭指定类型栈顶的非 <paramref name="exceptView"/> 视窗, 用于新面板入场动画完成后清理紧邻的旧面板，不影响下方页面栈。
        /// </summary>
        private void ViewCloseByTypeExcept(UIViewType uiViewType, IUiView exceptView)
        {
            List<IUiView> views = _viewStackDic[uiViewType];
            int index = views.Count - 1;
            if (index < 0) return;
            if (views[index] == exceptView) index--;
            if (index < 0) return;
            var closeView = views[index];
            if (closeView == exceptView) return;

            bool needCache = closeView.Binding is { ViewType: UIViewType.TopPermanent or UIViewType.BottomPermanent };
            ViewClose(closeView, false, !needCache);
        }

        /// <summary>
        /// 关闭某个类型的全部视窗
        /// </summary>
        private void ViewCloseAllWithType(UIViewType uiViewType, bool immediateDestroy, bool keepFirstView)
        {
            var uiViews = _viewStackDic[uiViewType];
            for (int i = uiViews.Count - 1; i >= 0; i--)
            {
                if (i == 0 && keepFirstView)
                {
                    ViewEnable(uiViews[i]);
                    continue;
                }

                ViewClose(uiViews[i], immediateDestroy, false);
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
    }
}
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
using EasyFramework.Managers.Ui.Popup;
using EasyFramework.Systems.Assets;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

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

            GameObject eventSystem = GameObject.Find("EventSystem");
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

            _currentPageView = null;

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
            return AssetsSystem.Instance.CurrentSystemType == AssetsSystemType.Default
            ? EFC.Projects.AppConst.UIPrefabsPath + viewName
            : viewName;
        }
        
        private static void DestroyObj(Object obj)
        {
            Destroy(obj);
        }

        private IUiView ViewCreate<T>() where T : IUiView, new()
        {
            IUiView uiView = new T();

            string viewName = uiView.GetType().Name;
            
            GameObject prefab = AssetsSystem.Instance.Load<GameObject>(GetAssetPath(viewName));
            if (!prefab)
            {
                D.Exception($"UI Prefab [ {viewName} ] not found in YooAsset or Resources.");
                return null;
            }

            GameObject uiObj = Object.Instantiate(prefab, _viewParentDic[uiView.ViewType], false);
            RectTransform rect = uiObj.GetComponent<RectTransform>();
            uiObj.name = viewName;
            rect.anchorMax = Vector3.one;
            rect.anchorMin = Vector3.zero;
            rect.sizeDelta = Vector3.zero;
            rect.localPosition = Vector3.zero;

            uiView.Bind(rect);
            uiView.SerialId = ++_serialId;
            uiView.Awake();
            return uiView;
        }

        private void ViewDestroy(IUiView uiView, List<IUiView> viewList)
        {
            if (uiView == null) return;

            if (null != uiView.View && null != uiView.View.gameObject)
            {
                if (_currentPageView == uiView)
                    _currentPageView = null;

                uiView.Quit();
                uiView.Dispose();
                DestroyObj(uiView.View.gameObject);
                
                AssetsSystem.Instance.Release(GetAssetPath(uiView.View.name)).Forget();
            }
            
            viewList?.Remove(uiView);
            _autoDestroyDic.Remove(uiView);
        }

        private bool ViewEnable(IUiView uiView, params object[] args)
        {
            if (null == uiView)
                return false;

            if (!_viewStackDic[uiView.ViewType].Contains(uiView))
                _viewStackDic[uiView.ViewType].Add(uiView);

            if (!uiView.View.parent.Equals(_viewParentDic[uiView.ViewType]))
                uiView.View.transform.SetParent(_viewParentDic[uiView.ViewType], false);

            if (uiView.ViewType == UIViewType.Page)
                _currentPageView = uiView;
            _autoDestroyDic.Remove(uiView);

            uiView.View.gameObject.SetActive(true);
            uiView.Enable(args);

            return true;
        }

        private bool ViewClose(IUiView uiView, bool immediateDestroy, bool onlyDisable, params object[] args)
        {
            if (null == uiView)
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
            if (uiView.ViewType == UIViewType.Page && _currentPageView == uiView)
                _currentPageView = null;

            return true;
        }

        private bool ViewCloseByType(UIViewType uiViewType, params object[] args)
        {
            if (_viewStackDic[uiViewType].Count <= 0)
                return false;

            IUiView closeView = _viewStackDic[uiViewType][^1];
            bool needCache = closeView is { ViewType: UIViewType.TopPermanent or UIViewType.BottomPermanent };
            ViewClose(closeView, false, !needCache, args);
            return true;
        }

        /// <summary>
        /// 关闭某个类型的全部视窗
        /// </summary>
        private void ViewCloseAllWithType(UIViewType uiViewType, bool immediateDestroy, bool keepFirstView,
            params object[] args)
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
        /// 打开页面
        /// </summary>
        /// <param name="args">This parameter will be sent to both the UI page that is about to be opened and the UI page that has been closed.
        /// <para>该参数将推送给即将打开的UI页面 和 被关闭的UI页面</para></param>
        public async UniTask<T> OpenPageView<T>(params object[] args) where T : IUiView, new()
        {
            await UniTask.CompletedTask;
            IUiView openView;
            bool needCreate = true;

            if (InViewList<T>(out openView, UIViewType.Page))
            {
                if (_currentPageView == openView)
                    return (T)openView;
                needCreate = false;
            }

            if (needCreate && InViewList<T>(out openView, UIViewType.Cache))
                _viewStackDic[UIViewType.Cache].Remove(openView);
            else
                openView = ViewCreate<T>();

            if (openView == null)
            {
                D.Error($"OpenPageView<{typeof(T).Name}> failed: ViewCreate returned null");
                return default;
            }
            
            ViewCloseByType(openView.ViewType);
            ViewEnable(openView, args);

            return (T)openView;
        }

        /// <summary>
        /// 打开页面
        /// </summary>
        /// <param name="uiView">要被打开的页面</param>
        /// <param name="args">This parameter will be sent to both the UI page that is about to be opened and the UI page that has been closed.
        /// <para>该参数将推送给即将打开的UI页面 和 被关闭的UI页面</para></param>
        public async UniTask<bool> OpenPageView(IUiView uiView, params object[] args)
        {
            await UniTask.CompletedTask;
            if (null == uiView || uiView == _currentPageView ||
                uiView.ViewType is not (UIViewType.Page or UIViewType.BottomPermanent or UIViewType.TopPermanent))
                return false;

            if (InViewList(uiView, UIViewType.Cache))
                _viewStackDic[UIViewType.Cache].Remove(uiView);
            else if (!InViewList(uiView, UIViewType.Page))
                return false;

            ViewCloseByType(uiView.ViewType);
            ViewEnable(uiView, args);
            return true;
        }

        /// <summary>
        /// 获取视窗
        /// </summary>
        /// <typeparam name="T">View type. <para>视窗类型</para></typeparam>
        public async UniTask<T> GetPageView<T>() where T : IUiView
        {
            await UniTask.CompletedTask;
            if (InViewList<T>(out IUiView uiView, UIViewType.Page) ||
                InViewList<T>(out uiView, UIViewType.TopPermanent) ||
                InViewList<T>(out uiView, UIViewType.BottomPermanent))
                return (T)uiView;

            return default;
        }

        /// <summary>
        /// 显示通用弹窗
        /// </summary>
        /// <param name="contents">显示内容</param>
        public async UniTask ShowPopupView(string contents)
        {
            if (InViewList<PopupView>(out var view, UIViewType.Cache))
                _viewStackDic[UIViewType.Cache].Remove(view);

            if (_viewStackDic[UIViewType.Popup].Count >= PopupViewMax)
            {
                var oldestView = _viewStackDic[UIViewType.Popup][_popupIndex];
                if (view != oldestView)
                    ViewClose(oldestView, false, false, null);
                _popupIndex = (_popupIndex + 1) % PopupViewMax;
            }

            view ??= ViewCreate<PopupView>();
            ViewEnable(view, contents);
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// 显示通用提示窗
        /// </summary>
        public async UniTask ShowTipsView(string contents, TipsViewExtraData viewExtraData)
        {
            if (InViewList<TipsView>(out var view, UIViewType.Cache))
                _viewStackDic[UIViewType.Cache].Remove(view);
            view ??= ViewCreate<TipsView>();
            ViewEnable(view, contents, viewExtraData);
            await UniTask.CompletedTask;
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
            await UniTask.CompletedTask;
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
            await UniTask.CompletedTask;
            if (null == uiView)
                return false;
            
            if (uiView.ViewType == UIViewType.Page && _viewStackDic[UIViewType.Page].Count >= 2)
                ViewEnable(_viewStackDic[UIViewType.Page][^2], args);

            return ViewClose(uiView, false, false, args);
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
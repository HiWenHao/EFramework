/*
 * ================================================
 * Describe:      承载UI管理器的逻辑编写.
 * Author:        Alvin8412
 * CreationTime:  2026-04-03 22:11:25
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-25 11:28:04
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;
using EasyFramework.Manager.UI;
using EasyFramework.UI;
using EasyFramework.UI.Popup;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace EasyFramework.Managers
{
    public partial class UiManager
    {
        void ISingleton.Init()
        {
            _target = new GameObject("UI").transform;
            _target.SetParent(EF.Managers);

            UICamera = new GameObject("UICamera").AddComponent<Camera>();
            UICamera.orthographic = true;
            UICamera.orthographicSize = Screen.height / 2.0f;
            UICamera.farClipPlane = 200.0f;
            UICamera.cullingMask = 32;
            UICamera.clearFlags = CameraClearFlags.Depth;
            UICamera.transform.SetParent(_target, false);

            UniversalAdditionalCameraData ucd = UICamera.GetUniversalAdditionalCameraData();
            ucd.renderType = CameraRenderType.Overlay;
            UniversalAdditionalCameraData ucdMain = Camera.main.GetUniversalAdditionalCameraData();
            if (null != ucdMain.scriptableRenderer)
                ucdMain.cameraStack.Add(UICamera);

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
                for (var i = 0; i < uiViews.Value.Count; i++)
                {
                    var uiView = uiViews.Value[i];
                    if (uiViews.Key != UIViewType.Cache)
                    {
                        uiView.Update(elapse, realElapse);
                        continue;
                    }

                    if ((_autoDestroyDic[uiView] -= elapse) > 0.0f)
                        continue;
                    ViewDestroy(uiView, uiViews.Value);
                }
            }
        }

        void ISingleton.Quit()
        {
            foreach (var uiViews in _viewStackDic)
            {
                for (int i = uiViews.Value.Count - 1; i >= 0; i--)
                {
                    ViewDestroy(uiViews.Value[i], uiViews.Value);
                }

                uiViews.Value.Clear();

                Destroy(_viewParentDic[uiViews.Key].gameObject);
            }

            _currentPageView = null;
            
            _viewStackDic.Clear();
            _viewStackDic = null;
            _viewParentDic.Clear();
            _viewParentDic = null;

            _autoDestroyDic.Clear();
            _autoDestroyDic = null;

            Destroy(UICamera.gameObject);
            UICamera = null;

            Destroy(_target.gameObject);
            _target = null;
        }

        private static void Destroy(Object obj)
        {
            Object.Destroy(obj);
        }

        private IUiView ViewCreate<T>() where T : IUiView, new()
        {
            IUiView uiView = new T();

            string viewName = uiView.GetType().Name;
            GameObject prefab = EF.Patch.IsUse
                ? EF.Load.LoadInYooSync<GameObject>(viewName)
                : EF.Load.LoadInResources<GameObject>(EF.Projects.AppConst.UIPrefabsPath + viewName);
            if (!prefab)
                prefab = EF.Load.LoadInResources<GameObject>(EF.Projects.AppConst.UIPrefabsPath + viewName);
            if (!prefab)
                D.Exception($"UI Prefab [ {viewName} ] not found in YooAsset or Resources Folder.");
            GameObject uiObj = Object.Instantiate(prefab, _viewParentDic[uiView.ViewType], true);
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
            uiView.Quit();
            uiView.Dispose();
            Destroy(uiView.View.gameObject);
            viewList?.Remove(uiView);
            _autoDestroyDic.Remove(uiView);
        }

        //  displaceCurrentPage 用来记录是否替换当前所展示的UI页面
        private void ViewEnable(IUiView uiView, bool displaceCurrentPage, params object[] args)
        {
            if (!_viewStackDic[uiView.ViewType].Contains(uiView))
                _viewStackDic[uiView.ViewType].Add(uiView);

            if (!uiView.View.parent.Equals(_viewParentDic[uiView.ViewType]))
                uiView.View.transform.SetParent(_viewParentDic[uiView.ViewType], false);
            uiView.View.SetSiblingIndex(PopupViewMax - 1);

            if (displaceCurrentPage)
                _currentPageView = uiView;
            _autoDestroyDic.Remove(uiView);

            uiView.Enable(args);
            uiView.View.gameObject.SetActive(true);
        }

        //  cache 用来判断是否需要将页面进入销毁倒计时
        private bool ViewClose(IUiView uiView, bool cache, params object[] args)
        {
            if (null == uiView)
                return false;

            if (uiView.View.gameObject.activeSelf)
            {
                uiView.DisEnable(args);
                uiView.View.gameObject.SetActive(false);
            }

            if (!cache || !uiView.AutoDestroy || _viewStackDic[UIViewType.Cache].Contains(uiView))
                return true;
            
            _autoDestroyDic[uiView] = uiView.AutoDestroyCountdown;
            _viewStackDic[UIViewType.Cache].Add(uiView);
            _viewStackDic[uiView.ViewType].Remove(uiView);
            uiView.View.transform.SetParent(_viewParentDic[UIViewType.Cache], false);
            
            return true;
        }

        /// <summary>
        /// 判断特定属性视窗是否存在
        /// </summary>
        /// <param name="uiView">视窗</param>
        /// <param name="viewType">视窗类型</param>
        private bool InViewList<T>(out IUiView uiView, UIViewType viewType)
        {
            foreach (IUiView view in _viewStackDic[viewType])
            {
                if (typeof(T) != view.GetType())
                    continue;

                uiView = view;
                return true;
            }

            uiView = null;
            return false;
        }

        /// <summary>
        /// 打开页面
        /// </summary>
        /// <param name="args">This parameter will be sent to both the UI page that is about to be opened and the UI page that has been closed.
        /// <para>该参数将推送给即将打开的UI页面 和 被关闭的UI页面</para></param>
        public T OpenPageView<T>(params object[] args) where T : IUiView, new()
        {
            IUiView openView;
            IUiView closeView = null;
            bool needCreate = true;

            if (InViewList<T>(out openView, UIViewType.Page))
            {
                if (_currentPageView == openView)
                    return (T)openView;
                needCreate = false;
            }

            if (needCreate && InViewList<T>(out openView, UIViewType.Cache))
            {
                _viewStackDic[UIViewType.Cache].Remove(openView);
                needCreate = false;
            }

            if (needCreate)
                openView = ViewCreate<T>();

            if (_viewStackDic[openView.ViewType].Count > 0)
                closeView = _viewStackDic[openView.ViewType][^1];
            
            bool needCache = closeView is { ViewType: UIViewType.TopPermanent or UIViewType.BottomPermanent };
            
            ViewClose(closeView, needCache, args);
            ViewEnable(openView, true, args);
            
            return (T)openView;
        }

        /// <summary>
        /// 获取视窗
        /// </summary>
        /// <typeparam name="T">View type. <para>视窗类型</para></typeparam>
        public T GetPageView<T>() where T : IUiView
        {
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
        public void ShowPopupView(string contents)
        {
            if (!InViewList<PopupView>(out IUiView view, UIViewType.Cache))
            {
                if (_viewStackDic[UIViewType.Popup].Count >= PopupViewMax)
                {
                    if (_popupIndex >= _viewStackDic[UIViewType.Popup].Count)
                        _popupIndex = 0;
                    view = _viewStackDic[UIViewType.Popup][_popupIndex++];
                }
            }

            view ??= ViewCreate<PopupView>();
            ViewEnable(view, false, contents);
        }

        /// <summary>
        /// 显示通用提示窗
        /// </summary>
        /// <param name="contents">显示内容</param>
        /// <param name="viewExtraData">附加数据</param>
        public void ShowTipsView<T>(string contents, TipsViewExtraData viewExtraData) where T : TipsView, new()
        {
            _tipsView ??= ViewCreate<T>();
            ViewEnable(_tipsView, false, contents, viewExtraData);
        }
        
        /// <summary>
        /// 关闭UI视窗
        /// </summary>
        /// <param name="args">This parameter will be sent to both the UI page that is about to be opened and the UI page that has been closed.
        /// <para>该参数将推送给即将打开的UI页面 和 被关闭的UI页面</para></param>
        public bool CloseView<T>(params object[] args) where T : IUiView
        {
            for (int i = 5; i > 0; i--)
            {
                if (!InViewList<T>(out IUiView uiView, (UIViewType)i))
                    continue;

                if (uiView.ViewType == UIViewType.Page)
                {
                    if (_viewStackDic[UIViewType.Page].Count >= 2)
                        ViewEnable(_viewStackDic[UIViewType.Page][^2], true, args);
                    else
                        _currentPageView = null;
                }
                
                return ViewClose(uiView, true, args);
            }

            return false;
        }
        
        /// <summary>
        /// 关闭视窗
        /// </summary>
        /// <param name="uiView">要被关闭的视窗</param>
        /// <param name="args">参数</param>
        /// <returns>是否关闭成功</returns>
        public bool CloseView(IUiView uiView, params object[] args)
        {
            return ViewClose(uiView, true, args);
        }

    }
}

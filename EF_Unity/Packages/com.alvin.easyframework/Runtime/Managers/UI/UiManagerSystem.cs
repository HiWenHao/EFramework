/*
 * ================================================
 * Describe:      承载UI管理器的逻辑编写.
 * Author:        Alvin8412
 * CreationTime:  2026-04-03 22:11:25
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-03 22:11:25
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;
using EasyFramework.Manager.UI.Tips;
using EasyFramework.UI.Popup;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace EasyFramework.Manager.UI
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
            Camera.main.GetUniversalAdditionalCameraData().cameraStack.Add(UICamera);

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
            _viewParentDic = new Dictionary<UIViewType, Transform>(typeCount);
            for (int i = 0; i < typeCount; i++)
            {
                Transform trans = new GameObject($"ViewType{i}-{(UIViewType)i}").transform;
                trans.SetParent(root.transform);
                trans.gameObject.SetActive(i != 0);
                trans.gameObject.layer = LayerMask.NameToLayer("UI");
                RectTransform rect = trans.gameObject.AddComponent<RectTransform>();
                rect.sizeDelta = Vector2.zero;
                rect.anchorMax = Vector3.one;
                rect.anchorMin = Vector3.zero;
                rect.localPosition = Vector3.zero;

                _viewParentDic.Add((UIViewType)i, rect);

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

            _allUsedViewsDict = new Dictionary<uint, IUiView>();
            _popupViewsList = new List<IUiView>(PopupViewMax);
            PageInit();
        }

        void IUpdate.Update(float elapse, float realElapse)
        {
            foreach (var uiView in _allUsedViewsDict)
            {
                if (!uiView.Value.View.gameObject.activeSelf)
                    continue;

                uiView.Value.Update(elapse, realElapse);
            }

            PageUpdate(elapse, realElapse);
        }

        void ISingleton.Quit()
        {
            PageQuit();
            _popupGameObject = null;

            _popupViewsList.Clear();
            _popupViewsList = null;
            
            foreach (KeyValuePair<uint, IUiView> view in _allUsedViewsDict)
            {
                view.Value.Quit();
                view.Value.Dispose();
                Destroy(view.Value.View.gameObject);
            }

            _allUsedViewsDict.Clear();
            _allUsedViewsDict = null;
            Destroy(UICamera.gameObject);
            UICamera = null;

            foreach (var viewParent in _viewParentDic)
            {
                Destroy(viewParent.Value.gameObject);
            }

            _viewParentDic.Clear();
            _viewParentDic = null;

            Destroy(_target.gameObject);
            _target = null;
        }

        private void Destroy(Object obj)
        {
            Object.Destroy(obj);
        }

        #region PageView

        /// <summary>
        /// 打开页面
        /// </summary>
        /// <param name="args">This parameter will be sent to both the UI page that is about to be opened and the UI page that has been closed.
        /// <para>该参数将推送给即将打开的UI页面 和 被关闭的UI页面</para></param>
        public void OpenPage<T>(params object[] args) where T : IUiView, new()
        {
            IUiView uiView;
            bool needCreate = true;
            
            if (InViewList<T>(out uiView, _viewStackDic[UIViewType.Page]))
            {
                if (_currentPageView == uiView)
                    return;
                needCreate = false;
            }
            
            if (InViewList<T>(out uiView, _viewStackDic[UIViewType.Cache]))
            {
                _viewStackDic[UIViewType.Cache].Remove(uiView);
                needCreate = false;
            }

            if (needCreate)
            {
                uiView = new T();
                string viewName = typeof(T).Name;
                GameObject prefab = EF.Patch.IsUse ? EF.Load.LoadInYooSync<GameObject>(viewName) 
                    : EF.Load.LoadInResources<GameObject>(EF.Projects.AppConst.UIPrefabsPath + viewName);
                if (!prefab)
                    D.Exception($"UI Prefab [ {viewName} ] not found in YooAsset or Resources Folder.");
                GameObject uiObj = Object.Instantiate(prefab, _viewParentDic[UIViewType.Page], true);
                RectTransform rect = uiObj.GetComponent<RectTransform>();
            
                rect.anchorMax = Vector3.one;
                rect.anchorMin = Vector3.zero;
                rect.sizeDelta = Vector3.zero;
                rect.localPosition = Vector3.zero;
        
                uiView.Bind(rect);
                uiView.SerialId = ++_serialId;
                uiObj.name = viewName;
                uiView.Awake();
            }
            
            ViewClose(_currentPageView, false, args);
            ViewEnable(uiView, args);
        }

        /// <summary>
        /// 获取视窗
        /// </summary>
        /// <typeparam name="T">View type. <para>视窗类型</para></typeparam>
        public IUiView GetView<T>() where T : IUiView, new()
        {
            InViewList<T>(out IUiView uiView, _viewStackDic[UIViewType.Page]);
            return uiView;
        }
        
        /// <summary>
        /// 关闭UI视窗
        /// </summary>
        /// <param name="args">This parameter will be sent to both the UI page that is about to be opened and the UI page that has been closed.
        /// <para>该参数将推送给即将打开的UI页面 和 被关闭的UI页面</para></param>
        public void ClosePage<T>(params object[] args) where T : IUiView
        {
            if (!InViewList<T>(out IUiView uiView, _viewStackDic[UIViewType.Page]))
                return;

            if (uiView == _currentPageView)
            {
                if (_viewStackDic[UIViewType.Page].Count >= 2)
                    ViewEnable(_viewStackDic[UIViewType.Page][^2], args);
                else
                    _currentPageView = null;
            }
            
            ViewClose(uiView, true, args);
        }
        
        private void PageInit()
        {
            _autoDestroyDic ??= new Dictionary<IUiView, float>();
            _viewStackDic ??= new Dictionary<UIViewType, List<IUiView>>();
            _viewStackDic[UIViewType.Page] = new List<IUiView>();
            _viewStackDic[UIViewType.Cache] = new List<IUiView>();
        }

        private void PageUpdate(float elapse, float realElapse)
        {
            foreach (IUiView uiView in _viewStackDic[UIViewType.Page])
            {
                uiView.Update(elapse, realElapse);
            }

            for (var i = 0; i < _viewStackDic[UIViewType.Cache].Count; i++)
            {
                IUiView uiView = _viewStackDic[UIViewType.Cache][i];
                if (!((_autoDestroyDic[uiView] -= elapse) <= 0.0f))
                    continue;
                ViewQuit(uiView);
            }
        }
        
        private  void PageQuit()
        {
            if (null == _viewStackDic[UIViewType.Page])
                return;

            List<IUiView> pages = _viewStackDic[UIViewType.Page];
            for (int i = pages.Count - 1; i >= 0; i--)
            {
                pages[i].Quit();
                pages[i].Dispose();
                pages.RemoveAt(i);
            }
            _viewStackDic[UIViewType.Page].Clear();
            _viewStackDic[UIViewType.Page] = null;
            _viewStackDic.Remove(UIViewType.Page);
            
            _autoDestroyDic.Clear();
            _autoDestroyDic = null;
        }
        
        private void ViewEnable(IUiView uiView, params object[] args)
        {
            uiView.Enable(args);
            uiView.View.gameObject.SetActive(true);
            
            if (!_viewStackDic[UIViewType.Page].Contains(uiView))
                _viewStackDic[UIViewType.Page].Add(uiView);

            if (!uiView.View.parent.Equals(_viewParentDic[UIViewType.Page]))
                uiView.View.transform.SetParent(_viewParentDic[UIViewType.Page], false);
            
            _currentPageView = uiView;
            _autoDestroyDic.Remove(uiView);
        }
        
        private void ViewClose(IUiView uiView, bool cache, params object[] args)
        {
            if (null == uiView)
                return;
            
            uiView.DisEnable(args);
            uiView.View.gameObject.SetActive(false);
            
            if (!cache)
                return;

            _autoDestroyDic[uiView] = 5.0f;
            D.Log(_autoDestroyDic.Count);
            _viewStackDic[UIViewType.Cache].Add(uiView);
            _viewStackDic[UIViewType.Page].Remove(uiView);
            uiView.View.transform.SetParent(_viewParentDic[UIViewType.Cache], false);
        }

        private void ViewQuit(IUiView uiView)
        {
            uiView.Quit();
            uiView.Dispose();
            Destroy(uiView.View.gameObject);
            _autoDestroyDic.Remove(uiView);
            _viewStackDic[UIViewType.Cache].Remove(uiView);
        }

        private bool InViewList<T>(out IUiView uiView, List<IUiView> viewList)
        {
            foreach (IUiView view in viewList)
            {
                if (typeof(T) != view.GetType())
                    continue;
                uiView = view;
                return true;
            }
            uiView = null; 
            return false;
        }

        #endregion

        #region Pooup

        /// <summary>
        /// 显示通用弹窗
        /// </summary>
        /// <param name="contents">显示内容</param>
        public void ShowPopupView(string contents)
        {
            IUiView view = null;
            foreach (var popupView in _popupViewsList)
            {
                if (popupView.View.gameObject.activeSelf)
                    continue;
                view = popupView;
                break;
            }

            if (view == null && _popupViewsList.Count >= PopupViewMax)
            {
                if (_popupIndex >= _popupViewsList.Count)
                    _popupIndex = 0;
                view = _popupViewsList[_popupIndex++];
            }

            if (view == null)
            {
                if (null == _popupGameObject)
                    _popupGameObject = Resources.Load<GameObject>($"{EF.Projects.AppConst.UIPrefabsPath}PopupView");
                RectTransform rect = Object.Instantiate(_popupGameObject, _viewParentDic[UIViewType.Popup], true)
                    .GetComponent<RectTransform>();
                
                rect.anchorMax = Vector3.one;
                rect.anchorMin = Vector3.zero;
                rect.sizeDelta = Vector3.zero;
                rect.localPosition = Vector3.zero;
                
                view = new PopupView();
                view.Bind(rect);
                view.SerialId = ++_serialId;
                view.Awake();
                _popupViewsList.Add(view);
                _allUsedViewsDict.Add(_serialId, view);
            }

            view.View.SetSiblingIndex(PopupViewMax - 1);
            view.Enable(contents);
            view.View.gameObject.SetActive(true);
        }

        #endregion

        #region TipsView

        /// <summary>
        /// 显示通用提示窗
        /// </summary>
        /// <param name="contents">显示内容</param>
        /// <param name="viewExtraData">附加数据</param>
        public void ShowTips(string contents, TipsViewExtraData viewExtraData)
        {
            if (null == _tipsView)
            {
                GameObject tipsObj =
                    Object.Instantiate(Resources.Load<GameObject>($"{EF.Projects.AppConst.UIPrefabsPath}TipsView"),
                        _viewParentDic[UIViewType.Tips], true);
                RectTransform rect = tipsObj.GetComponent<RectTransform>();
                
                rect.anchorMax = Vector3.one;
                rect.anchorMin = Vector3.zero;
                rect.sizeDelta = Vector3.zero;
                rect.localPosition = Vector3.zero;

                _tipsView = new TipsView();
                _tipsView.Bind(rect);
                _tipsView.SerialId = ++_serialId;
                _tipsView.Awake();
                _allUsedViewsDict.Add(_serialId, _tipsView);
            }
            _tipsView.Enable(contents, viewExtraData);
            _tipsView.View.gameObject.SetActive(true);
        }

        #endregion
    }
}
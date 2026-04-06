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
using System.Linq;
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
            //Camera.main.GetUniversalAdditionalCameraData().cameraStack.Add(UICamera);

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
            _allCachedViewsDict = new Dictionary<string, IUiView>();
            _popupViewsList = new List<IUiView>(PopupViewMax);
        }

        void IUpdate.Update(float elapse, float realElapse)
        {
            foreach (var uiView in _allUsedViewsDict)
            {
                if (!uiView.Value.View.gameObject.activeSelf)
                    continue;

                uiView.Value.Update(elapse, realElapse);
            }
        }

        void ISingleton.Quit()
        {
            _popupGameObject = null;

            _popupViewsList.Clear();
            _popupViewsList = null;
            
            foreach (KeyValuePair<uint, IUiView> view in _allUsedViewsDict)
            {
                view.Value.Quit();
                view.Value.Dispose();
                Destroy(view.Value.View.gameObject);
            }
            foreach (KeyValuePair<string, IUiView> view in _allCachedViewsDict)
            {
                view.Value.Quit();
                view.Value.Dispose();
                Destroy(view.Value.View.gameObject);
            }

            _allUsedViewsDict.Clear();
            _allUsedViewsDict = null;
            _allCachedViewsDict.Clear();
            _allCachedViewsDict = null;

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

        public void OpenPage(IUiView view, params object[] args)
        {
            if (view == null || _allUsedViewsDict.ContainsKey(view.SerialId))
                return;
            string viewName = view.GetType().Name;
            foreach (KeyValuePair<string, IUiView> uiView in _allCachedViewsDict)
            {
                if (!uiView.Value.View.name.Contains(viewName))
                    continue;

                if (!_allCachedViewsDict.Remove(uiView.Value.View.name, out IUiView cachedView))
                    continue;
                
                _allUsedViewsDict.Add(cachedView.SerialId, cachedView);
                cachedView.View.gameObject.SetActive(true);
                cachedView.Enable(args);
                
                _currentPageView?.DisEnable(args);
                _currentPageView?.View.gameObject.SetActive(false);
                _currentPageView = cachedView;
                return;
            }
            
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
            
            view.Bind(rect);
            view.SerialId = ++_serialId;
            uiObj.name = $"{_serialId} - {viewName}";
            
            _currentPageView?.DisEnable(args);
            _currentPageView?.View.gameObject.SetActive(false);
            _currentPageView = view;
        }

        public void ClosePage(IUiView view, params object[] args)
        {
            if (!_allUsedViewsDict.Remove(view.SerialId, out IUiView pageView))
                return;
            
            _allCachedViewsDict.Add(pageView.View.name, pageView);
                
            if (_currentPageView == view)
            {
                _currentPageView = _allUsedViewsDict.Last().Value;
                
                _currentPageView.Enable(args);
                _currentPageView.View.gameObject.SetActive(true);
                
            }
                
            view.DisEnable(args);
            view.View.gameObject.SetActive(false);
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
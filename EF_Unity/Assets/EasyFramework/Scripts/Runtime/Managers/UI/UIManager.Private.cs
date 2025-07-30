/* 
 * ================================================
 * Describe:      The class is ui page m_managerLevel
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-03-02 18:00:58
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-05-26 16:26:04
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
//using UnityEngine.Rendering.Universal;

namespace EasyFramework.Managers
{
    public partial class UIManager : Singleton<UIManager>, IManager, IUpdate
    {
        private const string PAGE_BASE_OBJECT_NAME = "Page_Base";

        int _serial;
        int _pageCount;
        bool _pageInit;

        GameObject _root;
        Transform _target;
        Transform _pageBaseObject;

        GameObject _currentObj;
        UIPageBase _currentPage;

        Stack<UIPageBase> _useUIStack;
        Stack<GameObject> _useGOStack;

        Dictionary<int, UIPageBase> _readyUIDic;
        Dictionary<int, GameObject> _readyGODic;

        void ISingleton.Init()
        {
            _target = new GameObject("UI").transform;
            _target.SetParent(EF.Managers);

            //CreateTimeEvent ui root object.
            {
                UICamera = new GameObject("_UICamera").AddComponent<Camera>();
                UICamera.orthographic = true;
                UICamera.orthographicSize = UiScreenHeight / 2.0f;
                UICamera.farClipPlane = 200.0f;
                UICamera.cullingMask = 32;
                UICamera.clearFlags = CameraClearFlags.Depth;

                _root = new GameObject("_UIRoot");
                _root.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
                _root.GetComponent<Canvas>().worldCamera = UICamera;
                _root.layer = 5;
                //UniversalAdditionalCameraData _ca = UICamera.GetUniversalAdditionalCameraData();
                //_ca.renderType = CameraRenderType.Overlay;
                //Camera.main.GetUniversalAdditionalCameraData().cameraStack.Add(UICamera);
                CanvasScaler _cs = _root.AddComponent<CanvasScaler>();
                _cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                _cs.referenceResolution = new Vector2(UiScreenWidth, UiScreenHeight);
                _cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                _root.transform.SetParent(_target, false);
                UICamera.transform.SetParent(_target, false);
            }
            GameObject _eventSystem = GameObject.Find("EventSystem");
            if (!_eventSystem)
            {
                _eventSystem = new GameObject("EventSystem");
                _eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                _eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                _eventSystem.transform.parent = _target;
            }
        }

        void IUpdate.Update(float elapse, float realElapse)
        {
            if (!_pageInit)
                return;

            _currentPage?.Update(elapse, realElapse);
        }

        void ISingleton.Quit()
        {
            PageExit();

            Destroy(UICamera.gameObject);
            UICamera = null;

            Destroy(_root);
            Destroy(_target.gameObject);

            _root = null;
            _target = null;
        }

        void Destroy(Object obj)
        {
            Object.Destroy(obj);
        }

        void PageInit()
        {
            RectTransform rect = new GameObject(PAGE_BASE_OBJECT_NAME).AddComponent<RectTransform>();
            _pageBaseObject = rect.transform;
            _pageBaseObject.SetParent(_root.transform, false);
            _pageBaseObject.localPosition = Vector3.zero;
            rect.anchorMin = Vector3.zero;
            rect.anchorMax = Vector3.one;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector2.one;

            _useUIStack = new Stack<UIPageBase>();
            _useGOStack = new Stack<GameObject>();
            _readyUIDic = new Dictionary<int, UIPageBase>();
            _readyGODic = new Dictionary<int, GameObject>();
        }

        void PageExit()
        {
            if (!_pageInit)
                return;

            while (_useUIStack.Count != 0)
            {
                _useUIStack.Pop().Quit();
                Destroy(_useGOStack.Pop());
            }

            int[] keyArr = _readyGODic.Keys.ToArray();

            for (int i = 0; i < keyArr.Length; i++)
            {
                _readyUIDic[keyArr[i]].Quit();
                Destroy(_readyGODic[keyArr[i]]);
                _readyUIDic.Remove(keyArr[i]);
                _readyGODic.Remove(keyArr[i]);
            }

            _readyUIDic.Clear();
            _readyGODic.Clear();
            _readyUIDic = null;
            _readyGODic = null;

            _useUIStack.Clear();
            _useGOStack.Clear();
            _useUIStack = null;
            _useGOStack = null;

            Destroy(_pageBaseObject.gameObject);
            _pageBaseObject = null;
        }

        private GameObject PageCreated(UIPageBase page)
        {
            GameObject uiObj = Object.Instantiate(EF.Load.LoadInResources<GameObject>(EF.Projects.AppConst.UIPrefabsPath + page.GetType().Name));
            uiObj.transform.SetParent(_pageBaseObject);
            RectTransform rect = uiObj.GetComponent<RectTransform>();
            rect.anchorMax = Vector2.one;
            rect.anchorMin = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector2.one;
            uiObj.name = page.GetType().Name;
            page.SerialId = _serial++;

            Canvas cv = uiObj.GetComponent<Canvas>();
            cv.overrideSorting = true;
            cv.sortingOrder = page.SerialId * 100;

            return uiObj;
        }

        private UIPageBase PageOpen(UIPageBase page, bool hideCurrent, params object[] args)
        {
            if (!_pageInit)
            {
                _pageInit = true;
                PageInit();
            }
            if (null != _currentPage)
            {
                _currentPage.OnFocus(false);
                _currentObj.SetActive(!hideCurrent);
            }

            int readyKey = -1;
            foreach (KeyValuePair<int, GameObject> item in _readyGODic)
            {
                if (item.Value.name.Equals(page.GetType().Name))
                {
                    readyKey = item.Key;
                    break;
                }
            }

            if (readyKey == -1)
            {
                _currentPage = page;
                _currentObj = PageCreated(page);

                _useGOStack.Push(_currentObj);
                _useUIStack.Push(page);

                _currentPage.Awake(_currentObj, args);
                _currentPage.Open(args);
            }
            else
            {
                _currentObj = _readyGODic[readyKey];
                _currentPage = _readyUIDic[readyKey];

                _useGOStack.Push(_currentObj);
                _useUIStack.Push(_currentPage);

                _currentPage.Open(args);

                _readyUIDic.Remove(readyKey);
                _readyGODic.Remove(readyKey);
            }

            ++_pageCount;

            _currentObj.SetActive(true);
            return _currentPage;
        }

        private void PageClose(bool destroy = false, params object[] args)
        {
            if (_pageCount <= 0)
                return;

            UIPageBase ui = _useUIStack.Pop();
            GameObject go = _useGOStack.Pop();

            ui.Close();
            if (destroy)
            {
                ui.Quit();
                Destroy(go);
            }
            else
            {
                go.SetActive(false);
                _readyUIDic.Add(ui.SerialId, ui);
                _readyGODic.Add(ui.SerialId, go);
            }

            _currentPage = null;

            if (--_pageCount > 0)
            {
                _currentObj = _useGOStack.Peek();
                _currentPage = _useUIStack.Peek();

                _currentObj.SetActive(true);
                _currentPage.OnFocus(true, args);
            }
        }
    }
}

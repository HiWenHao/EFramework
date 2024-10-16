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

        int m_Serial;
        int m_PageCount;
        bool m_PageInit;

        GameObject m_Root;
        Transform m_target;
        Transform m_pageBaseObject;

        GameObject m_CurrentObj;
        UIPageBase m_CurrentPage;

        Stack<UIPageBase> m_UseUIStack;
        Stack<GameObject> m_UseGOStack;

        Dictionary<int, UIPageBase> m_ReadyUIDic;
        Dictionary<int, GameObject> m_ReadyGODic;

        void ISingleton.Init()
        {
            m_target = new GameObject("UI").transform;
            m_target.SetParent(EF.Managers);

            //CreateTimeEvent ui root object.
            {
                UICamera = new GameObject("_UICamera").AddComponent<Camera>();
                UICamera.orthographic = true;
                UICamera.orthographicSize = UiScreenHeight / 2.0f;
                UICamera.farClipPlane = 200.0f;
                UICamera.cullingMask = 32;
                UICamera.clearFlags = CameraClearFlags.Depth;

                m_Root = new GameObject("_UIRoot");
                m_Root.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
                m_Root.GetComponent<Canvas>().worldCamera = UICamera;
                m_Root.layer = 5;
                //UniversalAdditionalCameraData _ca = UICamera.GetUniversalAdditionalCameraData();
                //_ca.renderType = CameraRenderType.Overlay;
                //Camera.main.GetUniversalAdditionalCameraData().cameraStack.Add(UICamera);
                CanvasScaler _cs = m_Root.AddComponent<CanvasScaler>();
                _cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                _cs.referenceResolution = new Vector2(UiScreenWidth, UiScreenHeight);
                _cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                m_Root.transform.SetParent(m_target, false);
                UICamera.transform.SetParent(m_target, false);
            }
            GameObject _eventSystem = GameObject.Find("EventSystem");
            if (!_eventSystem)
            {
                _eventSystem = new GameObject("EventSystem");
                _eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                _eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                _eventSystem.transform.parent = m_target;
            }
        }

        void IUpdate.Update(float elapse, float realElapse)
        {
            if (!m_PageInit)
                return;

            m_CurrentPage?.Update(elapse, realElapse);
        }

        void ISingleton.Quit()
        {
            PageExit();

            Destroy(UICamera.gameObject);
            UICamera = null;

            Destroy(m_Root);
            Destroy(m_target.gameObject);

            m_Root = null;
            m_target = null;
        }

        void Destroy(Object obj)
        {
            Object.Destroy(obj);
        }

        void PageInit()
        {
            RectTransform _rect = new GameObject(PAGE_BASE_OBJECT_NAME).AddComponent<RectTransform>();
            m_pageBaseObject = _rect.transform;
            m_pageBaseObject.SetParent(m_Root.transform, false);
            m_pageBaseObject.localPosition = Vector3.zero;
            _rect.anchorMin = Vector3.zero;
            _rect.anchorMax = Vector3.one;
            _rect.sizeDelta = Vector2.zero;
            _rect.localScale = Vector2.one;

            m_UseUIStack = new Stack<UIPageBase>();
            m_UseGOStack = new Stack<GameObject>();
            m_ReadyUIDic = new Dictionary<int, UIPageBase>();
            m_ReadyGODic = new Dictionary<int, GameObject>();
        }

        void PageExit()
        {
            if (!m_PageInit)
                return;

            while (m_UseUIStack.Count != 0)
            {
                m_UseUIStack.Pop().Quit();
                Destroy(m_UseGOStack.Pop());
            }

            int[] keyArr = m_ReadyGODic.Keys.ToArray();

            for (int i = 0; i < keyArr.Length; i++)
            {
                m_ReadyUIDic[keyArr[i]].Quit();
                Destroy(m_ReadyGODic[keyArr[i]]);
                m_ReadyUIDic.Remove(keyArr[i]);
                m_ReadyGODic.Remove(keyArr[i]);
            }

            m_ReadyUIDic.Clear();
            m_ReadyGODic.Clear();
            m_ReadyUIDic = null;
            m_ReadyGODic = null;

            m_UseUIStack.Clear();
            m_UseGOStack.Clear();
            m_UseUIStack = null;
            m_UseGOStack = null;

            Destroy(m_pageBaseObject.gameObject);
            m_pageBaseObject = null;
        }

        private GameObject PageCreated(UIPageBase page)
        {
            GameObject _uiObj = Object.Instantiate(EF.Load.LoadInResources<GameObject>(EF.Projects.AppConst.UIPrefabsPath + page.GetType().Name));
            _uiObj.transform.SetParent(m_pageBaseObject);
            RectTransform _rect = _uiObj.GetComponent<RectTransform>();
            _rect.anchorMax = Vector2.one;
            _rect.anchorMin = Vector2.zero;
            _rect.sizeDelta = Vector2.zero;
            _rect.anchoredPosition = Vector2.zero;
            _rect.localScale = Vector2.one;
            _uiObj.name = page.GetType().Name;
            page.SerialId = m_Serial++;

            Canvas _cv = _uiObj.GetComponent<Canvas>();
            _cv.overrideSorting = true;
            _cv.sortingOrder = page.SerialId * 100;

            return _uiObj;
        }

        private UIPageBase PageOpen(UIPageBase page, bool hideCurrent, params object[] args)
        {
            if (!m_PageInit)
            {
                m_PageInit = true;
                PageInit();
            }
            if (null != m_CurrentPage)
            {
                m_CurrentPage.OnFocus(false);
                m_CurrentObj.SetActive(!hideCurrent);
            }

            int _readyKey = -1;
            foreach (KeyValuePair<int, GameObject> item in m_ReadyGODic)
            {
                if (item.Value.name.Equals(page.GetType().Name))
                {
                    _readyKey = item.Key;
                    break;
                }
            }

            if (_readyKey == -1)
            {
                m_CurrentPage = page;
                m_CurrentObj = PageCreated(page);

                m_UseGOStack.Push(m_CurrentObj);
                m_UseUIStack.Push(page);

                m_CurrentPage.Awake(m_CurrentObj, args);
                m_CurrentPage.Open(args);
            }
            else
            {
                m_CurrentObj = m_ReadyGODic[_readyKey];
                m_CurrentPage = m_ReadyUIDic[_readyKey];

                m_UseGOStack.Push(m_CurrentObj);
                m_UseUIStack.Push(m_CurrentPage);

                m_CurrentPage.Open(args);

                m_ReadyUIDic.Remove(_readyKey);
                m_ReadyGODic.Remove(_readyKey);
            }

            ++m_PageCount;

            m_CurrentObj.SetActive(true);
            return m_CurrentPage;
        }

        private void PageClose(bool destroy = false, params object[] args)
        {
            if (m_PageCount <= 0)
                return;

            UIPageBase _ui = m_UseUIStack.Pop();
            GameObject _go = m_UseGOStack.Pop();

            _ui.Close();
            if (destroy)
            {
                _ui.Quit();
                Destroy(_go);
            }
            else
            {
                _go.SetActive(false);
                m_ReadyUIDic.Add(_ui.SerialId, _ui);
                m_ReadyGODic.Add(_ui.SerialId, _go);
            }

            m_CurrentPage = null;

            if (--m_PageCount > 0)
            {
                m_CurrentObj = m_UseGOStack.Peek();
                m_CurrentPage = m_UseUIStack.Peek();

                m_CurrentObj.SetActive(true);
                m_CurrentPage.OnFocus(true, args);
            }
        }
    }
}

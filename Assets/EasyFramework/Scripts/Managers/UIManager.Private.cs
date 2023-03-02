/* 
 * ================================================
 * Describe:      The class is ui page manager
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-03-02 18:00:58
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-03-02 18:00:58
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace EasyFramework.Managers
{
    public partial class UIManager : Singleton<UIManager>, IManager, IUpdate
    {
        private readonly string pageBaseObjectName = "UIPages";
        private readonly string showBoxBaseObjectName = "UIShowBox";
        private Transform m_target, pageBaseObject, showBoxBaseObject;

        #region Page information
        private int m_int_UiCount;
        private ParticleSystem m_ClickPS;
        private UIPageBase m_currentPage;
        private GameObject m_currentObj;

        private Stack<UIPageBase> m_stack_UI;
        private Stack<GameObject> m_stack_GO;
        #endregion

        #region New Page Information
        int m_int_SerialCount;

        Dictionary<int, UIPageBase> m_dic_UI;
        Dictionary<int, GameObject> m_dic_GO;
        #endregion

        #region Dialog information
        GameObject BoxDialog;
        Text show_txt_Text, show_txt_true, show_txt_false;
        Button show_btn_CloseBG, show_btn_True, show_btn_False;
        EAction show_actOk, show_actNo;

        Queue<PopupBox> m_que_BoxPopup;
        #endregion

        void ISingleton.Init()
        {
            m_target = new GameObject("UI").transform;
            m_target.SetParent(EF.Managers);

            #region New Camera and Canvas
            UICamera = new GameObject("_UICamera").AddComponent<Camera>();
            UICamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            UICamera.orthographic = true;
            UICamera.orthographicSize = UiScreenHeight / 2.0f;
            UICamera.farClipPlane = 200.0f;
            UICamera.cullingMask = 32;
            Camera.main.GetUniversalAdditionalCameraData().cameraStack.Add(UICamera);
            GameObject _root = new GameObject("_UIRoot");
            _root.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
            _root.GetComponent<Canvas>().worldCamera = UICamera;
            _root.layer = 5;
            CanvasScaler _cs = _root.AddComponent<CanvasScaler>();
            _cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _cs.referenceResolution = new Vector2(UiScreenWidth, UiScreenHeight);
            _cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            _root.AddComponent<GraphicRaycaster>();
            _root.transform.SetParent(m_target, false);
            UICamera.transform.SetParent(m_target, false);
            #endregion

            #region EventSystem
            GameObject _eventSystem = GameObject.Find("EventSystem");
            if (!_eventSystem)
            {
                _eventSystem = new GameObject("EventSystem");
                _eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                _eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                _eventSystem.transform.parent = m_target;
            }
            #endregion

            #region New page base object
            RectTransform _rect = new GameObject(pageBaseObjectName).AddComponent<RectTransform>();
            _rect.gameObject.AddComponent<CanvasRenderer>();
            pageBaseObject = _rect.transform;
            pageBaseObject.SetParent(_root.transform, false);
            pageBaseObject.localPosition = Vector3.zero;
            _rect.anchorMin = Vector3.zero;
            _rect.anchorMax = Vector3.one;
            _rect.sizeDelta = Vector2.zero;
            _rect.localScale = Vector2.one;
            #endregion

            showBoxBaseObject = new GameObject(showBoxBaseObjectName).transform;
            showBoxBaseObject.SetParent(_root.transform, false);


            m_stack_UI = new Stack<UIPageBase>();
            m_stack_GO = new Stack<GameObject>();
            m_dic_UI = new Dictionary<int, UIPageBase>();
            m_dic_GO = new Dictionary<int, GameObject>();

            m_ClickPS = Object.Instantiate(EF.Load.Load<ParticleSystem>("Prefabs/ClickEffect"));
            m_ClickPS.transform.SetParent(m_target, false);
        }

        void IUpdate.Update(float elapse, float realElapse)
        {
            if (Input.GetMouseButtonDown(0))
            {
                m_ClickPS.transform.transform.position = EF.Tool.ScreenPointToWorldPoint(Input.mousePosition, UICamera, 11f);
                m_ClickPS.Play();
            }
            PageUpdate();
            ShowBoxUpdate();
        }

        void ISingleton.Quit()
        {
            for (int i = 0; i < m_int_UiCount; i++)
            {
                m_stack_UI.Pop().Quit();
                Destroy(m_stack_GO.Pop());
            }
            m_stack_UI.Clear();
            m_stack_GO.Clear();
            m_stack_UI = null;
            m_stack_GO = null;

            if (null != m_que_BoxPopup)
            {
                while (0 != m_que_BoxPopup.Count)
                {
                    PopupBox _popup = m_que_BoxPopup.Dequeue();
                    Destroy(_popup.BoxObject);
                    _popup.OnDestory();
                }
                m_que_BoxPopup.Clear();
                m_que_BoxPopup = null;

                show_actOk = show_actNo = null;
                show_btn_CloseBG.onClick.RemoveAllListeners();
                show_btn_True.onClick.RemoveAllListeners();
                show_btn_False.onClick.RemoveAllListeners();
                show_btn_CloseBG = show_btn_True = show_btn_False = null;
                show_txt_Text = show_txt_false = show_txt_true = null;

                Destroy(BoxDialog);
                BoxDialog = null;
                Destroy(pageBaseObject.gameObject);
                Destroy(showBoxBaseObject.gameObject);
                pageBaseObject = showBoxBaseObject = null;
            }

            Destroy(m_ClickPS.gameObject);
            m_ClickPS = null;

            Camera.main.GetUniversalAdditionalCameraData().cameraStack.Remove(UICamera);
            Destroy(UICamera.gameObject);
            UICamera = null;
        }

        #region Update
        private void PageUpdate()
        {
            m_currentPage?.Update();
        }
        private void ShowBoxUpdate()
        {
            if (null != m_que_BoxPopup)
            {
                foreach (var popup in m_que_BoxPopup)
                {
                    popup.Update();
                }
            }
        }
        #endregion

        #region Old Private function
        void Destroy(Object obj)
        {
            Object.Destroy(obj);
        }
        private GameObject CreateUI(UIPageBase page)
        {
            GameObject uiObj = Object.Instantiate(EF.Load.Load<GameObject>($"{AppConst.UI}{page.GetType().Name}"));
            uiObj.transform.SetParent(pageBaseObject);
            RectTransform _rect = uiObj.GetComponent<RectTransform>();
            _rect.sizeDelta = Vector2.zero;
            _rect.localPosition = Vector2.zero;
            _rect.localScale = Vector2.one;
            m_stack_GO.Push(uiObj);
            uiObj.SetActive(true);
            return uiObj;
        }
        private void PushTo(UIPageBase page, bool hideCurrent, params object[] args)
        {
            if (null != m_currentPage)
            {
                m_currentPage.OnFocus(false, args);
                m_currentObj.SetActive(!hideCurrent);
            }
            m_currentObj = CreateUI(page);
            m_stack_UI.Push(page);
            m_currentPage = page;
            ++m_int_UiCount;
            m_currentPage.Awake(m_currentObj, args);
        }
        private void PopTo(params object[] args)
        {
            if (m_int_UiCount != 0)
            {
                --m_int_UiCount;
                m_currentPage.OnFocus(false, args);
                m_currentPage.Quit();
                m_stack_UI.Pop();
                Destroy(m_stack_GO.Pop());
            }
            m_currentPage = null;
            m_currentObj = null;
        }
        #endregion

        #region New Private function
        private GameObject Created(UIPageBase page)
        {
            GameObject _uiObj = Object.Instantiate(EF.Load.Load<GameObject>($"{AppConst.UI}{page.GetType().Name}"));
            _uiObj.transform.SetParent(pageBaseObject);
            RectTransform _rect = _uiObj.GetComponent<RectTransform>();
            _rect.sizeDelta = Vector2.zero;
            _rect.localPosition = Vector2.zero;
            _rect.localScale = Vector2.one;

            _uiObj.SetActive(true);

            return _uiObj;
        }
        private void Pushed(UIPageBase page, bool hideCurrent, params object[] args)
        {
            if (null != m_currentPage)
            {
                m_currentPage.OnFocus(false, args);
                m_currentObj.SetActive(!hideCurrent);
            }
            m_currentObj = CreateUI(page);
            m_currentPage = page;

            m_currentPage.Awake(m_currentObj, args);

            m_dic_GO.Add(m_int_SerialCount, m_currentObj);
            m_dic_UI.Add(m_int_SerialCount, m_currentPage);

            m_currentPage.Open(args);

            page.SerialId = m_int_SerialCount;

            ++m_int_SerialCount;
            ++m_int_UiCount;
        }
        private void Poped(params object[] args)
        {
            if (m_int_UiCount != 0)
            {
                --m_int_UiCount;
                m_currentPage.OnFocus(false, args);
                m_currentPage.Quit();
                m_stack_UI.Pop();
                Destroy(m_stack_GO.Pop());
            }
            m_currentPage = null;
            m_currentObj = null;
        }
        #endregion
    }
}

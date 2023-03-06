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
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace EasyFramework.Managers
{
    public partial class UIManager : Singleton<UIManager>, IManager, IUpdate
    {
        int IManager.ManagerLevel => 100;

        private readonly string pageBaseObjectName = "UIPages";
        private readonly string showBoxBaseObjectName = "UIShowBox";
        private Transform m_target, pageBaseObject, showBoxBaseObject;
        private ParticleSystem m_ClickPS;
        private GameObject m_Root;
        void ISingleton.Init()
        {
            m_target = new GameObject("UI").transform;
            m_target.SetParent(EF.Managers);

            //Create ui root object.
            {
                UICamera = new GameObject("_UICamera").AddComponent<Camera>();
                UICamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
                UICamera.orthographic = true;
                UICamera.orthographicSize = UiScreenHeight / 2.0f;
                UICamera.farClipPlane = 200.0f;
                UICamera.cullingMask = 32;
                Camera.main.GetUniversalAdditionalCameraData().cameraStack.Add(UICamera);
                m_Root = new GameObject("_UIRoot");
                m_Root.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
                m_Root.GetComponent<Canvas>().worldCamera = UICamera;
                m_Root.layer = 5;
                CanvasScaler _cs = m_Root.AddComponent<CanvasScaler>();
                _cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                _cs.referenceResolution = new Vector2(UiScreenWidth, UiScreenHeight);
                _cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                m_Root.AddComponent<GraphicRaycaster>();
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

            PageInit();
            ShowBoxInit();

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
            PageExit();
            ShowBoxQuit();

            Destroy(m_ClickPS.gameObject);
            m_ClickPS = null;

            Camera.main.GetUniversalAdditionalCameraData().cameraStack.Remove(UICamera);
            Destroy(UICamera.gameObject);
            UICamera = null;
        }

        void Destroy(Object obj)
        {
            Object.Destroy(obj);
        }

        #region Page
        int m_int_Serial;
        int m_int_PageCount;
        GameObject m_CurrentObj;
        UIPageBase m_CurrentPage;

        Stack<UIPageBase> m_stc_UseUI;
        Stack<GameObject> m_stc_UseGO;

        Dictionary<int, UIPageBase> m_dic_ReadyUI;
        Dictionary<int, GameObject> m_dic_ReadyGO;

        void PageInit()
        {
            RectTransform _rect = new GameObject(pageBaseObjectName).AddComponent<RectTransform>();
            _rect.gameObject.AddComponent<CanvasRenderer>();
            pageBaseObject = _rect.transform;
            pageBaseObject.SetParent(m_Root.transform, false);
            pageBaseObject.localPosition = Vector3.zero;
            _rect.anchorMin = Vector3.zero;
            _rect.anchorMax = Vector3.one;
            _rect.sizeDelta = Vector2.zero;
            _rect.localScale = Vector2.one;

            m_stc_UseUI = new Stack<UIPageBase>();
            m_stc_UseGO = new Stack<GameObject>();
            m_dic_ReadyUI = new Dictionary<int, UIPageBase>();
            m_dic_ReadyGO = new Dictionary<int, GameObject>();
        }
        void PageUpdate()
        {
            if (null != m_CurrentPage)
                m_CurrentPage?.Update();
        }
        void PageExit()
        {
            while (m_stc_UseUI.Count != 0)
            {
                m_stc_UseUI.Pop().Quit();
                Destroy(m_stc_UseGO.Pop());
            }

            int[] keyArr = m_dic_ReadyGO.Keys.ToArray();

            for (int i = 0; i < keyArr.Length; i++)
            {
                m_dic_ReadyUI[keyArr[i]].Quit();
                Destroy(m_dic_ReadyGO[keyArr[i]]);
                m_dic_ReadyUI.Remove(keyArr[i]);
                m_dic_ReadyGO.Remove(keyArr[i]);
            }

            m_dic_ReadyUI.Clear();
            m_dic_ReadyGO.Clear();
            m_dic_ReadyUI = null;
            m_dic_ReadyGO = null;

            m_stc_UseUI.Clear();
            m_stc_UseGO.Clear();
            m_stc_UseUI = null;
            m_stc_UseGO = null;
        }

        private GameObject PageCreated(UIPageBase page)
        {
            GameObject _uiObj = Object.Instantiate(EF.Load.Load<GameObject>($"{AppConst.UI}{page.GetType().Name}"));
            _uiObj.transform.SetParent(pageBaseObject);
            RectTransform _rect = _uiObj.GetComponent<RectTransform>();
            _rect.sizeDelta = Vector2.zero;
            _rect.localPosition = Vector2.zero;
            _rect.localScale = Vector2.one;
            _uiObj.SetActive(true);
            _uiObj.name = page.GetType().Name;
            page.SerialId = m_int_Serial++;
            return _uiObj;
        }
        private void PageOpen(UIPageBase page, bool hideCurrent, params object[] args)
        {
            if (null != m_CurrentPage)
            {
                m_CurrentPage.OnFocus(false);
                m_CurrentObj.SetActive(!hideCurrent);
            }

            int _readyKey = -1;
            foreach (KeyValuePair<int, GameObject> item in m_dic_ReadyGO)
            {
                if (item.Value.name == page.GetType().Name)
                {
                    _readyKey = item.Key;
                    break;
                }
            }

            if (_readyKey == -1)
            {
                m_CurrentPage = page;
                m_CurrentObj = PageCreated(page);
                m_CurrentPage.Awake(m_CurrentObj, args);
                m_CurrentPage.Open(args);
            }
            else
            {
                m_CurrentObj = m_dic_ReadyGO[_readyKey];
                m_CurrentPage = m_dic_ReadyUI[_readyKey];

                m_CurrentPage.Open(m_CurrentObj, args);
                m_CurrentObj.SetActive(true);

                m_dic_ReadyUI.Remove(_readyKey);
                m_dic_ReadyGO.Remove(_readyKey);
            }

            m_stc_UseGO.Push(m_CurrentObj);
            m_stc_UseUI.Push(m_CurrentPage);

            ++m_int_PageCount;
        }
        private void PageClose(bool destroy = false, params object[] args)
        {
            if (m_int_PageCount <= 0)
                return;

            UIPageBase _ui = m_stc_UseUI.Pop();
            GameObject _go = m_stc_UseGO.Pop();

            _ui.Close();
            if (destroy)
            {
                _ui.Quit();
                Destroy(_go);
            }
            else
            {
                _go.SetActive(false);
                m_dic_ReadyUI.Add(_ui.SerialId, _ui);
                m_dic_ReadyGO.Add(_ui.SerialId, _go);
            }

            m_CurrentPage = null;

            if (--m_int_PageCount > 0)
            {
                m_CurrentObj = m_stc_UseGO.Peek();
                m_CurrentPage = m_stc_UseUI.Peek();

                m_CurrentObj.SetActive(true);
                m_CurrentPage.OnFocus(true, args);
            }
        }
        #endregion

        #region Show box
        GameObject BoxDialog;
        Text show_txt_Text, show_txt_true, show_txt_false;
        Button show_btn_CloseBG, show_btn_True, show_btn_False;
        EAction show_actOk, show_actNo;

        Queue<PopupBox> m_que_BoxPopup;

        void ShowBoxInit()
        {
            showBoxBaseObject = new GameObject(showBoxBaseObjectName).transform;
            showBoxBaseObject.SetParent(m_Root.transform, false);

            BoxDialog = Object.Instantiate(EF.Load.Load<GameObject>(AppConst.UI + "Box_Dialog"), showBoxBaseObject);
            show_btn_CloseBG = BoxDialog.transform.Find("btn_CloseBG").GetComponent<Button>();
            show_txt_Text = BoxDialog.transform.Find("img_ShowBG/txt_Text").GetComponent<Text>();
            show_btn_True = BoxDialog.transform.Find("img_ShowBG/btn_True").GetComponent<Button>();
            show_btn_False = BoxDialog.transform.Find("img_ShowBG/btn_False").GetComponent<Button>();
            show_txt_true = show_btn_True.transform.Find("txt_true").GetComponent<Text>();
            show_txt_false = show_btn_False.transform.Find("txt_false").GetComponent<Text>();

            show_btn_CloseBG.onClick.RemoveAllListeners();
            show_btn_True.onClick.RemoveAllListeners();
            show_btn_False.onClick.RemoveAllListeners();
            show_btn_CloseBG.onClick.AddListener(delegate
            {
                BoxDialog.SetActive(false);
            });
            show_btn_True.onClick.AddListener(delegate
            {
                BoxDialog.SetActive(false);
                show_actOk?.Invoke();
            });
            show_btn_False.onClick.AddListener(delegate
            {
                BoxDialog.SetActive(false);
                show_actNo?.Invoke();
            });
            BoxDialog.SetActive(false);

            m_que_BoxPopup = new Queue<PopupBox>();
        }
        void ShowBoxUpdate()
        {
            if (null != m_que_BoxPopup)
            {
                foreach (var popup in m_que_BoxPopup)
                {
                    popup.Update();
                }
            }
        }
        void ShowBoxQuit()
        {
            while (0 != m_que_BoxPopup.Count)
            {
                PopupBox _popup = m_que_BoxPopup.Dequeue();
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
        #endregion

    }
}

/*
 * ================================================
 * Describe:        The class is ui page manager.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-01 14:33:01
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2022-10-18 15:59:29
 * Version:         1.0
 * ===============================================
 */
using EasyFramework.Framework.Core;
using EasyFramework.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using XHTools;

namespace EasyFramework.Managers
{
    public class UIManager : MonoSingleton<UIManager>, ISingleton, IManager
    {
        public readonly float UiScreenHeight = Screen.height;
        public readonly float UiScreenWidth = Screen.width;

        private readonly string pageBaseObjectName = "UIPages";
        private readonly string showBoxBaseObjectName = "UIShowBox";

        private Transform pageBaseObject, showBoxBaseObject;

        #region Page information
        private int m_int_UiCount;
        private ParticleSystem m_ClickPS;
        private UIPageBase m_currentPage;
        private GameObject m_currentObj;
        private Stack<UIPageBase> m_stack_UI;
        private Stack<GameObject> m_stack_GO;
        #endregion

        #region Dialog information
        GameObject BoxDialog;
        Text show_txt_Text, show_txt_true, show_txt_false;
        Button show_btn_CloseBG, show_btn_True, show_btn_False;
        EAction show_actOk, show_actNo;

        Queue<PopupBox> m_que_BoxPopup;
        #endregion

        public Camera UICamera { get; private set; }
        public int UiPageCount => m_int_UiCount;
        void ISingleton.Init()
        {
            GameObject _root = GameObject.Find("_UIRoot");
            if (!_root)
            {
                UICamera = new GameObject("_UICamera").AddComponent<Camera>();
                UICamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
                UICamera.orthographic = true;
                UICamera.orthographicSize = UiScreenHeight / 2.0f;
                UICamera.farClipPlane = 200.0f;
                Camera.main.GetUniversalAdditionalCameraData().cameraStack.Add(UICamera);
                _root = new GameObject("_UIRoot");
                _root.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
                _root.GetComponent<Canvas>().worldCamera = UICamera;
                CanvasScaler _cs = _root.AddComponent<CanvasScaler>();
                _cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                _cs.referenceResolution = new Vector2(UiScreenWidth, UiScreenHeight);
                _cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                _root.AddComponent<GraphicRaycaster>();
                _root.transform.SetParent(transform, false);
                UICamera.transform.SetParent(transform, false);
            }
            GameObject _eventSystem = GameObject.Find("EventSystem");
            if (!_eventSystem)
            {
                _eventSystem = new GameObject("EventSystem");
                _eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                _eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                _eventSystem.transform.parent = transform;
            }

            RectTransform _rect = new GameObject(pageBaseObjectName).AddComponent<RectTransform>();
            _rect.gameObject.AddComponent<CanvasRenderer>();
            pageBaseObject = _rect.transform;
            pageBaseObject.SetParent(_root.transform, false);
            pageBaseObject.localPosition = Vector3.zero;
            _rect.anchorMin = Vector3.zero;
            _rect.anchorMax = Vector3.one;
            _rect.sizeDelta = Vector2.zero;
            _rect.localScale = Vector2.one;

            showBoxBaseObject = new GameObject(showBoxBaseObjectName).transform;
            showBoxBaseObject.SetParent(_root.transform, false);


            m_stack_UI = new Stack<UIPageBase>();
            m_stack_GO = new Stack<GameObject>();

            m_ClickPS = Instantiate(EF.Load.Load<ParticleSystem>("Prefabs/ClickEffect"));
            m_ClickPS.transform.SetParent(transform, false);
        }

        private void Update()
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
            if (null != m_currentPage)
                m_currentPage.Update();
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

        #region Private function
        private GameObject CreateUI(UIPageBase page)
        {
            //string[] _names = page.GetType().ToString().Split('.');
            //GameObject uiObj = Instantiate(GM.Load.Load<GameObject>($"{AppConst.UI}{_names[_names.Length - 1]}"));
            GameObject uiObj = Instantiate(EF.Load.Load<GameObject>($"{AppConst.UI}{page.GetType().Name}"));
            uiObj.transform.SetParent(pageBaseObject);
            RectTransform _rect = uiObj.GetComponent<RectTransform>();
            _rect.sizeDelta = Vector2.zero;
            _rect.localPosition = Vector2.zero;
            _rect.localScale = Vector2.one;
            m_stack_GO.Push(uiObj);
            uiObj.SetActive(true);
            return uiObj;
        }
        private void PushTo(UIPageBase page, params object[] args)
        {
            if (null != m_currentPage)
            {
                m_currentPage.OnPause(false, args);
                m_currentObj.SetActive(false);
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
                m_currentPage.OnPause(false, args);
                m_currentPage.Quit();
                m_stack_UI.Pop();
                Destroy(m_stack_GO.Pop());
            }
        }
        #endregion

        #region My public UI function region
        #region Page region
        /// <summary>
        /// Push to next ui page.进入下个页面
        /// </summary>
        /// <param name="page">Next ui page should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.下个页面需要存在于相关路径下，并且名字要与类名相同</param>
        /// <param name="args">Send this params to current and next ui page.给当前页和要进入的页面 都传递该参数</param>
        public void Push(UIPageBase page, params object[] args)
        {
            if (page == m_currentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please use other Push function. ");
                return;
            }
            PushTo(page, args);
        }

        /// <summary>
        /// Push to next ui page.进入下个页面
        /// </summary>
        /// <param name="page">Next ui page should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.下个页面需要存在于相关路径下，并且名字要与类名相同</param>
        /// <param name="repeatCreated">If the page exists, repeat the creation.该页面如果存在，则重复创建</param>
        /// <param name="args">Send this params to current and next ui page.给当前页和要进入的页面 都传递该参数</param>
        public void Push(bool repeatCreated, UIPageBase page, params object[] args)
        {
            if (!repeatCreated && page == m_currentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please set params 'repeatCreated' to true. ");
                return;
            }
            PushTo(page, args);
        }

        /// <summary>
        /// Pop current ui page, and push next ui page.退出当前页面并进入下一页面
        /// </summary>
        /// <param name="page">Next ui page.should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.下个页面需要存在于相关路径下，并且名字要与类名相同</param>
        /// <param name="args">Send this params to current and next ui page.给当前页和要进入的页面 都传递该参数</param>
        public void PopAndPushTo(UIPageBase page, params object[] args)
        {
            if (page == m_currentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please set params 'repeatCreated' to true");
                return;
            }
            PopTo();
            PushTo(page, args);
        }

        /// <summary>
        /// Pop current ui page, and push next ui page.退出当前页面并进入下一页面
        /// </summary>
        /// <param name="page">Next ui page.should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.下个页面需要存在于相关路径下，并且名字要与类名相同</param>
        /// <param name="repeatCreated">If the page exists, repeat the creation.该页面如果存在，则重复创建</param>
        /// <param name="args">Send this params to current and next ui page.给当前页和要进入的页面 都传递该参数</param>
        public void PopAndPushTo(bool repeatCreated, UIPageBase page, params object[] args)
        {
            if (!repeatCreated && page == m_currentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please set params 'repeatCreated' to true");
                return;
            }
            PopTo();
            PushTo(page, args);
        }

        /// <summary>
        /// Pop current ui page.退出当前页面
        /// </summary>
        /// <param name="args"> Send this params to last ui page. 给前一个页面传递参数</param>
        public void Pop(params object[] args)
        {
            PopTo(args);
            m_currentPage = null;
            m_currentObj = null;

            if (m_int_UiCount == 0) return;
            m_currentPage = m_stack_UI.Peek();
            m_currentObj = m_stack_GO.Peek();
            m_currentPage.OnPause(true, args);
            m_currentObj.SetActive(true);
        }
        #endregion

        #region Show box region
        /// <summary>
        /// The dialog board.确认弹窗
        /// </summary>
        /// <param name="text">Show text info.展示内容</param>
        /// <param name="okEvent">The agreed action.确定按钮</param>
        /// <param name="noEvent">The cancel action.取消按钮</param>
        /// <param name="okBtnText">The agreed button text info.确认按钮内容</param>
        /// <param name="noBtnText">The cancel button text info.取消按钮内容</param>
        /// <param name="openCloseBG">Open the close backbround.开启背景可关闭</param>
        public void ShowDialog(string text, EAction okEvent = null, EAction noEvent = null, string okBtnText = "OK", string noBtnText = "Cancel", bool openCloseBG = true)
        {
            show_actOk = okEvent;
            show_actNo = noEvent;
            if (!BoxDialog)
            {
                BoxDialog = Instantiate(EF.Load.Load<GameObject>(AppConst.UI + "Box_Dialog"), pageBaseObject.parent);
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
            }

            show_txt_Text.text = text;
            show_txt_true.text = okBtnText;
            show_txt_false.text = noBtnText;
            show_btn_CloseBG.interactable = openCloseBG;

            BoxDialog.SetActive(true);
        }

        /// <summary>
        /// Show the popup info borad.展示弹窗面板
        /// </summary>
        /// <param name="text">The show info. 展示的信息</param>
        /// <param name="backgroundAlpha">The alpha with background.背景的透明度</param>
        /// <param name="textColor">The color with text. 文字颜色</param>
        /// <param name="backgroundColor">The color with background. 背景颜色</param>
        public void ShowPopup(string text, float backgroundAlpha = .85f, Color textColor = default, Color backgroundColor = default)
        {
            if (null == m_que_BoxPopup)
            {
                m_que_BoxPopup = new Queue<PopupBox>();
            }
            PopupBox _popup;
            if (5 == m_que_BoxPopup.Count)
            {
                _popup = m_que_BoxPopup.Dequeue();
            }
            else
            {
                _popup = new PopupBox(Instantiate(EF.Load.Load<Transform>(AppConst.UI + "Box_Popup"), showBoxBaseObject));
            }

            _popup.TextInfo = text;
            _popup.TextColor = textColor == default ? Color.white : textColor;
            _popup.BackgroundColor = backgroundColor == default ? Color.black : backgroundColor;
            _popup.Alpha = backgroundAlpha;
            m_que_BoxPopup.Enqueue(_popup);
        }
        #endregion
        #endregion
    }
}

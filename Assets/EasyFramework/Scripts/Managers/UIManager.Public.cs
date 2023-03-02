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

using EasyFramework.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XHTools;

namespace EasyFramework.Managers
{
    /// <summary>
    /// UI管理器
    /// </summary>
    public partial class UIManager : Singleton<UIManager>, IManager, IUpdate
    {
        public readonly float UiScreenHeight = Screen.height;
        public readonly float UiScreenWidth = Screen.width;

        /// <summary>
        /// UI相机
        /// </summary>
        public Camera UICamera { get; private set; }

        /// <summary>
        /// UI界面数量
        /// </summary>
        public int UiPageCount => m_int_UiCount;

        int IManager.ManagerLevel => 100;

        #region My public UI function region
        #region Page region
        /// <summary>
        /// Push to next ui page.进入下个页面
        /// </summary>
        /// <param name="page">Next ui page should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.下个页面需要存在于相关路径下，并且名字要与类名相同</param>
        /// <param name="args">Send this params to current and next ui page.给当前页和要进入的页面 都传递该参数</param>
        public void Push(UIPageBase page,  params object[] args)
        {
            if (page == m_currentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please use other Push function. ");
                return;
            }
            PushTo(page, true, args);
        }

        /// <summary>
        /// Push to next ui page.进入下个页面
        /// </summary>
        /// <param name="page">Next ui page should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.下个页面需要存在于相关路径下，并且名字要与类名相同</param>
        /// <param name="hideCurrent">Hide current ui page.隐藏当前界面.</param>
        /// <param name="args">Send this params to current and next ui page.给当前页和要进入的页面 都传递该参数</param>
        public void Push(UIPageBase page, bool hideCurrent, params object[] args)
        {
            if (page == m_currentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please use other Push function. ");
                return;
            }
            PushTo(page, hideCurrent, args);
        }
        
        /// <summary>
        /// Push to next ui page.进入下个页面
        /// </summary>
        /// <param name="repeatCreated">If the page exists, repeat the creation.该页面如果存在，则重复创建</param>
        /// <param name="page">Next ui page should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.下个页面需要存在于相关路径下，并且名字要与类名相同</param>
        /// <param name="args">Send this params to current and next ui page.给当前页和要进入的页面 都传递该参数</param>
        public void Push(bool repeatCreated, UIPageBase page, params object[] args)
        {
            if (!repeatCreated && page == m_currentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please set params 'repeatCreated' to true. ");
                return;
            }
            PushTo(page, true, args);
        }

        /// <summary>
        /// Push to next ui page.进入下个页面
        /// </summary>
        /// <param name="repeatCreated">If the page exists, repeat the creation.该页面如果存在，则重复创建</param>
        /// <param name="page">Next ui page should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.下个页面需要存在于相关路径下，并且名字要与类名相同</param>
        /// <param name="hideCurrent">Hide current ui page.隐藏当前界面.</param>
        /// <param name="args">Send this params to current and next ui page.给当前页和要进入的页面 都传递该参数</param>
        public void Push(bool repeatCreated, UIPageBase page, bool hideCurrent, params object[] args)
        {
            if (!repeatCreated && page == m_currentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please set params 'repeatCreated' to true. ");
                return;
            }
            PushTo(page, hideCurrent, args);
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
            PushTo(page, true, args);
        }

        /// <summary>
        /// Pop current ui page, and push next ui page.退出当前页面并进入下一页面
        /// </summary>
        /// <param name="page">Next ui page.should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.下个页面需要存在于相关路径下，并且名字要与类名相同</param>
        /// <param name="hideCurrent">Hide current ui page.隐藏当前界面.</param>
        /// <param name="args">Send this params to current and next ui page.给当前页和要进入的页面 都传递该参数</param>
        public void PopAndPushTo(UIPageBase page, bool hideCurrent, params object[] args)
        {
            if (page == m_currentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please set params 'repeatCreated' to true");
                return;
            }
            PopTo();
            PushTo(page, hideCurrent, args);
        }

        /// <summary>
        /// Pop current ui page, and push next ui page.退出当前页面并进入下一页面
        /// </summary>
        /// <param name="repeatCreated">If the page exists, repeat the creation.该页面如果存在，则重复创建</param>
        /// <param name="page">Next ui page.should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.下个页面需要存在于相关路径下，并且名字要与类名相同</param>
        /// <param name="args">Send this params to current and next ui page.给当前页和要进入的页面 都传递该参数</param>
        public void PopAndPushTo(bool repeatCreated, UIPageBase page, params object[] args)
        {
            if (!repeatCreated && page == m_currentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please set params 'repeatCreated' to true");
                return;
            }
            PopTo();
            PushTo(page, true, args);
        }

        /// <summary>
        /// Pop current ui page, and push next ui page.退出当前页面并进入下一页面
        /// </summary>
        /// <param name="repeatCreated">If the page exists, repeat the creation.该页面如果存在，则重复创建</param>
        /// <param name="page">Next ui page.should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.下个页面需要存在于相关路径下，并且名字要与类名相同</param>
        /// <param name="hideCurrent">Hide current ui page.隐藏当前界面.</param>
        /// <param name="args">Send this params to current and next ui page.给当前页和要进入的页面 都传递该参数</param>
        public void PopAndPushTo(bool repeatCreated, UIPageBase page, bool hideCurrent, params object[] args)
        {
            if (!repeatCreated && page == m_currentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please set params 'repeatCreated' to true");
                return;
            }
            PopTo();
            PushTo(page, hideCurrent, args);
        }

        /// <summary>
        /// Pop current ui page.退出当前页面
        /// </summary>
        /// <param name="args"> Send this params to last ui page. 给前一个页面传递参数</param>
        public void Pop(params object[] args)
        {
            PopTo(args);

            if (m_int_UiCount == 0) return;
            m_currentPage = m_stack_UI.Peek();
            m_currentObj = m_stack_GO.Peek();
            m_currentPage.OnFocus(true, args);
            m_currentObj.SetActive(true);
        }

        /// <summary>
        /// Pop current ui page.退出当前页面
        /// </summary>
        /// <param name="count">Number of consecutively quit ui page.连续退出页面的数量</param>
        /// <param name="args"> Pass parameters to the page you are going to jump to. 给即将跳转到的页面传递参数</param>
        public void Pop(int count, params object[] args)
        {
            do
            {
                PopTo();

                if (m_int_UiCount == 0) return;
                m_currentPage = m_stack_UI.Peek();
                m_currentObj = m_stack_GO.Peek();
                if (count == 1)
                {
                    m_currentPage.OnFocus(true, args);
                    m_currentObj.SetActive(true);
                }
            } while (--count > 0);
        }

        /// <summary>
        /// Pop to only has one page.退出到只剩一个界面
        /// </summary>
        /// <param name="args"> Send this params to home ui page. 给主页面传递参数</param>
        public void PopToOnlyOnePage(params object[] args)
        {
            while (m_int_UiCount > 2)
            {
                Pop();
            }

            Pop(args);
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
                BoxDialog = Object.Instantiate(EF.Load.Load<GameObject>(AppConst.UI + "Box_Dialog"), pageBaseObject.parent);
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
                _popup = new PopupBox(Object.Instantiate(EF.Load.Load<Transform>(AppConst.UI + "Box_Popup"), showBoxBaseObject));
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

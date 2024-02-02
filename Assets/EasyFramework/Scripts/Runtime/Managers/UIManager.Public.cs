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
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyFramework.Managers
{
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
        public int UiPageCount => m_int_PageCount;

        /// <summary>
        /// The popups count.弹出窗口的最大数量
        /// </summary>
        public int PopupsMaxCount { get; set; } = 5;

        /// <summary>
        /// Open the mouse on click effect. 开启点击特效
        /// </summary>
        public bool OpenClickEffect { get; set; } = true;

        #region Page region
        #region Push
        /// <summary>
        /// Push to next ui page.进入下个页面
        /// </summary>
        /// <param name="page">Next ui page should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.下个页面需要存在于相关路径下，并且名字要与类名相同</param>
        /// <param name="args">Send this params to next ui page.给要进入的页面 传递该参数</param>
        public UIPageBase Push(UIPageBase page,  params object[] args)
        {
            if (page == m_CurrentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please use other Push function. ");
                return null;
            }
            return PageOpen(page, true, args);
        }

        /// <summary>
        /// Push to next ui page.进入下个页面
        /// </summary>
        /// <param name="page">Next ui page should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.下个页面需要存在于相关路径下，并且名字要与类名相同</param>
        /// <param name="hideCurrent">Hide current ui page.隐藏当前界面.</param>
        /// <param name="args">Send this params to next ui page.给要进入的页面 传递该参数</param>
        public UIPageBase Push(UIPageBase page, bool hideCurrent, params object[] args)
        {
            if (page == m_CurrentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please use other Push function. ");
                return null;
            }
            return PageOpen(page, hideCurrent, args);
        }
        
        /// <summary>
        /// Push to next ui page.进入下个页面
        /// </summary>
        /// <param name="repeatCreated">If the page exists, repeat the creation.该页面如果存在，则重复创建</param>
        /// <param name="page">Next ui page should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.下个页面需要存在于相关路径下，并且名字要与类名相同</param>
        /// <param name="args">Send this params to next ui page.给要进入的页面 传递该参数</param>
        public UIPageBase Push(bool repeatCreated, UIPageBase page, params object[] args)
        {
            if (!repeatCreated && page == m_CurrentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please set params 'repeatCreated' to true. ");
                return null;
            }
            return PageOpen(page, true, args);
        }

        /// <summary>
        /// Push to next ui page.进入下个页面
        /// </summary>
        /// <param name="repeatCreated">If the page exists, repeat the creation.该页面如果存在，则重复创建</param>
        /// <param name="page">Next ui page should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.下个页面需要存在于相关路径下，并且名字要与类名相同</param>
        /// <param name="hideCurrent">Hide current ui page.隐藏当前界面.</param>
        /// <param name="args">Send this params to next ui page.给要进入的页面 传递该参数</param>
        public UIPageBase Push(bool repeatCreated, UIPageBase page, bool hideCurrent, params object[] args)
        {
            if (!repeatCreated && page == m_CurrentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please set params 'repeatCreated' to true. ");
                return null;
            }
            return PageOpen(page, hideCurrent, args);
        }
        #endregion

        #region PopAndPushTo
        /// <summary>
        /// Pop current ui page, and push next ui page.退出当前页面并进入下一页面
        /// </summary>
        /// <param name="page">Next ui page.should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.下个页面需要存在于相关路径下，并且名字要与类名相同</param>
        /// <param name="args">Send this params to next ui page.给要进入的页面 传递该参数</param>
        public UIPageBase PopAndPushTo(UIPageBase page, params object[] args)
        {
            if (page == m_CurrentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please set params 'repeatCreated' to true");
                return null;
            }
            PageClose();
            return PageOpen(page, true, args);
        }

        /// <summary>
        /// Pop current ui page, and push next ui page.退出当前页面并进入下一页面
        /// </summary>
        /// <param name="page">Next ui page.should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.下个页面需要存在于相关路径下，并且名字要与类名相同</param>
        /// <param name="hideCurrent">Hide current ui page.隐藏当前界面.</param>
        /// <param name="args">Send this params to next ui page.给要进入的页面 传递该参数</param>
        public UIPageBase PopAndPushTo(UIPageBase page, bool hideCurrent, params object[] args)
        {
            if (page == m_CurrentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please set params 'repeatCreated' to true");
                return null;
            }
            PageClose();
            return PageOpen(page, hideCurrent, args);
        }

        /// <summary>
        /// Pop current ui page, and push next ui page.退出当前页面并进入下一页面
        /// </summary>
        /// <param name="repeatCreated">If the page exists, repeat the creation.该页面如果存在，则重复创建</param>
        /// <param name="page">Next ui page.should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.下个页面需要存在于相关路径下，并且名字要与类名相同</param>
        /// <param name="args">Send this params to next ui page.给要进入的页面 传递该参数</param>
        public UIPageBase PopAndPushTo(bool repeatCreated, UIPageBase page, params object[] args)
        {
            if (!repeatCreated && page == m_CurrentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please set params 'repeatCreated' to true");
                return null;
            }
            PageClose();
            return PageOpen(page, true, args);
        }

        /// <summary>
        /// Pop current ui page, and push next ui page.退出当前页面并进入下一页面
        /// </summary>
        /// <param name="repeatCreated">If the page exists, repeat the creation.该页面如果存在，则重复创建</param>
        /// <param name="page">Next ui page.should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.下个页面需要存在于相关路径下，并且名字要与类名相同</param>
        /// <param name="destroy">Destroy current ui page.销毁当前界面.</param>
        /// <param name="hideCurrent">Hide current ui page.隐藏当前界面.</param>
        /// <param name="args">Send this params to next ui page.给要进入的页面 传递该参数</param>
        public UIPageBase PopAndPushTo(bool repeatCreated, UIPageBase page, bool destroy, bool hideCurrent, params object[] args)
        {
            if (!repeatCreated && page == m_CurrentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please set params 'repeatCreated' to true");
                return null;
            }
            PageClose(destroy);
            return PageOpen(page, hideCurrent, args);
        }
        #endregion

        #region Pop
        /// <summary>
        /// Pop current ui page.退出当前页面
        /// </summary>
        /// <param name="args"> Send this params to last ui page. 给前一个页面传递参数</param>
        public void Pop(params object[] args)
        {
            PageClose(false, args);
        }

        /// <summary>
        /// Pop current ui page.退出当前页面
        /// </summary>
        /// <param name="destroy">Destroy current ui page.销毁当前界面.</param>
        /// <param name="args"> Send this params to last ui page. 给前一个页面传递参数</param>
        public void Pop(bool destroy, params object[] args)
        {
            PageClose(destroy, args);
        }

        /// <summary>
        /// Pop current ui page.退出当前页面
        /// </summary>
        /// <param name="count">Number of consecutively quit ui page.连续退出页面的数量</param>
        /// <param name="destroy">Destroy current ui page.销毁当前界面.</param>
        /// <param name="args"> Pass parameters to the page you are going to jump to. 给即将跳转到的页面传递参数</param>
        public void Pop(int count, bool destroy = false, params object[] args)
        {
            do
            {
                PageClose(destroy, args);

            } while (--count > 0);
        }

        /// <summary>
        /// Pop to only has one page.退出到只剩一个界面
        /// </summary>
        /// <param name="destroy">Destroy current ui page.销毁当前界面.</param>
        /// <param name="args"> Send this params to home ui page. 给主页面传递参数</param>
        public void PopToOnlyOne(bool destroy = false, params object[] args)
        {
            while (m_int_PageCount > 2)
            {
                PageClose(destroy);
            }

            PageClose(destroy, args);
        }
        #endregion
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
        /// <param name="openCloseBG">PageOpen the close backbround.开启背景可关闭</param>
        public void ShowDialog(string text, Action okEvent = null, Action noEvent = null, string okBtnText = "OK", string noBtnText = "Cancel", bool openCloseBG = true)
        {
            show_actOk = okEvent;
            show_actNo = noEvent;

            show_txt_Text.text = text;
            show_txt_true.text = okBtnText;
            show_txt_false.text = noBtnText;
            show_btn_CloseBG.interactable = openCloseBG;

            BoxDialog.transform.localScale = Vector3.one;
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
            PopupBox _popup;
            if (PopupsMaxCount == m_que_BoxPopup.Count)
            {
                _popup = m_que_BoxPopup.Dequeue();
            }
            else
            {
                _popup = new PopupBox(Object.Instantiate(EF.Load.LoadInResources<Transform>(EF.Projects.AppConst.UIPath + "Box_Popup"), showBoxBaseObject));
            }

            _popup.TextInfo = text;
            _popup.TextColor = textColor == default ? Color.white : textColor;
            _popup.BackgroundColor = backgroundColor == default ? Color.black : backgroundColor;
            _popup.Alpha = backgroundAlpha;
            m_que_BoxPopup.Enqueue(_popup);
        }
        #endregion
    }
}

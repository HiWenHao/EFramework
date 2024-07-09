/*
 * ================================================
 * Describe:        The class is ui page m_managerLevel.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-01 14:33:01
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2024-07-09 15:39:05
 * Version:         1.0
 * ===============================================
 */

using EasyFramework.UI;
using UnityEngine;

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
        /// Number of UI page
        /// <para>UI界面数量</para>
        /// </summary>
        public int UiPageCount => m_PageCount;

        /// <summary>
        /// The popups count.<para>弹出窗口的最大数量</para>
        /// </summary>
        public int PopupsMaxCount { get; set; } = 5;

        #region Push
        /// <summary>
        /// Push to next ui page.
        /// <para>进入下个页面</para>
        /// </summary>
        /// <param name="page">Next ui page should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.
        /// <para>下个页面需要存在于相关路径下，并且名字要与类名相同</para></param>
        /// <param name="args">Send this params to next ui page.<para>给要进入的页面 传递该参数</para></param>
        public UIPageBase Push(UIPageBase page, params object[] args)
        {
            if (page == m_CurrentPage)
            {
                D.Error("This is already the page, please dont enter again. If you need repeat enter, please use other Push function. ");
                return null;
            }
            return PageOpen(page, true, args);
        }

        /// <summary>
        /// Push to next ui page.
        /// <para>进入下个页面</para>
        /// </summary>
        /// <param name="page">Next ui page should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.
        /// <para>下个页面需要存在于相关路径下，并且名字要与类名相同</para></param>
        /// <param name="hideCurrent">Hide current ui page.<para>隐藏当前界面</para>.</param>
        /// <param name="args">Send this params to next ui page.<para>给要进入的页面 传递该参数</para></param>
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
        /// Push to next ui page.
        /// <para>进入下个页面</para>
        /// </summary>
        /// <param name="repeatCreated">If the page exists, repeat the creation.<para>该页面如果存在，则重复创建</para></param>
        /// <param name="page">Next ui page should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.
        /// <para>下个页面需要存在于相关路径下，并且名字要与类名相同</para></param>
        /// <param name="args">Send this params to next ui page.<para>给要进入的页面 传递该参数</para></param>
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
        /// Push to next ui page.
        /// <para>进入下个页面</para>
        /// </summary>
        /// <param name="repeatCreated">If the page exists, repeat the creation.<para>该页面如果存在，则重复创建</para></param>
        /// <param name="page">Next ui page should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.
        /// <para>下个页面需要存在于相关路径下，并且名字要与类名相同</para></param>
        /// <param name="hideCurrent">Hide current ui page.<para>隐藏当前界面</para>.</param>
        /// <param name="args">Send this params to next ui page.<para>给要进入的页面 传递该参数</para></param>
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
        /// Pop current ui page, and push next ui page.
        /// <para>退出当前页面并进入下一页面</para>
        /// </summary>
        /// <param name="page">Next ui page.should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.
        /// <para>下个页面需要存在于相关路径下，并且名字要与类名相同</para></param>
        /// <param name="args">Send this params to next ui page.<para>给要进入的页面 传递该参数</para></param>
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
        /// Pop current ui page, and push next ui page.
        /// <para>退出当前页面并进入下一页面</para>
        /// </summary>
        /// <param name="page">Next ui page.should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.
        /// <para>下个页面需要存在于相关路径下，并且名字要与类名相同</para></param>
        /// <param name="hideCurrent">Hide current ui page.<para>隐藏当前界面</para>.</param>
        /// <param name="args">Send this params to next ui page.<para>给要进入的页面 传递该参数</para></param>
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
        /// Pop current ui page, and push next ui page.
        /// <para>退出当前页面并进入下一页面</para>
        /// </summary>
        /// <param name="repeatCreated">If the page exists, repeat the creation.<para>该页面如果存在，则重复创建</para></param>
        /// <param name="page">Next ui page.should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.
        /// <para>下个页面需要存在于相关路径下，并且名字要与类名相同</para></param>
        /// <param name="args">Send this params to next ui page.<para>给要进入的页面 传递该参数</para></param>
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
        /// Pop current ui page, and push next ui page.
        /// <para>退出当前页面并进入下一页面</para>
        /// </summary>
        /// <param name="repeatCreated">If the page exists, repeat the creation.<para>该页面如果存在，则重复创建</para></param>
        /// <param name="page">Next ui page.should be exist in Assets\Resources\Prefabs\UI path,and be named the class name.
        /// <para>下个页面需要存在于相关路径下，并且名字要与类名相同</para></param>
        /// <param name="destroy">Destroy current ui page.<para>销毁当前界面</para></param>
        /// <param name="hideCurrent">Hide current ui page.<para>隐藏当前界面.</para></param>
        /// <param name="args">Send this params to next ui page.<para>给要进入的页面 传递该参数</para></param>
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
        /// Pop current ui page.
        /// <para>退出当前页面</para>
        /// </summary>
        /// <param name="args"> Send this params to last ui page. <para>给前一个页面传递参数</para></param>
        public void Pop(params object[] args)
        {
            PageClose(false, args);
        }

        /// <summary>
        /// Pop current ui page.
        /// <para>退出当前页面</para>
        /// </summary>
        /// <param name="destroy">Destroy current ui page.<para>销毁当前界面</para></param>
        /// <param name="args"> Send this params to last ui page.  <para>给前一个页面传递参数</para></param>
        public void Pop(bool destroy, params object[] args)
        {
            PageClose(destroy, args);
        }

        /// <summary>
        /// Pop current ui page.
        /// <para>退出当前页面</para>
        /// </summary>
        /// <param name="count">Number of consecutively quit ui page.<para>连续退出页面的数量</para></param>
        /// <param name="destroy">Destroy current ui page.<para>销毁当前界面.</para></param>
        /// <param name="args"> Pass parameters to the page you are going to jump to. <para>给即将跳转到的页面传递参数</para></param>
        public void Pop(int count, bool destroy = false, params object[] args)
        {
            do
            {
                PageClose(destroy, args);

            } while (--count > 0);
        }

        /// <summary>
        /// Pop to only has one page.
        /// <para>退出到只剩一个界面</para>
        /// </summary>
        /// <param name="destroy">Destroy current ui page.<para>销毁当前界面.</para></param>
        /// <param name="args"> Send this params to home ui page. <para>给主页面传递参数</para></param>
        public void PopToOnlyOne(bool destroy = false, params object[] args)
        {
            while (m_PageCount > 2)
            {
                PageClose(destroy);
            }

            PageClose(destroy, args);
        }
        #endregion
    }
}

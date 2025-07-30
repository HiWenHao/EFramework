/* 
 * ================================================
 * Describe:      This script is used to extension button event. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-11-17 18:06:29
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-11-17 18:06:29
 * ScriptVersion: 0.1
 * ===============================================
*/
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EasyFramework.UI
{
    /// <summary>
    /// Extension button event.扩展按钮事件
    /// </summary>
    public static class ExtensionEventButton
	{
        #region Button
        /// <summary>
        /// Register inL list and bind event.向列表注册自身，并且绑定事件
        /// </summary>
        /// <param name="button">按钮</param>
        /// <param name="events">事件</param>
        /// <param name="list">Button list manager. 按钮列表</param>
        /// <returns></returns>
        public static Button RegisterInListAndBindEvent(this Button button, UnityAction events, ref List<Button> list)
		{
			if (null == list)
            {
                list = new List<Button>();
            }
            button.onClick.AddListener(events);
			list.Add(button);
			return button;
        }




        #endregion

        #region List<Button>
        /// <summary>
        /// Releases all button events and removes buttons one by one from the list.释放所有按钮事件并且从列表中逐一删除按钮
        /// </summary>
        /// <param name="buttons">Button list. 按钮列表</param>
        public static List<Button> ReleaseAndRemoveEvent(this List<Button> buttons)
        {
            if (null == buttons)
                return null;

            int tempLength = buttons.Count;
            
            while (--tempLength >= 0)
                buttons[tempLength].onClick.RemoveAllListeners();

            buttons.Clear();
            return buttons;
        }





        #endregion

        #region ButtonPro
        /// <summary>
        /// Register inL list and bind event.向列表注册自身，并且绑定事件
        /// </summary>
        /// <param name="button">按钮</param>
        /// <param name="events">事件</param>
        /// <param name="list">Button list manager. 按钮列表</param>
        /// <returns></returns>
        public static ButtonPro RegisterInListAndBindEvent(this ButtonPro button, UnityAction events, ref List<ButtonPro> list)
        {
            if (null == list)
            {
                list = new List<ButtonPro>();
            }
            button.OnClickLeft.AddListener(events);
            list.Add(button);
            return button;
        }




        #endregion

        #region List<ButtonPro>
        /// <summary>
        /// Releases all button events and removes buttons one by one from the list.释放所有按钮事件并且从列表中逐一删除按钮
        /// </summary>
        /// <param name="buttons">Button list. 按钮列表</param>
        public static List<ButtonPro> ReleaseAndRemoveEvent(this List<ButtonPro> buttons)
        {
            if (null == buttons)
                return null;

            int tempLength = buttons.Count;

            while (--tempLength >= 0)
                buttons[tempLength].OnClickLeft.RemoveAllListeners();

            buttons.Clear();
            return buttons;
        }





        #endregion
    }
}

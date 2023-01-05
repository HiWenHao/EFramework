/*
 * ================================================
 * Describe:        The class is ui page base class.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-14:33:01
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2022-05-14:33:01
 * Version:         1.0
 * ===============================================
 */
using UnityEngine;

namespace EasyFramework.UI
{
    public abstract class UIPageBase
    {
        /// <summary>
        /// Initialize this page in created.在当前页面被创建时调用
        /// </summary>
        /// <param name="obj">The GameObject with current page. 这个页面的游戏物体.</param>
        /// <param name="args">Send this params to current ui page.给这个页面传递的参数.</param>
        public abstract void Awake(GameObject obj, params object[] args);

        /// <summary>
        /// 轮询刷新
        /// </summary>
        public virtual void Update() { }

        /// <summary>
        /// Called when on non-first entering or leaving the page.在非首次进入或离开该页面时调用.
        /// </summary>
        /// <param name="enable">When enter current page, this value is true.当进入该页面时，为真.</param>
        /// <param name="args">When last page quit and pass the args, the args is poosible have value.当上一个页面退出并传递参数时，参数才可能有值.</param>
        public virtual void OnPause(bool enable, params object[] args) { }

        /// <summary>
        /// On quit current ui page.退出当前页面时被调用
        /// </summary>
        public abstract void Quit();
    }
}

/*
 * ================================================
 * Describe:        The class is ui page base class.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-01 14:33:01
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2024-07-08 17:06:55
 * Version:         2.0
 * ===============================================
 */
using UnityEngine;

namespace EasyFramework.UI
{
    /// <summary>
    /// UI界面接口
    /// </summary>
    public abstract class UIPageBase
    {
        private bool m_Focus = true;

        /// <summary>
        /// Page serial number, do not change.
        /// <para>页面序列号,请勿改动</para>
        /// </summary>
        public int SerialId { get; set; }

        /// <summary>
        /// Focus or not.
        /// <para>是否被聚焦</para>
        /// </summary>
        public bool IsFocus => m_Focus;

        /// <summary>
        /// Initialize this page in created.
        /// <para>在当前页面被创建时调用</para>
        /// </summary>
        /// <param name="obj">The GameObject with current page. <para>这个页面的游戏物体</para></param>
        /// <param name="args">Send this params to current ui page.<para>给这个页面传递的参数</para></param>
        public abstract void Awake(GameObject obj, params object[] args);

        /// <summary>
        /// On open current ui page.
        /// <para>当打开当前UI页面时</para>
        /// </summary>
        /// <param name="args">Send this params to current ui page.<para>给这个页面传递的参数</para></param>
        public virtual void Open(params object[] args) { }

        /// <summary>
        /// 轮询刷新
        /// </summary>
        public virtual void Update(float elapse, float realElapse) { }

        /// <summary>
        /// Called when on non-first entering or leaving the page.
        /// <para>在非首次进入或离开该页面时调用.</para>
        /// </summary>
        /// <param name="focus">When enter current page, this value is true.<para>当进入该页面时，为真.</para></param>
        /// <param name="args">When last page quit and pass the args, the args is poosible have value.
        /// <para>当上一个页面退出并传递参数时，参数才可能有值.</para></param>
        public virtual void OnFocus(bool focus, params object[] args) 
        {
            m_Focus = focus;
        }

        /// <summary>
        /// On close current ui page.
        /// <para>关闭当前页面时被调用</para>
        /// </summary>
        public virtual void Close() { }

        /// <summary>
        /// On quit current ui page.
        /// <para>退出当前页面时被调用</para>
        /// </summary>
        public abstract void Quit();
    }
}

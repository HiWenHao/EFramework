/*
 * ================================================
 * Describe:      所有UI视窗接口, 给管理器函数增加约束.
 * Author:        Alvin8412
 * CreationTime:  2026-04-03 23:04:18
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-03 23:04:18
 * ScriptVersion: 0.1
 * ===============================================
 */

using UnityEngine;

namespace EasyFramework.Manager.UI
{
    /// <summary>
    /// 所有UI视窗接口
    /// </summary>
    public interface IUiView
    {
        /// <summary>
        /// 当前页面对象
        /// </summary>
        public RectTransform View { get; }

        /// <summary>
        /// 当前视窗类型
        /// </summary>
        public UIViewType ViewType { get; }

        /// <summary>
        /// 自动销毁
        /// </summary>
        protected internal bool AutoDestroy { get; }
        
        /// <summary>
        /// view serial number, do not change.
        /// <para>页面序列号,请勿改动</para>
        /// </summary>
        protected internal uint SerialId { get; set; }

        /// <summary>
        /// Initialize this page in created.
        /// <para>在当前页面被创建时调用</para>
        /// </summary>
        protected internal void Awake();

        /// <summary>
        /// When the view is activated.
        /// <para>当页面被激活时</para>
        /// </summary>
        /// <param name="args">Send this params to current ui page.<para>给这个页面传递的参数</para></param>
        protected internal void Enable(params object[] args)
        {
        }

        /// <summary>
        /// 轮询更新
        /// </summary>
        /// <param name="elapse">The interval in seconds from the last frame to the current one.
        /// <para>逻辑流逝时间，以秒为单位</para>
        /// </param>
        /// <param name="realElapse">The timeScale-independent interval in seconds from the last frame to the current one.
        /// <para>真实流逝时间，以秒为单位</para>
        /// </param>
        protected internal void Update(float elapse, float realElapse)
        {
        }

        /// <summary>
        /// When the view is disabled.
        /// <para>当页面被禁用时</para>
        /// </summary>
        /// <param name="args">Send this params to closed ui page.<para>给要被关闭的页面传递的参数</para></param>
        protected internal void DisEnable(params object[] args)
        {
        }

        /// <summary>
        /// On quit current ui page.
        /// <para>退出当前页面时被调用</para>
        /// </summary>
        protected internal void Quit();

        /// <summary>
        /// 内部调用，用来处理UI组件绑定关系，在Awake函数前
        /// </summary>
        /// <param name="uiViewRect">UI对象</param>
        protected internal void Bind(RectTransform uiViewRect);

        /// <summary>
        /// 内部调用，释放该UI视窗，在Quit函数后
        /// </summary>
        protected internal void Dispose();
    }
}
/*
 * ================================================
 * Describe:      提示窗口附加参数.
 * Author:        Alvin5100
 * CreationTime:  2026-04-03 15:05:30
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-03 15:05:30
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.UI.Tips
{
    /// <summary>
    /// 提示窗附加数据
    /// </summary>
    public struct TipsViewExtraData : IDisposable
    {
        /// <summary>
        /// 确定按钮换名字
        /// </summary>
        public string ConfirmName;

        /// <summary>
        /// 确定方法
        /// </summary>
        public Action ConfirmCallBack;

        /// <summary>
        /// 取消按钮换名字
        /// </summary>
        public string CancelName;

        /// <summary>
        /// 取消方法
        /// </summary>
        public Action CancelCallBack;

        /// <summary>
        /// 关闭方法
        /// </summary>
        public Action CloseCallBack;

        public void Dispose()
        {
            CancelName = string.Empty;
            ConfirmName = string.Empty;
            CloseCallBack = null;
            CancelCallBack = null;
            ConfirmCallBack = null;
        }
    }
}
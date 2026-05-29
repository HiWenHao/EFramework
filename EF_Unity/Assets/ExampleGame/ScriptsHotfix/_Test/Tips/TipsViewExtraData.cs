/*
 * ================================================
 * Describe:      TipsView 附加数据 — 按钮文字与回调配置
 * ===============================================
 */

using System;

namespace EFExample.UI.Tips
{
    /// <summary>
    /// 提示窗附加数据参考
    /// 开发者可根据自己需求扩展，例如增加 Checkbox、输入框等
    /// </summary>
    public class TipsViewExtraData
    {
        public string ConfirmName;        // 确认按钮文字（默认"确认"）
        public Action ConfirmCallBack;    // 确认回调（为空则不显示按钮）
        public string CancelName;         // 取消按钮文字（默认"取消"）
        public Action CancelCallBack;     // 取消回调（为空则不显示按钮）
        public Action CloseCallBack;      // 关闭回调
    }
}

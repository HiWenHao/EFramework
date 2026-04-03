/*
 * ================================================
 * Describe:      提示窗接口类 作为约束的存在.
 * Author:        Alvin5100
 * CreationTime:  2026-04-03 13:55:13
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-03 13:55:13
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework.UI.Tips
{
    /// <summary>
    /// 提示窗接口
    /// </summary>
    internal interface ITipsView
    {

        public void Show(string displayContents, TipsViewExtraData viewExtraData);

        #region Callback - 回调

        public void Confirm();

        public void Cancel();

        public void Close();

        #endregion
    }
}
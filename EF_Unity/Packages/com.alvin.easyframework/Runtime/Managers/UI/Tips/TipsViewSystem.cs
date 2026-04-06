/*
 * ================================================
 * Describe:      提示窗
 * Author:        Alvin5100
 * CreationTime:  2026-04-03 15:10:49
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-03 15:10:49
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework.Manager.UI.Tips
{
    /// <summary>
    /// 提示窗基类
    /// </summary>
    public partial class TipsView
    {
        void IUiView.Awake()
        {
        }

        void IUiView.Enable(params object[] args)
        {
            _tipsExtraData.Dispose();
            _tipsExtraData = (TipsViewExtraData)args[1];

            Txt_Display.text = $"{args[0]}";
            Txt_Cancel.transform.parent.gameObject.SetActive(null != _tipsExtraData.CancelCallBack);
            Txt_Confirm.transform.parent.gameObject.SetActive(null != _tipsExtraData.ConfirmCallBack);
            Txt_Cancel.text = string.IsNullOrEmpty(_tipsExtraData.CancelName) ? "取消" : _tipsExtraData.CancelName;
            Txt_Confirm.text = string.IsNullOrEmpty(_tipsExtraData.ConfirmName) ? "确认" : _tipsExtraData.ConfirmName;

            View.gameObject.SetActive(true);
        }

        void IUiView.Quit()
        {
        }

        private void Hide()
        {
            View.gameObject.SetActive(false);
        }

        /// <summary>
        /// 确定
        /// </summary>
        void OnClickClose()
        {
            _tipsExtraData.CloseCallBack?.Invoke();
            Hide();
        }

        /// <summary>
        /// 取消
        /// </summary>
        void OnClickCancel()
        {
            _tipsExtraData.CancelCallBack?.Invoke();
            Hide();
        }

        /// <summary>
        /// 关闭
        /// </summary>
        void OnClickConfirm()
        {
            _tipsExtraData.ConfirmCallBack?.Invoke();
            Hide();
        }
    }
}
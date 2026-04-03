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

using UnityEngine;

namespace EasyFramework.UI.Tips
{
    /// <summary>
    /// 提示窗基类
    /// </summary>
    public partial class TipsView
    {
        public void Awake(GameObject obj)
        {
            if (obj.Equals(_tipsViewGameObject))
                return;

            if (_tipsViewGameObject != null)
                Object.Destroy(_tipsViewGameObject);

            _tipsViewGameObject = obj;
            Bind();
        }

        public void Quit()
        {
            Dispose();
        }

        /// <summary>
        /// 展示
        /// </summary>
        /// <param name="displayContents">显示内容</param>
        /// <param name="viewExtraData">附加数据</param>
        public void Show(string displayContents, TipsViewExtraData viewExtraData)
        {
            _tipsExtraData?.Dispose();
            _tipsExtraData = viewExtraData;

            Txt_Display.text = displayContents;
            Txt_Cancel.transform.parent.gameObject.SetActive(null != viewExtraData.CancelCallBack);
            Txt_Confirm.transform.parent.gameObject.SetActive(null != viewExtraData.ConfirmCallBack);
            Txt_Cancel.text = string.IsNullOrEmpty(viewExtraData.CancelName) ? "取消" : viewExtraData.CancelName;
            Txt_Confirm.text = string.IsNullOrEmpty(viewExtraData.ConfirmName) ? "确认" : viewExtraData.ConfirmName;

            _tipsViewGameObject.SetActive(true);
        }

        private void Hide()
        {
            _tipsViewGameObject.SetActive(false);
        }

        /// <summary>
        /// 确定
        /// </summary>
        void OnClickClose()
        {
            _tipsExtraData?.CloseCallBack?.Invoke();
            Hide();
        }

        /// <summary>
        /// 取消
        /// </summary>
        void OnClickCancel()
        {
            _tipsExtraData?.CancelCallBack?.Invoke();
            Hide();
        }

        /// <summary>
        /// 关闭
        /// </summary>
        void OnClickConfirm()
        {
            _tipsExtraData?.ConfirmCallBack?.Invoke();
            Hide();
        }
    }
}
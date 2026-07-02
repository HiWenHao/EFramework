/*
 * ================================================
 * Describe:        This script is used to Update the StaticViersion file.
 * Author:          Alvin8412
 * CreationTime:    2026-05-29 22:39:39
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-29 22:39:39
 * ScriptVersion:   0.1
 * ===============================================
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework;
using UnityEngine;
using UnityEngine.UI;
using EasyFramework.Managers.Ui;

namespace EFExample.UI.Tips
{
    /// <summary>
    /// 提示窗参考实现
    /// 功能：显示文字 + 确认/取消/关闭按钮
    /// </summary>
    public class TipsView : IUiView, IUiEnable<string, TipsViewExtraData>
    {
        bool IUiView.AutoDestroy => true;
        float IUiView.AutoDestroyCountdown => 10.0f;
        uint IUiView.SerialId { get; set; }
        public RectTransform View { get; private set; }
        public UIViewType ViewType => UIViewType.Tips;

        private TipsViewExtraData _tipsExtraData;

        private Text Txt_Cancel;
        private Text Txt_Display;
        private Text Txt_Confirm;
        private List<Button> m_AllButtons;
        private List<ButtonPro> m_AllButtonPros;

        void IUiView.Bind(RectTransform uiViewRect)
        {
            View = uiViewRect;

            Txt_Cancel = EF.Tool.Find<Text>(uiViewRect, "Txt_Cancel");
            Txt_Display = EF.Tool.Find<Text>(uiViewRect, "Txt_Display");
            Txt_Confirm = EF.Tool.Find<Text>(uiViewRect, "Txt_Confirm");
            EF.Tool.Find<Button>(uiViewRect, "Btn_Close")
                .RegisterInListAndBindEvent(OnClickClose, ref m_AllButtons);
            EF.Tool.Find<Button>(uiViewRect, "Btn_Cancel")
                .RegisterInListAndBindEvent(OnClickCancel, ref m_AllButtons);
            EF.Tool.Find<Button>(uiViewRect, "Btn_Confirm")
                .RegisterInListAndBindEvent(OnClickConfirm, ref m_AllButtons);
        }

        void IUiView.Dispose()
        {
            m_AllButtons.ReleaseAndRemoveEvent();
            m_AllButtons = null;
            m_AllButtonPros.ReleaseAndRemoveEvent();
            m_AllButtonPros = null;
        }

        void IUiView.Awake() { }

        public void Enable(string args1, TipsViewExtraData args2)
        {
            if (string.IsNullOrEmpty(args1)) return;
            _tipsExtraData = args2;
            Txt_Display.text = args1;
            Txt_Cancel.transform.parent.gameObject.SetActive(null != _tipsExtraData.CancelCallBack);
            Txt_Confirm.transform.parent.gameObject.SetActive(null != _tipsExtraData.ConfirmCallBack);
            Txt_Cancel.text = string.IsNullOrEmpty(_tipsExtraData.CancelName) ? "取消" : _tipsExtraData.CancelName;
            Txt_Confirm.text = string.IsNullOrEmpty(_tipsExtraData.ConfirmName) ? "确认" : _tipsExtraData.ConfirmName;
            View.gameObject.SetActive(true);
        }

        void IUiView.Update(float elapse, float realElapse) { }

        void IUiView.Quit() { }

        void OnClickClose()
        {
            _tipsExtraData.CloseCallBack?.Invoke();
            UiSystem.Instance.CloseView(this).Forget();
        }

        void OnClickCancel()
        {
            _tipsExtraData.CancelCallBack?.Invoke();
            UiSystem.Instance.CloseView(this).Forget();
        }

        void OnClickConfirm()
        {
            _tipsExtraData.ConfirmCallBack?.Invoke();
            UiSystem.Instance.CloseView(this).Forget();
        }
    }
}

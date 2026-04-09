/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-04-03 16:44:22
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-03 16:44:22
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;
using EasyFramework.Manager.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.UI
{
    public partial class TipsView : IUiView
    {
        public static TipsView Open(params object[] args)
        {
            return EF.Ui.OpenPageView<TipsView>(args);
        }

        public static bool Close(params object[] args)
        {
            return EF.Ui.CloseView<TipsView>(args);
        }
        
        bool IUiView.AutoDestroy => true;
        uint IUiView.SerialId { get; set; }

        public RectTransform View { get; private set; }

        public UIViewType ViewType => UIViewType.Tips;
        
        private TipsViewExtraData _tipsExtraData;

        #region Components.可使用组件 -- Auto

        private Text Txt_Cancel;
        private Text Txt_Display;
        private Text Txt_Confirm;
        private List<Button> m_AllButtons;
        private List<ButtonPro> m_AllButtonPros;

        #endregion Components -- Auto

        void IUiView.Bind(RectTransform uiViewRect)
        {
            View = uiViewRect;

            #region Find components and register button event. 查找组件并且注册按钮事件 -- Auto

            Txt_Cancel = EF.Tool.Find<Text>(uiViewRect, "Txt_Cancel");
            Txt_Display = EF.Tool.Find<Text>(uiViewRect, "Txt_Display");
            Txt_Confirm = EF.Tool.Find<Text>(uiViewRect, "Txt_Confirm");
            EF.Tool.Find<Button>(uiViewRect, "Btn_Close")
                .RegisterInListAndBindEvent(OnClickClose, ref m_AllButtons);
            EF.Tool.Find<Button>(uiViewRect, "Btn_Cancel")
                .RegisterInListAndBindEvent(OnClickCancel, ref m_AllButtons);
            EF.Tool.Find<Button>(uiViewRect, "Btn_Confirm")
                .RegisterInListAndBindEvent(OnClickConfirm, ref m_AllButtons);

            #endregion Find components end. -- Auto
        }

        void IUiView.Dispose()
        {
            #region Quit Buttons.按钮 -- Auto

            m_AllButtons.ReleaseAndRemoveEvent();
            m_AllButtons = null;
            m_AllButtonPros.ReleaseAndRemoveEvent();
            m_AllButtonPros = null;

            #endregion Buttons.按钮 -- Auto
        }
    }
}
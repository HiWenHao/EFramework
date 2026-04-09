/*
 * ================================================
 * Describe:      Please modify the description..
 * Author:        Alvin8412
 * CreationTime:  2026-04-09 11:00:27
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-09 11:00:27
 * ScriptVersion: 0.1 
 * ================================================
 */

using EasyFramework;
using EasyFramework.UI;
using System.Collections.Generic;
using EasyFramework.Manager.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EFExample
{
    //-----The script is auto generated. Please do not make any changes-----
    public partial class UiCView : IUiView
    {
        public static UiCView Open(params object[] args)
        {
            return EF.Ui.OpenPageView<UiCView>(args);
        }

        public static bool Close(params object[] args)
        {
            return EF.Ui.CloseView<UiCView>(args);
        }

        bool IUiView.AutoDestroy => true;
        uint IUiView.SerialId { get; set; }
        public UIViewType ViewType => UIViewType.Page;
        public RectTransform View { get; private set; }

        private Text Txt_Title;
        private Text Txt_TotalTime;
        private Slider Sld_Timer;
        private List<Button> m_AllButtons;
        private List<ButtonPro> m_AllButtonPros;

        void IUiView.Bind(RectTransform uiViewRect)
        {
            View = uiViewRect;
            Txt_Title = EF.Tool.Find<Text>(uiViewRect.transform, "Txt_Title");
            Txt_TotalTime = EF.Tool.Find<Text>(uiViewRect.transform, "Txt_TotalTime");
            Sld_Timer = EF.Tool.Find<Slider>(uiViewRect.transform, "Sld_Timer");
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_QuitC").RegisterInListAndBindEvent(OnClickBtn_QuitC, ref m_AllButtons);
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_AddTimeEvent").RegisterInListAndBindEvent(OnClickBtn_AddTimeEvent, ref m_AllButtons);
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_RemoveTimeEvent").RegisterInListAndBindEvent(OnClickBtn_RemoveTimeEvent, ref m_AllButtons);
        }

        void IUiView.Dispose()
        {
            m_AllButtons.ReleaseAndRemoveEvent();
            m_AllButtons = null;
            m_AllButtonPros.ReleaseAndRemoveEvent();
            m_AllButtonPros = null;
        }
    }
    //-----The script is auto generated. Please do not make any changes-----
}

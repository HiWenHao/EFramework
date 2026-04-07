/*
 * ================================================
 * Describe:      案例音频管理器.
 * Author:        Alvin8412
 * CreationTime:  2026-04-06 23:05:28
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-06 23:05:28
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
    public partial class UiBView : IUiView
    {
        bool IUiView.AutoDestroy => true;
        uint IUiView.SerialId { get; set; }
        public UIViewType ViewType => UIViewType.Page;
        public RectTransform View { get; private set; }

        private Slider Sld_Volum;
        private List<Button> m_AllButtons;
        private List<ButtonPro> m_AllButtonPros;

        void IUiView.Bind(RectTransform uiViewRect)
        {
            View = uiViewRect;
            Sld_Volum = EF.Tool.Find<Slider>(uiViewRect.transform, "Sld_Volum");
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_2D").RegisterInListAndBindEvent(OnClickBtn_2D, ref m_AllButtons);
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_3D").RegisterInListAndBindEvent(OnClickBtn_3D, ref m_AllButtons);
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_ToC").RegisterInListAndBindEvent(OnClickBtn_ToC, ref m_AllButtons);
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_bgm").RegisterInListAndBindEvent(OnClickBtn_bgm, ref m_AllButtons);
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_BackA").RegisterInListAndBindEvent(OnClickBtn_BackA, ref m_AllButtons);
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_ToCPop").RegisterInListAndBindEvent(OnClickBtn_ToCPop, ref m_AllButtons);
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_StopBGM").RegisterInListAndBindEvent(OnClickBtn_StopBGM, ref m_AllButtons);
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_MuteAll").RegisterInListAndBindEvent(OnClickBtn_MuteAll, ref m_AllButtons);
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_PauseAll").RegisterInListAndBindEvent(OnClickBtn_PauseAll, ref m_AllButtons);
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_StopEffect").RegisterInListAndBindEvent(OnClickBtn_StopEffect, ref m_AllButtons);
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_UnPauseAll").RegisterInListAndBindEvent(OnClickBtn_UnPauseAll, ref m_AllButtons);
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_StopAllEffect").RegisterInListAndBindEvent(OnClickBtn_StopAllEffect, ref m_AllButtons);
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

/*
 * ================================================
 * Describe:      Please modify the description.
 * Author:        Alvin8412
 * CreationTime:  2026-04-27 16:14:58
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-27 16:14:58
 * ScriptVersion: 0.1 
 * ================================================
 */

using EasyFramework;
using EasyFramework.Managers.Ui;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFExample
{
    //-----The script is auto generated. Please do not make any changes-----
    public partial class TestTopViewOther : IUiView
    {
        bool IUiView.AutoDestroy => true;
        float IUiView.AutoDestroyCountdown => 60f;
        uint IUiView.SerialId { get; set; }
        public UIViewType ViewType => UIViewType.TopPermanent;
        public RectTransform View { get; private set; }

        private List<Button> m_AllButtons;
        private List<ButtonPro> m_AllButtonPros;

        void IUiView.Bind(RectTransform uiViewRect)
        {
            View = uiViewRect;
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_OpenOne").RegisterInListAndBindEvent(OnClickBtn_OpenOne, ref m_AllButtons);
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_CloseAll").RegisterInListAndBindEvent(OnClickBtn_CloseAll, ref m_AllButtons);
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

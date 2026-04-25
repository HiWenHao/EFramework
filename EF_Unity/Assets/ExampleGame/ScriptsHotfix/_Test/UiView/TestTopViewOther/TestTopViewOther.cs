/*
 * ================================================
 * Describe:      Please modify the description..
 * Author:        Alvin8412
 * CreationTime:  2026-04-25 11:24:58
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-25 11:24:58
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
    public partial class TestTopViewOther : IUiView
    {
        public static TestTopViewOther Open(params object[] args)
        {
            return EF.Ui.OpenPageView<TestTopViewOther>(args);
        }

        public static bool Close(params object[] args)
        {
            return EF.Ui.CloseView<TestTopViewOther>(args);
        }

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

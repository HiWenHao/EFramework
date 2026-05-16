/*
 * ================================================
 * Describe:      Please modify the description..
 * Author:        Alvin8412
 * CreationTime:  2026-04-29 11:00:07
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-29 11:00:07
 * ScriptVersion: 0.1 
 * ================================================
 */

using EasyFramework;

using System.Collections.Generic;
using EasyFramework.Managers.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace EFExample
{
    //-----The script is auto generated. Please do not make any changes-----
    public partial class LoginView : IUiView
    {
        bool IUiView.AutoDestroy => true;
        float IUiView.AutoDestroyCountdown => 10f;
        uint IUiView.SerialId { get; set; }
        public UIViewType ViewType => UIViewType.Page;
        public RectTransform View { get; private set; }

        private List<Button> m_AllButtons;
        private List<ButtonPro> m_AllButtonPros;

        void IUiView.Bind(RectTransform uiViewRect)
        {
            View = uiViewRect;
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_Login").RegisterInListAndBindEvent(OnClickBtn_Login, ref m_AllButtons);
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

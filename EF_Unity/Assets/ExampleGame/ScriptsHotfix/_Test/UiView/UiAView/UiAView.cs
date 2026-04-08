/*
 * ================================================
 * Describe:      Please modify the description..
 * Author:        Alvin8412
 * CreationTime:  2026-04-06 23:37:16
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-06 23:37:16
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
    public partial class UiAView : IUiView
    {
        bool IUiView.AutoDestroy => true;
        uint IUiView.SerialId { get; set; }
        public UIViewType ViewType => UIViewType.Page;
        public RectTransform View { get; private set; }

        private List<Button> m_AllButtons;
        private List<ButtonPro> m_AllButtonPros;

        void IUiView.Bind(RectTransform uiViewRect)
        {
            View = uiViewRect;
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_ToB").RegisterInListAndBindEvent(OnClickBtn_ToB, ref m_AllButtons);
            EF.Tool.Find<Button>(uiViewRect.transform, "Btn_Quit").RegisterInListAndBindEvent(OnClickBtn_Quit, ref m_AllButtons);
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

/*
 * ================================================
 * Describe:      Please modify the description..
 * Author:        Alvin8412
 * CreationTime:  2026-04-29 11:00:38
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-29 11:00:38
 * ScriptVersion: 0.1 
 * ================================================
 */

using EasyFramework;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace EFExample
{
    //-----The script is auto generated. Please do not make any changes-----
    public partial class BottomSubfieldView : IUiView
    {
        public static async UniTask<BottomSubfieldView> Open(params object[] args)
        {
            return await EF.Ui.OpenPageView<BottomSubfieldView>(args);
        }

        public static async UniTask<bool> Close(params object[] args)
        {
            return await EF.Ui.CloseView<BottomSubfieldView>(args);
        }

        bool IUiView.AutoDestroy => false;
        float IUiView.AutoDestroyCountdown => 10f;
        uint IUiView.SerialId { get; set; }
        public UIViewType ViewType => UIViewType.TopPermanent;
        public RectTransform View { get; private set; }

        private List<Button> m_AllButtons;
        private List<ButtonPro> m_AllButtonPros;

        void IUiView.Bind(RectTransform uiViewRect)
        {
            View = uiViewRect;
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

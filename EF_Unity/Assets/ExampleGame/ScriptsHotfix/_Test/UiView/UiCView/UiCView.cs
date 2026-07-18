/*
 * ================================================
 * Describe:        Please modify the description.
 * Author:          Alvin8412
 * CreationTime:    2026-07-14 15:18:38
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 15:18:38
 * ScriptVersion:   0.1
 * ================================================
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.Managers.Ui;

namespace EFExample
{
    //-----The script is auto generated. Please do not make any changes-----
    public partial class UiCView : IUiView
    {
        uint IUiView.SerialId { get; set; }
        public UiBinding Binding { get; private set; }
        public RectTransform View { get; private set; }

        private Text Txt_Title;
        private Text Txt_TotalTime;
        private Slider Sld_Timer;

        private Button Btn_QuitC;
        private Button Btn_AddTimeEvent;
        private Button Btn_RemoveTimeEvent;


        void IUiView.Bind(RectTransform uiViewRect, UiBinding binding)
        {
            View = uiViewRect;
            Binding = binding;
            Txt_Title = Binding.Resolve<Text>(nameof(Txt_Title));
            Txt_TotalTime = Binding.Resolve<Text>(nameof(Txt_TotalTime));
            Sld_Timer = Binding.Resolve<Slider>(nameof(Sld_Timer));
            Btn_QuitC = Binding.Resolve<Button>(nameof(Btn_QuitC));
            Btn_QuitC.onClick.AddListener(OnClickBtn_QuitC);
            Btn_AddTimeEvent = Binding.Resolve<Button>(nameof(Btn_AddTimeEvent));
            Btn_AddTimeEvent.onClick.AddListener(OnClickBtn_AddTimeEvent);
            Btn_RemoveTimeEvent = Binding.Resolve<Button>(nameof(Btn_RemoveTimeEvent));
            Btn_RemoveTimeEvent.onClick.AddListener(OnClickBtn_RemoveTimeEvent);
        }

        void IUiView.Dispose()
        {
            Btn_QuitC?.onClick.RemoveListener(OnClickBtn_QuitC);
            Btn_QuitC = null;
            Btn_AddTimeEvent?.onClick.RemoveListener(OnClickBtn_AddTimeEvent);
            Btn_AddTimeEvent = null;
            Btn_RemoveTimeEvent?.onClick.RemoveListener(OnClickBtn_RemoveTimeEvent);
            Btn_RemoveTimeEvent = null;
            Txt_Title = null;
            Txt_TotalTime = null;
            Sld_Timer = null;
        }
    }
    //-----The script is auto generated. Please do not make any changes-----
}

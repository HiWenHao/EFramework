/*
 * ================================================
 * Describe:        Please modify the description.
 * Author:          Alvin8412
 * CreationTime:    2026-07-14 15:24:54
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 15:24:54
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
    public partial class TipsView : IUiView
    {
        uint IUiView.SerialId { get; set; }
        public UiBinding Binding { get; private set; }
        public RectTransform View { get; private set; }

        private Text Txt_Cancel;
        private Text Txt_Display;
        private Text Txt_Confirm;

        private Button Btn_Close;
        private Button Btn_Cancel;
        private Button Btn_Confirm;


        void IUiView.Bind(RectTransform uiViewRect, UiBinding binding)
        {
            View = uiViewRect;
            Binding = binding;
            Txt_Cancel = Binding.Resolve<Text>(nameof(Txt_Cancel));
            Txt_Display = Binding.Resolve<Text>(nameof(Txt_Display));
            Txt_Confirm = Binding.Resolve<Text>(nameof(Txt_Confirm));
            Btn_Close = Binding.Resolve<Button>(nameof(Btn_Close));
            Btn_Close.onClick.AddListener(OnClickBtn_Close);
            Btn_Cancel = Binding.Resolve<Button>(nameof(Btn_Cancel));
            Btn_Cancel.onClick.AddListener(OnClickBtn_Cancel);
            Btn_Confirm = Binding.Resolve<Button>(nameof(Btn_Confirm));
            Btn_Confirm.onClick.AddListener(OnClickBtn_Confirm);
        }

        void IUiView.Dispose()
        {
            Btn_Close?.onClick.RemoveListener(OnClickBtn_Close);
            Btn_Close = null;
            Btn_Cancel?.onClick.RemoveListener(OnClickBtn_Cancel);
            Btn_Cancel = null;
            Btn_Confirm?.onClick.RemoveListener(OnClickBtn_Confirm);
            Btn_Confirm = null;
            Txt_Cancel = null;
            Txt_Display = null;
            Txt_Confirm = null;
        }
    }
    //-----The script is auto generated. Please do not make any changes-----
}

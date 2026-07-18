/*
 * ================================================
 * Describe:        Please modify the description.
 * Author:          Alvin8412
 * CreationTime:    2026-07-14 14:03:02
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 14:03:02
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
    public partial class CahtView : IUiView
    {
        public UiBinding Binding { get; private set; }
        uint IUiView.SerialId { get; set; }
        public RectTransform View { get; private set; }


        private Button Btn_ToChatMessage;


        void IUiView.Bind(RectTransform uiViewRect, UiBinding binding)
        {
            View = uiViewRect;
            Binding = binding;
            Btn_ToChatMessage = Binding.Resolve<Button>(nameof(Btn_ToChatMessage));
            Btn_ToChatMessage.onClick.AddListener(OnClickBtn_ToChatMessage);
        }

        void IUiView.Dispose()
        {
            Btn_ToChatMessage?.onClick.RemoveListener(OnClickBtn_ToChatMessage);
            Btn_ToChatMessage = null;
        }
    }
    //-----The script is auto generated. Please do not make any changes-----
}

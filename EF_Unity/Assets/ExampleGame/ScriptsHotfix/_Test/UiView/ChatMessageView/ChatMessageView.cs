/*
 * ================================================
 * Describe:        Please modify the description.
 * Author:          Alvin8412
 * CreationTime:    2026-07-14 14:03:15
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 14:03:15
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
    public partial class ChatMessageView : IUiView
    {
        public UiBinding Binding { get; private set; }
        uint IUiView.SerialId { get; set; }
        public RectTransform View { get; private set; }


        private Button Btn_Back;


        void IUiView.Bind(RectTransform uiViewRect, UiBinding binding)
        {
            View = uiViewRect;
            Binding = binding;
            Btn_Back = Binding.Resolve<Button>(nameof(Btn_Back));
            Btn_Back.onClick.AddListener(OnClickBtn_Back);
        }

        void IUiView.Dispose()
        {
            Btn_Back?.onClick.RemoveListener(OnClickBtn_Back);
            Btn_Back = null;
        }
    }
    //-----The script is auto generated. Please do not make any changes-----
}

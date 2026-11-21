/*
 * ================================================
 * Describe:        A视窗的子面板
 * Author:          Alvin5100
 * CreationTime:    2026-11-20 10:46:09
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-11-20 10:46:09
 * ScriptVersion:   0.1
 * ================================================
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.Ui;

namespace ExampleGame
{
    //-----The script is auto generated. Please do not make any changes-----
    public partial class UiAViewChild : IUiChildView
    {
        uint IUiView.SerialId { get; set; }
        public UiBinding Binding { get; private set; }
        public RectTransform View { get; private set; }


        private Button Btn_CloseSelf;


        void IUiView.Bind(RectTransform uiViewRect, UiBinding binding)
        {
            View = uiViewRect;
            Binding = binding;
            Btn_CloseSelf = Binding.Resolve<Button>(nameof(Btn_CloseSelf));
            Btn_CloseSelf.onClick.AddListener(OnClickBtn_CloseSelf);
        }

        void IUiView.Dispose()
        {
            Btn_CloseSelf?.onClick.RemoveListener(OnClickBtn_CloseSelf);
            Btn_CloseSelf = null;
        }
    }
    //-----The script is auto generated. Please do not make any changes-----
}

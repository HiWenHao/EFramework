/*
 * ================================================
 * Describe:        
 * Author:          Alvin8412
 * CreationTime:    2026-07-14 15:24:45
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 15:24:45
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
    public partial class PopupView : IUiView
    {
        uint IUiView.SerialId { get; set; }
        public UiBinding Binding { get; private set; }
        public RectTransform View { get; private set; }

        private RectTransform Rect_Bg;
        private Text Txt_Contents;



        void IUiView.Bind(RectTransform uiViewRect, UiBinding binding)
        {
            View = uiViewRect;
            Binding = binding;
            Rect_Bg = Binding.Resolve<RectTransform>(nameof(Rect_Bg));
            Txt_Contents = Binding.Resolve<Text>(nameof(Txt_Contents));
        }

        void IUiView.Dispose()
        {
            Rect_Bg = null;
            Txt_Contents = null;
        }
    }
    //-----The script is auto generated. Please do not make any changes-----
}

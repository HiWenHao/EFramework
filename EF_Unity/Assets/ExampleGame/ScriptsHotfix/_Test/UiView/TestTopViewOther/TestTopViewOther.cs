/*
 * ================================================
 * Describe:        Please modify the description.
 * Author:          Alvin8412
 * CreationTime:    2026-07-14 14:14:29
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 14:14:29
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
    public partial class TestTopViewOther : IUiView
    {
        public UiBinding Binding { get; private set; }
        uint IUiView.SerialId { get; set; }
        public RectTransform View { get; private set; }


        private Button Btn_OpenOne;
        private Button Btn_CloseAll;


        void IUiView.Bind(RectTransform uiViewRect, UiBinding binding)
        {
            View = uiViewRect;
            Binding = binding;
            Btn_OpenOne = Binding.Resolve<Button>(nameof(Btn_OpenOne));
            Btn_OpenOne.onClick.AddListener(OnClickBtn_OpenOne);
            Btn_CloseAll = Binding.Resolve<Button>(nameof(Btn_CloseAll));
            Btn_CloseAll.onClick.AddListener(OnClickBtn_CloseAll);
        }

        void IUiView.Dispose()
        {
            Btn_OpenOne?.onClick.RemoveListener(OnClickBtn_OpenOne);
            Btn_OpenOne = null;
            Btn_CloseAll?.onClick.RemoveListener(OnClickBtn_CloseAll);
            Btn_CloseAll = null;
        }
    }
    //-----The script is auto generated. Please do not make any changes-----
}

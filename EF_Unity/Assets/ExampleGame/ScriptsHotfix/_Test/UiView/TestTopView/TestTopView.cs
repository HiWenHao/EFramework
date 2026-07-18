/*
 * ================================================
 * Describe:        Please modify the description.
 * Author:          Alvin8412
 * CreationTime:    2026-07-14 15:25:07
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 15:25:07
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
    public partial class TestTopView : IUiView
    {
        uint IUiView.SerialId { get; set; }
        public UiBinding Binding { get; private set; }
        public RectTransform View { get; private set; }




        void IUiView.Bind(RectTransform uiViewRect, UiBinding binding)
        {
            View = uiViewRect;
            Binding = binding;
        }

        void IUiView.Dispose()
        {
        }
    }
    //-----The script is auto generated. Please do not make any changes-----
}

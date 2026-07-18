/*
 * ================================================
 * Describe:        这是测试UI的AAAAAA页面
 * Author:          Alvin8412
 * CreationTime:    2026-07-14 14:14:41
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 14:14:41
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
    public partial class UiAView : IUiView
    {
        public UiBinding Binding { get; private set; }
        uint IUiView.SerialId { get; set; }
        public RectTransform View { get; private set; }


        private Button Btn_ToB;
        private Button Btn_Quit;


        void IUiView.Bind(RectTransform uiViewRect, UiBinding binding)
        {
            View = uiViewRect;
            Binding = binding;
            Btn_ToB = Binding.Resolve<Button>(nameof(Btn_ToB));
            Btn_ToB.onClick.AddListener(OnClickBtn_ToB);
            Btn_Quit = Binding.Resolve<Button>(nameof(Btn_Quit));
            Btn_Quit.onClick.AddListener(OnClickBtn_Quit);
        }

        void IUiView.Dispose()
        {
            Btn_ToB?.onClick.RemoveListener(OnClickBtn_ToB);
            Btn_ToB = null;
            Btn_Quit?.onClick.RemoveListener(OnClickBtn_Quit);
            Btn_Quit = null;
        }
    }
    //-----The script is auto generated. Please do not make any changes-----
}

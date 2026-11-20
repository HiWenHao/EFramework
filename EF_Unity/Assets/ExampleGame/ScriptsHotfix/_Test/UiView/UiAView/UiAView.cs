/*
 * ================================================
 * Describe:        这是测试UI的AAAAAA页面
 * Author:          Alvin5100
 * CreationTime:    2026-11-20 09:54:37
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-11-20 09:54:37
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
        uint IUiView.SerialId { get; set; }
        public UiBinding Binding { get; private set; }
        public RectTransform View { get; private set; }


        private Button Btn_ToB;
        private Button Btn_Quit;
        private Button Btn_Test;


        void IUiView.Bind(RectTransform uiViewRect, UiBinding binding)
        {
            View = uiViewRect;
            Binding = binding;
            Btn_ToB = Binding.Resolve<Button>(nameof(Btn_ToB));
            Btn_ToB.onClick.AddListener(OnClickBtn_ToB);
            Btn_Quit = Binding.Resolve<Button>(nameof(Btn_Quit));
            Btn_Quit.onClick.AddListener(OnClickBtn_Quit);
            Btn_Test = Binding.Resolve<Button>(nameof(Btn_Test));
            Btn_Test.onClick.AddListener(OnClickBtn_Test);
        }

        void IUiView.Dispose()
        {
            Btn_ToB?.onClick.RemoveListener(OnClickBtn_ToB);
            Btn_ToB = null;
            Btn_Quit?.onClick.RemoveListener(OnClickBtn_Quit);
            Btn_Quit = null;
            Btn_Test?.onClick.RemoveListener(OnClickBtn_Test);
            Btn_Test = null;
        }
    }
    //-----The script is auto generated. Please do not make any changes-----
}

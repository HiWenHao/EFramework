/*
 * ================================================
 * Describe:        Please modify the description.
 * Author:          Alvin8412
 * CreationTime:    2026-07-14 14:03:27
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 14:03:27
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
    public partial class LoginView : IUiView
    {
        public UiBinding Binding { get; private set; }
        uint IUiView.SerialId { get; set; }
        public RectTransform View { get; private set; }


        private Button Btn_Login;


        void IUiView.Bind(RectTransform uiViewRect, UiBinding binding)
        {
            View = uiViewRect;
            Binding = binding;
            Btn_Login = Binding.Resolve<Button>(nameof(Btn_Login));
            Btn_Login.onClick.AddListener(OnClickBtn_Login);
        }

        void IUiView.Dispose()
        {
            Btn_Login?.onClick.RemoveListener(OnClickBtn_Login);
            Btn_Login = null;
        }
    }
    //-----The script is auto generated. Please do not make any changes-----
}

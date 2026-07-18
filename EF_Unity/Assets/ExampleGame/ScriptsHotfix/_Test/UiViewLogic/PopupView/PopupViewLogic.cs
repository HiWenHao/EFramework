/*
 * ================================================
 * Describe:        
 * Author:          Alvin8412
 * CreationTime:    2026-07-14 14:03:47
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
    /// <summary>
    /// 
    /// </summary>
    public partial class PopupView: IUiEnable<string>
    {
        private float _exitTime = 2.0f;
        void IUiView.Awake()
        {
        }

        void IUiView.Quit()
        {
        }

        public void Enable(string args)
        {
            if (string.IsNullOrEmpty(args)) return;
            Txt_Contents.text = args;
            _exitTime = 1.0f;
            Rect_Bg.anchoredPosition = Vector2.up * -40.0f;
        }

        void IUiView.Update(float elapse, float realElapse)
        {
            if ((_exitTime -= elapse) < 0.0f)
            {
                UiSystem.Instance.CloseView(this).Forget();
                return;
            }

            Rect_Bg.anchoredPosition += Vector2.up * (elapse * 80.0f);
        }
        #region Button invoke event. Do not change here.不要更改这行 -- Auto

        #endregion button invoke event. Do not change here.不要更改这行 -- Auto
    }
}

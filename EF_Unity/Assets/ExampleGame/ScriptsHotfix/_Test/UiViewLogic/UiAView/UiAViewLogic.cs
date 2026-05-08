/*
 * ================================================
 * Describe:      案例首页.
 * Author:        Alvin8412
 * CreationTime:  2026-04-06 23:04:44
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-08 17:13:02
 * ScriptVersion: 0.1 
 * ================================================
 */

using EasyFramework;
using EasyFramework.UI;
using System.Collections.Generic;
using EasyFramework.Managers.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EFExample
{
    /// <summary>
    /// 案例首页
    /// </summary>
    public partial class UiAView
    {
        void IUiView.Awake()
        {
            EF.Ui.OpenPageView<TestTopView>();
            EF.Ui.OpenPageView<TestBottomViewOne>();
        }

        void IUiView.Quit()
        {
            
            D.Warning("A Quit");
        }

        #region Button invoke event. Do not change here.不要更改这行 -- Auto

        private void OnClickBtn_ToB()
        {
            EF.Ui.OpenPageView<UiBView>("向B传递参数");
        }

        private void OnClickBtn_Quit()
        {
            EF.QuitGame();
        }

        private void OnClickBtnP_Test()
        {
            D.Log("OnClick:  BtnP_Test");
        }

        #endregion button invoke event. Do not change here.不要更改这行 -- Auto
    }
}

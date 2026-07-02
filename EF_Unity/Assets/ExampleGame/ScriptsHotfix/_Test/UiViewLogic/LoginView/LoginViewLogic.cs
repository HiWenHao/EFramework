/*
 * ================================================
 * Describe:      Please modify the description..
 * Author:        Alvin8412
 * CreationTime:  2026-04-29 11:00:07
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-29 11:00:07
 * ScriptVersion: 0.1 
 * ================================================
 */

using EasyFramework;

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace EFExample
{
    /// <summary>
    /// Please modify the description.
    /// </summary>
    public partial class LoginView
    {
        void IUiView.Awake()
        {
        }

        void IUiView.Quit()
        {
        }

        #region Button invoke event. Do not change here.不要更改这行 -- Auto

        private void OnClickBtn_Login()
        {
            UiSystem.Instance.OpenView<CahtView>().Forget();
            UiSystem.Instance.OpenView<BottomSubfieldView>().Forget();
        }

        #endregion button invoke event. Do not change here.不要更改这行 -- Auto
    }
}

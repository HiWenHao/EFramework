/*
 * ================================================
 * Describe:        A视窗的子面板
 * Author:          Alvin5100
 * CreationTime:    2026-11-20 09:50:17
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
    /// <summary>
    /// A视窗的子面板
    /// </summary>
    public partial class UiAViewChild
    {
        void IUiView.Awake()
        {
        }

        void IUiView.Quit()
        {
        }

        #region Button invoke event. Do not change here.不要更改这行 -- Auto

        private void OnClickBtn_CloseSelf()
        {
            EF.UI.CloseView(this).Forget();
        }

        #endregion button invoke event. Do not change here.不要更改这行 -- Auto
    }
}

/*
 * ================================================
 * Describe:      案例首页.
 * Author:        Alvin8412
 * CreationTime:  2026-04-06 23:04:44
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-06 23:37:16
 * ScriptVersion: 0.1 
 * ================================================
 */

using EasyFramework;
using EasyFramework.UI;
using System.Collections.Generic;
using EasyFramework.Manager.UI;
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
        }

        void IUiView.Quit()
        {
        }

        #region Button invoke event. Do not change here.不要更改这行 -- Auto

        private void OnClickBtn_ToB()
        {
            EF.Uii.OpenPage<UiBView>("向B传递参数");
        }

        private void OnClickBtn_Quit()
        {
            EF.QuitGame();
        }

        #endregion button invoke event. Do not change here.不要更改这行 -- Auto
    }
}

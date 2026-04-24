/*
 * ================================================
 * Describe:      Please modify the description..
 * Author:        Alvin8412
 * CreationTime:  2026-04-24 21:45:15
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-24 21:45:15
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
    /// Please modify the description.
    /// </summary>
    public partial class TestTopView
    {
        void IUiView.Awake()
        {
            D.Log("TestTopView Awake");

            EF.Timer.AddOnce(10.0f, delegate
            {
                EF.Ui.OpenPageView<TestTopViewOther>();
            });
        }

        void IUiView.Quit()
        {
            D.Warning("TestTopView Quit");
        }

        #region Button invoke event. Do not change here.不要更改这行 -- Auto

        #endregion button invoke event. Do not change here.不要更改这行 -- Auto
    }
}

/*
 * ================================================
 * Describe:      Please modify the description..
 * Author:        Alvin8412
 * CreationTime:  2026-04-24 21:58:47
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-25 11:24:58
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
    public partial class TestTopViewOther
    {
        void IUiView.Awake()
        {
            D.Log("TestTopViewOther Awake");
        }

        void IUiView.Quit()
        {
            D.Log("TestTopViewOther Quit");
        }

        #region Button invoke event. Do not change here.不要更改这行 -- Auto

        private void OnClickBtn_OpenOne()
        {
            EF.Ui.OpenPageView<TestTopView>();
        }

        #endregion button invoke event. Do not change here.不要更改这行 -- Auto
    }
}

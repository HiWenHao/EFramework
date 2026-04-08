/*
 * ================================================
 * Describe:      案例时间管理器.
 * Author:        Alvin8412
 * CreationTime:  2026-04-06 23:05:30
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-06 23:05:30
 * ScriptVersion: 0.1 
 * ================================================
 */

using System;
using EasyFramework;
using EasyFramework.UI;
using System.Collections.Generic;
using EasyFramework.Manager.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EFExample
{
    /// <summary>
    /// 案例时间管理器
    /// </summary>
    public partial class UiCView
    {
        private int _timeEventId;

        private Action<bool> _timeEvent;
        void IUiView.Awake()
        {
            _timeEvent = (bol) => { D.Warning("TimerEvent is done.   10f"); };

        }

        void IUiView.Update(float elapse, float realElapse)
        {
            Txt_TotalTime.text = $"当前游戏已运行 {EF.Timer.TotalTime} s\t 当前游戏已运行 {realElapse} s";
        }

        void IUiView.Quit()
        {
            D.Warning("C quit");
        }

        #region Button invoke event. Do not change here.不要更改这行 -- Auto

        private void OnClickBtn_QuitC()
        {
            EF.Uii.CloseView<UiCView>("C页面退出，向即将被显示的页面B传递参数");
        }

        private void OnClickBtn_AddTimeEvent()
        {
            D.Warning("Add timerEvent with 10.0f");
            _timeEventId = EF.Timer.AddOnce(10f, _timeEvent);
        }

        private void OnClickBtn_RemoveTimeEvent()
        {
            D.Log("OnClick:  Btn_RemoveTimeEvent");
            EF.Timer.RemoveAt(_timeEventId);
        }

        #endregion button invoke event. Do not change here.不要更改这行 -- Auto
    }
}

/*
 * ================================================
 * Describe:      案例时间管理器.
 * Author:        Alvin8412
 * CreationTime:  2026-04-06 23:05:30
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 15:18:38
 * ScriptVersion: 0.1
 * ================================================
 */

using System;
using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.Managers.Ui;

namespace EFExample
{
    /// <summary>
    /// 案例时间管理器
    /// </summary>
    public partial class UiCView : IUiEnable<int>
    {
        private int _viewId;
        private int _timeEventId;

        private Action<bool> _timeEvent;

        void IUiView.Awake()
        {
            _timeEvent = (bol) => { D.Warning("TimerEvent is done.   10f"); };
        }

        public void Enable(int args1)
        {
            _viewId = args1;
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
            if (_viewId == 2)
                UiSystem.Instance.CloseViewAndNotify<UiCView, UiBView, string>("C页面退出，向即将被显示的页面B传递参数").Forget();
            else
                UiSystem.Instance.CloseView<UiCView>().Forget();
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

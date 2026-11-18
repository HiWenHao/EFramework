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
using System.Threading;
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
        private float _elapsedTime;
        private CancellationTokenSource _timerCts;

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
            _elapsedTime += elapse;
            Txt_TotalTime.text = $"当前游戏已运行 {_elapsedTime} s\t 当前游戏已运行 {realElapse} s";
        }

        void IUiView.Quit()
        {
            CancelTimer();
            D.Warning("C quit");
        }

        // 取消当前计时器
        private void CancelTimer()
        {
            if (_timerCts == null)
                return;
            _timerCts.Cancel();
            _timerCts.Dispose();
            _timerCts = null;
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
            CancelTimer();
            var callback = _timeEvent;
            _timerCts = new CancellationTokenSource();
            DelayThenInvokeAsync(callback, _timerCts.Token).Forget();
        }

        // 延时 10 秒后触发回调，支持取消
        private static async UniTaskVoid DelayThenInvokeAsync(Action<bool> callback, CancellationToken token)
        {
            try
            {
                await UniTask.Delay(10000, cancellationToken: token);
                callback?.Invoke(true);
            }
            catch (OperationCanceledException)
            {
                // 取消是正常行为，静默完成
            }
        }

        private void OnClickBtn_RemoveTimeEvent()
        {
            D.Log("OnClick:  Btn_RemoveTimeEvent");
            CancelTimer();
        }

        #endregion button invoke event. Do not change here.不要更改这行 -- Auto
    }
}

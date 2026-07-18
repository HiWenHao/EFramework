/*
 * ================================================
 * Describe:        Please modify the description.
 * Author:          Alvin8412
 * CreationTime:    2026-07-14 14:10:42
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 15:24:54
 * ScriptVersion:   0.1
 * ================================================
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.Managers.Ui;
using EFExample.UI.Tips;

namespace EFExample
{
    /// <summary>
    /// Please modify the description.
    /// </summary>
    public partial class TipsView : IUiEnable<string, TipsViewExtraData>
    {
        private TipsViewExtraData _tipsExtraData;

        void IUiView.Awake()
        {
        }

        public void Enable(string args1, TipsViewExtraData args2)
        {
            if (string.IsNullOrEmpty(args1)) return;
            _tipsExtraData = args2;
            Txt_Display.text = args1;
            Txt_Cancel.transform.parent.gameObject.SetActive(null != _tipsExtraData.CancelCallBack);
            Txt_Confirm.transform.parent.gameObject.SetActive(null != _tipsExtraData.ConfirmCallBack);
            Txt_Cancel.text = string.IsNullOrEmpty(_tipsExtraData.CancelName) ? "取消" : _tipsExtraData.CancelName;
            Txt_Confirm.text = string.IsNullOrEmpty(_tipsExtraData.ConfirmName) ? "确认" : _tipsExtraData.ConfirmName;
            View.gameObject.SetActive(true);
        }

        void IUiView.Quit()
        {
        }

        #region Button invoke event. Do not change here.不要更改这行 -- Auto

        private void OnClickBtn_Close()
        {
            _tipsExtraData.CloseCallBack?.Invoke();
            UiSystem.Instance.CloseView(this).Forget();
        }

        private void OnClickBtn_Cancel()
        {
            _tipsExtraData.CancelCallBack?.Invoke();
            UiSystem.Instance.CloseView(this).Forget();
        }

        private void OnClickBtn_Confirm()
        {
            _tipsExtraData.ConfirmCallBack?.Invoke();
            UiSystem.Instance.CloseView(this).Forget();
        }

        #endregion button invoke event. Do not change here.不要更改这行 -- Auto
    }
}

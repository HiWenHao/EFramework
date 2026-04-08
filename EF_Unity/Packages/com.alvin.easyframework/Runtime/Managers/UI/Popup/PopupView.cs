/*
 * ================================================
 * Describe:      用来显示简短文字的弹窗
 * Author:        Alvin8412
 * CreationTime:  2026-04-05 20:25:05
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-05 20:25:05
 * ScriptVersion: 0.1
 * ================================================
 */

using EasyFramework.Manager.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.UI.Popup
{
    /// <summary>
    /// 弹窗面板
    /// </summary>
    public partial class PopupView : IUiView
    {
        public static void Open(params object[] args)
        {
            EF.Uii.OpenPageView<PopupView>(args);
        }

        public static void Close(params object[] args)
        {
            EF.Uii.CloseView<PopupView>(args);
        }

        bool IUiView.AutoDestroy => true;
        uint IUiView.SerialId { get; set; }
        public UIViewType ViewType => UIViewType.Popup;
        public RectTransform View { get; private set; }

        private float _exitTime = 2f; //	弹窗退出时间

        #region Components.可使用组件 -- Auto

        private Text Txt_Contents;
        private RectTransform Rect_Bg;

        #endregion Components -- Auto

        void IUiView.Bind(RectTransform uiViewRect)
        {
            View = uiViewRect;

            #region Find components and register button event. 查找组件并且注册按钮事件 -- Auto

            Txt_Contents = EF.Tool.Find<Text>(uiViewRect, "Txt_Contents");
            Rect_Bg = EF.Tool.Find<RectTransform>(uiViewRect, "Rect_Bg");

            #endregion Find components end. -- Auto

            View.anchorMax = Vector3.one;
            View.anchorMin = Vector3.zero;
            View.sizeDelta = Vector3.zero;
            View.localPosition = Vector3.zero;
        }

        void IUiView.Dispose()
        {
        }

        #region Button event in game ui page.

        #endregion button event.  Do not change here.不要更改这行 -- Auto
    }
}
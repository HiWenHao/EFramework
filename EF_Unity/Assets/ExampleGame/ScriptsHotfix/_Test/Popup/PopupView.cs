/*
 * ================================================
 * Describe:        This script is used to Update the StaticViersion file.
 * Author:          Alvin8412
 * CreationTime:    2026-05-29 22:39:39
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-29 22:39:39
 * ScriptVersion:   0.1
 * ===============================================
 */

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using EasyFramework.Managers.Ui;

namespace EFExample
{
    /// <summary>
    /// 简短文字弹窗参考实现
    /// 功能：显示一段文字，1 秒后自动向上消失
    /// </summary>
    public class PopupView : IUiView, IUiEnable<string>
    {
        bool IUiView.AutoDestroy => true;
        float IUiView.AutoDestroyCountdown => 10.0f;
        uint IUiView.SerialId { get; set; }
        public UIViewType ViewType => UIViewType.Popup;
        public RectTransform View { get; private set; }

        private float _exitTime = 2f;

        private Text Txt_Contents;
        private RectTransform Rect_Bg;

        void IUiView.Bind(RectTransform uiViewRect)
        {
            View = uiViewRect;
            Txt_Contents = EF.Tool.Find<Text>(uiViewRect, "Txt_Contents");
            Rect_Bg = EF.Tool.Find<RectTransform>(uiViewRect, "Rect_Bg");
        }

        void IUiView.Dispose()
        {
        }

        void IUiView.Awake()
        {
        }

        public void Enable(string args)
        {
            if (string.IsNullOrEmpty(args)) return;
            Txt_Contents.text = args;
            _exitTime = 1.0f;
            Rect_Bg.anchoredPosition = Vector2.up * -40.0f;
        }

        void IUiView.Update(float elapse, float realElapse)
        {
            if ((_exitTime -= elapse) < 0.0f)
            {
                UiSystem.Instance.CloseView(this).Forget();
                return;
            }

            Rect_Bg.anchoredPosition += Vector2.up * (elapse * 80.0f);
        }

        void IUiView.Quit()
        {
        }
    }
}
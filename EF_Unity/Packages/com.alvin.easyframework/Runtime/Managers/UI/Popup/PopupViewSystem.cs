/*
 * ================================================
 * Describe:      弹窗接口类 作为约束的存在.
 * Author:        Alvin5100
 * CreationTime:  2026-04-03 13:52:09
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-03 13:52:09
 * ScriptVersion: 0.1
 * ===============================================
 */

using EasyFramework.Manager.UI;
using UnityEngine;

namespace EasyFramework.UI.Popup
{
    /// <summary>
    /// 弹窗接口
    /// </summary>
    public partial class PopupView
    {
        void IUiView.Awake()
        {
        }

        void IUiView.Enable(params object[] args)
        {
            Txt_Contents.text = $"{args[0]}";

            _exitTime = 1.0f;
            Rect_Bg.anchoredPosition = Vector2.up * -40.0f;
        }

        void IUiView.Update(float elapse, float realElapse)
        {
            if ((_exitTime -= elapse) < 0.0f)
            {
                //View.gameObject.SetActive(false);
                
                //EF.Uii.CloseView(this);
                
                Close();
                return;
            }

            Rect_Bg.anchoredPosition += Vector2.up * (elapse * 80.0f);
        }

        void IUiView.Quit()
        {
        }
    }
}
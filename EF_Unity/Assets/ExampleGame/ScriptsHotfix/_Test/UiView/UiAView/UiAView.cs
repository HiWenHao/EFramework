/*
 * ================================================
 * Describe:      Please modify the description.
 * Author:        Alvin5100
 * CreationTime:  2026-05-08 17:13:02
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-08 17:13:02
 * ScriptVersion: 0.1 
 * ================================================
 */

using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers.Ui;

namespace EFExample
{
    //-----The script is auto generated. Please do not make any changes-----
    public partial class UiAView : IUiView
    {
        public static async UniTask<UiAView> Open(params object[] args)
        {
            return await UiSystem.Instance.OpenPageView<UiAView>(args);
        }

        public static async UniTask<bool> Close(params object[] args)
        {
            return await UiSystem.Instance.CloseView<UiAView>(args);
        }

        bool IUiView.AutoDestroy => true;
        float IUiView.AutoDestroyCountdown => 10f;
        uint IUiView.SerialId { get; set; }
        public UIViewType ViewType => UIViewType.Page;
        public RectTransform View { get; private set; }


        private Button btn_Btn_ToB;
        private Button btn_Btn_Quit;


        void IUiView.Bind(RectTransform uiViewRect)
        {
            View = uiViewRect;
            btn_Btn_ToB = EF.Tool.Find<Button>(uiViewRect.transform, "Btn_ToB");
            btn_Btn_ToB.onClick.AddListener(OnClickBtn_ToB);
            btn_Btn_Quit = EF.Tool.Find<Button>(uiViewRect.transform, "Btn_Quit");
            btn_Btn_Quit.onClick.AddListener(OnClickBtn_Quit);
        }

        void IUiView.Dispose()
        {
            btn_Btn_ToB?.onClick.RemoveListener(OnClickBtn_ToB);
            btn_Btn_ToB = null;
            btn_Btn_Quit?.onClick.RemoveListener(OnClickBtn_Quit);
            btn_Btn_Quit = null;
        }
    }
    //-----The script is auto generated. Please do not make any changes-----
}

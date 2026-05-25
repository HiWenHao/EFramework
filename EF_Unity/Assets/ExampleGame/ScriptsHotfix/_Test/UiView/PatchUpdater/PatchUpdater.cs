/*
 * ================================================
 * Describe:        更新面板.
 * Author:          Alvin8412
 * CreationTime:    2026-05-25 17:46:02
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-25 17:46:02
 * ScriptVersion:   0.1 
 * ================================================
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.Managers.Ui;

namespace EFExample
{
    //-----The script is auto generated. Please do not make any changes-----
    public partial class PatchUpdater : IUiView
    {
        public static async UniTask<PatchUpdater> Open(params object[] args)
        {
            return await UiSystem.Instance.OpenPageView<PatchUpdater>(args);
        }

        public static async UniTask<bool> Close(params object[] args)
        {
            return await UiSystem.Instance.CloseView<PatchUpdater>(args);
        }

        bool IUiView.AutoDestroy => true;
        float IUiView.AutoDestroyCountdown => 10f;
        uint IUiView.SerialId { get; set; }
        public UIViewType ViewType => UIViewType.Page;
        public RectTransform View { get; private set; }

        private RectTransform Tran_Updater;
        private Slider Sld_UpdaterSlider;
        private Text Txt_UpdaterTips;



        void IUiView.Bind(RectTransform uiViewRect)
        {
            View = uiViewRect;
            Tran_Updater = EF.Tool.Find<RectTransform>(uiViewRect.transform, "Tran_Updater");
            Sld_UpdaterSlider = EF.Tool.Find<Slider>(uiViewRect.transform, "Sld_UpdaterSlider");
            Txt_UpdaterTips = EF.Tool.Find<Text>(uiViewRect.transform, "Txt_UpdaterTips");
        }

        void IUiView.Dispose()
        {
            Tran_Updater = null;
            Sld_UpdaterSlider = null;
            Txt_UpdaterTips = null;
        }
    }
    //-----The script is auto generated. Please do not make any changes-----
}

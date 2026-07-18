/*
 * ================================================
 * Describe:        更新面板
 * Author:          Alvin8412
 * CreationTime:    2026-07-14 14:03:36
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 14:03:36
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
        public UiBinding Binding { get; private set; }
        uint IUiView.SerialId { get; set; }
        public RectTransform View { get; private set; }

        private RectTransform Tran_Updater;
        private Slider Sld_UpdaterSlider;
        private Text Txt_UpdaterTips;



        void IUiView.Bind(RectTransform uiViewRect, UiBinding binding)
        {
            View = uiViewRect;
            Binding = binding;
            Tran_Updater = Binding.Resolve<RectTransform>(nameof(Tran_Updater));
            Sld_UpdaterSlider = Binding.Resolve<Slider>(nameof(Sld_UpdaterSlider));
            Txt_UpdaterTips = Binding.Resolve<Text>(nameof(Txt_UpdaterTips));
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

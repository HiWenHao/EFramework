/*
 * ================================================
 * Describe:        这是测试UI的B页面1111
 * Author:          Alvin8412
 * CreationTime:    2026-07-14 15:18:29
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 15:18:29
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
    public partial class UiBView : IUiView
    {
        uint IUiView.SerialId { get; set; }
        public UiBinding Binding { get; private set; }
        public RectTransform View { get; private set; }

        private Image Img_bgm;
        private Slider Sld_Volum;

        private Button Btn_2D;
        private Button Btn_3D;
        private Button Btn_ToC;
        private Button Btn_bgm;
        private Button Btn_BackA;
        private Button Btn_ToCPop;
        private Button Btn_StopBGM;
        private Button Btn_MuteAll;
        private Button Btn_PauseAll;
        private Button Btn_StopEffect;
        private Button Btn_UnPauseAll;
        private Button Btn_StopAllEffect;


        void IUiView.Bind(RectTransform uiViewRect, UiBinding binding)
        {
            View = uiViewRect;
            Binding = binding;
            Img_bgm = Binding.Resolve<Image>(nameof(Img_bgm));
            Sld_Volum = Binding.Resolve<Slider>(nameof(Sld_Volum));
            Btn_2D = Binding.Resolve<Button>(nameof(Btn_2D));
            Btn_2D.onClick.AddListener(OnClickBtn_2D);
            Btn_3D = Binding.Resolve<Button>(nameof(Btn_3D));
            Btn_3D.onClick.AddListener(OnClickBtn_3D);
            Btn_ToC = Binding.Resolve<Button>(nameof(Btn_ToC));
            Btn_ToC.onClick.AddListener(OnClickBtn_ToC);
            Btn_bgm = Binding.Resolve<Button>(nameof(Btn_bgm));
            Btn_bgm.onClick.AddListener(OnClickBtn_bgm);
            Btn_BackA = Binding.Resolve<Button>(nameof(Btn_BackA));
            Btn_BackA.onClick.AddListener(OnClickBtn_BackA);
            Btn_ToCPop = Binding.Resolve<Button>(nameof(Btn_ToCPop));
            Btn_ToCPop.onClick.AddListener(OnClickBtn_ToCPop);
            Btn_StopBGM = Binding.Resolve<Button>(nameof(Btn_StopBGM));
            Btn_StopBGM.onClick.AddListener(OnClickBtn_StopBGM);
            Btn_MuteAll = Binding.Resolve<Button>(nameof(Btn_MuteAll));
            Btn_MuteAll.onClick.AddListener(OnClickBtn_MuteAll);
            Btn_PauseAll = Binding.Resolve<Button>(nameof(Btn_PauseAll));
            Btn_PauseAll.onClick.AddListener(OnClickBtn_PauseAll);
            Btn_StopEffect = Binding.Resolve<Button>(nameof(Btn_StopEffect));
            Btn_StopEffect.onClick.AddListener(OnClickBtn_StopEffect);
            Btn_UnPauseAll = Binding.Resolve<Button>(nameof(Btn_UnPauseAll));
            Btn_UnPauseAll.onClick.AddListener(OnClickBtn_UnPauseAll);
            Btn_StopAllEffect = Binding.Resolve<Button>(nameof(Btn_StopAllEffect));
            Btn_StopAllEffect.onClick.AddListener(OnClickBtn_StopAllEffect);
        }

        void IUiView.Dispose()
        {
            Btn_2D?.onClick.RemoveListener(OnClickBtn_2D);
            Btn_2D = null;
            Btn_3D?.onClick.RemoveListener(OnClickBtn_3D);
            Btn_3D = null;
            Btn_ToC?.onClick.RemoveListener(OnClickBtn_ToC);
            Btn_ToC = null;
            Btn_bgm?.onClick.RemoveListener(OnClickBtn_bgm);
            Btn_bgm = null;
            Btn_BackA?.onClick.RemoveListener(OnClickBtn_BackA);
            Btn_BackA = null;
            Btn_ToCPop?.onClick.RemoveListener(OnClickBtn_ToCPop);
            Btn_ToCPop = null;
            Btn_StopBGM?.onClick.RemoveListener(OnClickBtn_StopBGM);
            Btn_StopBGM = null;
            Btn_MuteAll?.onClick.RemoveListener(OnClickBtn_MuteAll);
            Btn_MuteAll = null;
            Btn_PauseAll?.onClick.RemoveListener(OnClickBtn_PauseAll);
            Btn_PauseAll = null;
            Btn_StopEffect?.onClick.RemoveListener(OnClickBtn_StopEffect);
            Btn_StopEffect = null;
            Btn_UnPauseAll?.onClick.RemoveListener(OnClickBtn_UnPauseAll);
            Btn_UnPauseAll = null;
            Btn_StopAllEffect?.onClick.RemoveListener(OnClickBtn_StopAllEffect);
            Btn_StopAllEffect = null;
            Img_bgm = null;
            Sld_Volum = null;
        }
    }
    //-----The script is auto generated. Please do not make any changes-----
}

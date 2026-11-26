/*
 * ================================================
 * Describe:        GM示例总面板
 * Author:          Alvin5100
 * CreationTime:    2026-11-25 13:49:52
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-11-25 13:49:52
 * ScriptVersion:   0.1
 * ================================================
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.Ui;

namespace ExampleGame
{
    //-----The script is auto generated. Please do not make any changes-----
    public partial class GmPanelView : IUiView
    {
        uint IUiView.SerialId { get; set; }
        public UiBinding Binding { get; private set; }
        public RectTransform View { get; private set; }

        private ScrollListPro ScrList_SectionList;
        private ScrollListPro ScrList_CommandList;

        private Button Btn_CloseButton;
        private Button Btn_RefreshButton;


        void IUiView.Bind(RectTransform uiViewRect, UiBinding binding)
        {
            View = uiViewRect;
            Binding = binding;
            ScrList_SectionList = Binding.Resolve<ScrollListPro>(nameof(ScrList_SectionList));
            ScrList_CommandList = Binding.Resolve<ScrollListPro>(nameof(ScrList_CommandList));
            Btn_CloseButton = Binding.Resolve<Button>(nameof(Btn_CloseButton));
            Btn_CloseButton.onClick.AddListener(OnClickBtn_CloseButton);
            Btn_RefreshButton = Binding.Resolve<Button>(nameof(Btn_RefreshButton));
            Btn_RefreshButton.onClick.AddListener(OnClickBtn_RefreshButton);
        }

        void IUiView.Dispose()
        {
            Btn_CloseButton?.onClick.RemoveListener(OnClickBtn_CloseButton);
            Btn_CloseButton = null;
            Btn_RefreshButton?.onClick.RemoveListener(OnClickBtn_RefreshButton);
            Btn_RefreshButton = null;
            ScrList_SectionList = null;
            ScrList_CommandList = null;
        }
    }
    //-----The script is auto generated. Please do not make any changes-----
}
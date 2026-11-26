/*
 * ================================================
 * Describe:        单个命令行元素
 * Author:          Alvin5100
 * CreationTime:    2026-11-25 11:18:25
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-26 17:50:00
 * ScriptVersion:   0.3
 * ================================================
 */

using Cysharp.Threading.Tasks;
using EasyFramework.GameMaster;
using EasyFramework.Ui;
using UnityEngine;
using UnityEngine.UI;
using EasyFramework.Managers;

namespace ExampleGame
{
    /// <summary>
    /// 单个命令行元素
    /// </summary>
    public class CommandItem : ScrollProItemBase
    {
        private Text DisplayID;
        private Text DisplayName;
        private Button Run;
        private Image Bg;

        private Text Placeholder;
        private InputField IptContents;

        private GmCommandInfo _data;

        protected override void OnCreate()
        {
            Bg = transform.GetComponent<Image>();
            DisplayID = EFC.GetOrCreate<ToolManager>().Find<Text>(transform, nameof(DisplayID));
            DisplayName = EFC.GetOrCreate<ToolManager>().Find<Text>(transform, nameof(DisplayName));
            Placeholder = EFC.GetOrCreate<ToolManager>().Find<Text>(transform, nameof(Placeholder));
            Run = EFC.GetOrCreate<ToolManager>().Find<Button>(transform, nameof(Run));
            IptContents = EFC.GetOrCreate<ToolManager>().Find<InputField>(transform, nameof(IptContents));

            Run.onClick.AddListener(OnClickSelf);
        }

        protected override void OnShowContent(int dataIndex)
        {
            GmCommandInfo data = GmData.CurrentCommandList[dataIndex];
            if (_data != null && _data.Equals(data)) return;
            _data = data;
            DisplayID.text = _data.Id;
            DisplayName.text = _data.DisplayName;
            bool isEmpty = string.IsNullOrEmpty(data.ParamHint);
            if (!isEmpty)
            {
                IptContents.text = "";
                Placeholder.text = data.ParamHint;
            }

            IptContents.gameObject.SetActive(!isEmpty);

            // 按风险等级着色（示例局部配色）
            if (Bg != null) Bg.color = GetRiskColor(_data.Risk);
        }

        protected override void OnDestroyed()
        {
            Bg = null;
            _data = null;
            Run.onClick.RemoveAllListeners();
            IptContents = null;
            DisplayID = null;
            DisplayName = null;
            Placeholder = null;
            Run = null;
        }


        // 风险等级 -> 背景色（示例局部配色，浅色低饱和不压文字）
        private static Color GetRiskColor(GmRisk risk)
        {
            return risk switch
            {
                GmRisk.ReadOnly => new Color(0.82f, 0.85f, 0.88f, 1f),
                GmRisk.Normal => new Color(0.82f, 0.92f, 0.84f, 1f),
                GmRisk.Dangerous => new Color(0.98f, 0.91f, 0.78f, 1f),
                GmRisk.Destructive => new Color(0.96f, 0.82f, 0.82f, 1f),
                _ => Color.white
            };
        }

        private void OnClickSelf()
        {
            if (_data == null) return;
            GameMasterManager.Run(_data.Id, IptContents.text).Forget();
        }
    }
}
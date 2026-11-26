/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Alvin5100
 * CreationTime:    2026-11-25 11:18:25
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-11-25 11:18:25
 * ScriptVersion:   0.1
 * ================================================
 */

using EasyFramework;
using EasyFramework.Ui;
using UnityEngine.UI;
using EasyFramework.Managers;

namespace ExampleGame
{
    /// <summary>
    /// 单个标题元素
    /// </summary>
    public class SectionItem : ScrollProItemBase
    {
        private Text _content;
        private Button _selfButton;

        private string _section;

        protected override void OnCreate()
        {
            _content = EFC.GetOrCreate<ToolManager>().Find<Text>(transform, "Content");
            _selfButton = transform.GetComponent<Button>();

            _selfButton.onClick.AddListener(OnClickSelf);
        }

        protected override void OnShowContent(int dataIndex)
        {
            string data = GmData.SectionList[dataIndex];
            if (!string.IsNullOrEmpty(_section) && _section.Equals(data)) return;
            _section = data;
            _content.text = data;
        }

        protected override void OnDestroyed()
        {
            _selfButton.onClick.RemoveAllListeners();
            _content = null;
            _selfButton = null;
        }

        private void OnClickSelf()
        {
            EFC.GetOrCreate<EventManager>().Publish(new SectionChanged()
            {
                CurrentSection = _section,
            });
        }
    }
}
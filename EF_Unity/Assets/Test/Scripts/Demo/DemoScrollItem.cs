using UnityEngine;
using UnityEngine.UI;

namespace EFExample
{
    /// <summary>
    /// ScrollItemBase 子类演示。
    /// 挂载到 item prefab 上，配合 ScrollListDemo_AutoLayout 使用。
    /// </summary>
    public class DemoScrollItem : ScrollItemBase
    {
        // 数据源引用（由 Demo 在 Start 时注入）
        public static System.Func<int, string> GetData;

        private static System.Func<int, (string title, string body)> _getRichData;
        public static void SetDataProvider(System.Func<int, (string title, string body)> provider)
        {
            _getRichData = provider;
        }

        private Text   _titleText;
        private Text   _bodyText;

        protected override void OnCreate()
        {
            _titleText = transform.Find("TitleText")?.GetComponent<Text>();
            _bodyText  = transform.Find("BodyText")?.GetComponent<Text>();

            // 如果找不到具名子节点，尝试获取所有 Text 组件
            if (_bodyText == null)
            {
                var texts = GetComponentsInChildren<Text>();
                if (texts.Length >= 2)
                {
                    _titleText = texts[0];
                    _bodyText  = texts[1];
                }
                else if (texts.Length >= 1)
                {
                    _bodyText = texts[0];
                }
            }
        }

        protected override void OnShowContent(int dataIndex)
        {
            // 设置标题
            if (_titleText != null)
                _titleText.text = $"Item #{dataIndex}";
            else
            {
                var firstText = GetComponentInChildren<Text>();
                if (firstText != null)
                    firstText.text = $"Item #{dataIndex}";
            }

            // 设置内容文本
            if (_bodyText != null && _getRichData != null)
            {
                var data = _getRichData(dataIndex);
                _bodyText.text = data.body;
            }
            else if (_bodyText != null && GetData != null)
            {
                _bodyText.text = GetData(dataIndex);
            }

            // 不定高 Image
            var imgChild = transform.Find("Image");
            if (imgChild != null)
            {
                var rect = imgChild.GetComponent<RectTransform>();
                if (rect != null)
                {
                    var rng = new System.Random((int)(dataIndex * 2654435761));
                    rect.sizeDelta = new Vector2(100, Random.value * 200f + 100f);
                    // 用确定性随机（不改 data 时尺寸稳定）
                    rect.sizeDelta = new Vector2(100, 100f + 200f * (float)(rng.NextDouble()));
                }
            }

            // 背景色
            var bg = GetComponent<Image>();
            if (bg != null)
            {
                bg.color = (dataIndex % 2 == 0)
                    ? new Color(0.18f, 0.18f, 0.22f)
                    : new Color(0.22f, 0.22f, 0.26f);
            }
        }
    }
}

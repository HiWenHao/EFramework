using EasyFramework.Managers.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace EFExample
{
    /// <summary>
    /// ScrollItemBase 子类演示。
    /// 挂载到 item prefab 上，配合 ScrollListDemo_AutoLayout 使用。
    /// </summary>
    public class DemoScrollItem : ScrollProItemBase
    {
        public static System.Func<int, string> GetData;
        
        public Image Bg;
        public Image Tex;
        public Text Contents;

        RectTransform _texRect;
        protected override void OnCreate()
        {
            Bg = transform.GetComponent<Image>();
            Tex = transform.Find("Image").GetComponent<Image>();
            Contents  = transform.Find("ContentInfo").GetComponent<Text>();
            _texRect = Tex.GetComponent<RectTransform>();
            _texRect.sizeDelta = new Vector2(300f, Random.value * 300f + 50f);
        }

        protected override void OnShowContent(int dataIndex)
        {
            var data = GetData(dataIndex);
            // 每次展示都重新随机图片高度，模拟不规则 item
            Bg.color = (dataIndex % 2 == 0)
                ? new Color(0.18f, 0.18f, 0.22f)
                : new Color(0.22f, 0.22f, 0.26f);
            Contents.text = $"#{dataIndex}: {data}";
        }
    }
}

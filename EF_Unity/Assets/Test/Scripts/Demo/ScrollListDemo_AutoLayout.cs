// ================================================================
// ScrollListDemo_AutoLayout.cs
// 演示 _autoRebuildLayout 模式：OnGetItemSize 返回估算值，
// 真实尺寸由 VerticalLayoutGroup / ContentSizeFitter 驱动，
// 组件自动 ForceRebuildLayoutImmediate + 测量后修正位置。
// ================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFExample
{
    /// <summary>
    /// 自适应布局模式演示。
    /// Prefab 结构示例（VerticalLayoutGroup + ContentSizeFitter）：
    ///   ItemRoot (Image + VerticalLayoutGroup + ContentSizeFitter)
    ///     └── TitleText (Text)
    ///     └── BodyText  (Text)  ← 变长内容驱动 item 高度
    ///
    /// 使用方式：在 Inspector 中勾选 InfiniteIrregularScrollList 的 Auto Rebuild Layout。
    /// OnGetItemSize 只需返回一个合理的估算值（如 100f）。
    /// </summary>
    public class ScrollListDemo_AutoLayout : MonoBehaviour
    {
        [Header("引用")]
        public InfiniteIrregularScrollList ScrollList;

        [Header("UI")]
        public Button BtnAppend;
        public Button BtnPrepend;
        public Button BtnScrollBottom;
        public Button BtnBatchAdd;
        public Text   TxtInfo;
        public Text   TxtEdgeHint;

        [Header("模拟数据")]
        public string[] SampleTexts = new string[]
        {
            "短文本",
            "这是一段中等长度的文本，用来模拟聊天消息的不同高度。",
            "这段文本比较长。当 VerticalLayoutGroup 和 ContentSizeFitter 协同工作时，item 的实际高度由子物体的累加高度决定。开启 Auto Rebuild Layout 后，组件会在 OnUpdateItem 填充完内容后自动调用 ForceRebuildLayoutImmediate 并测量真实高度，然后修正后续所有 item 的累积位置。",
            "超短",
            "又是一段中等长度的文本，用来演示自适应布局。",
        };

        private readonly List<string> _data = new List<string>();

        private void Start()
        {
            if (ScrollList == null)
            {
                Debug.LogError("[ScrollListDemo_AutoLayout] 未找到 InfiniteIrregularScrollList。");
                return;
            }

            // ---- 关键：勾选 Inspector 中的 Auto Rebuild Layout 开关 ----
            // ScrollList._autoRebuildLayout = true;  // 或在 Inspector 中手动勾选

            // OnGetItemSize 只返回估算值，真实尺寸由布局系统决定
            ScrollList.OnGetItemSize = _ => 120f; // 估算值，会被后续测量覆盖
            ScrollList.OnUpdateItem  = OnUpdateItem;

            // ---- 边缘回调：演示自动无限加载 ----
            ScrollList.OnReachTop    += OnReachEdgeTop;
            ScrollList.OnReachBottom += OnReachEdgeBottom;

            // ---- 可见性回调 ----
            ScrollList.OnItemVisibilityChanged += (idx, visible) =>
            {
                if (TxtEdgeHint != null)
                    TxtEdgeHint.text = $"Item#{idx} {(visible ? "进入" : "离开")} | T={ScrollList.IsAtTop()} B={ScrollList.IsAtBottom()}";
            };

            // 初始化数据
            for (int i = 0; i < 30; i++)
                _data.Add(GetRandomText(i));

            ScrollList.Initialize(_data.Count);
            UpdateInfoText();

            if (BtnAppend      != null) BtnAppend.onClick.AddListener(OnClickAppend);
            if (BtnPrepend     != null) BtnPrepend.onClick.AddListener(OnClickPrepend);
            if (BtnScrollBottom!= null) BtnScrollBottom.onClick.AddListener(() => ScrollList.ScrollToBottom());
            if (BtnBatchAdd    != null) BtnBatchAdd.onClick.AddListener(OnClickBatchAdd);
        }

        private void OnReachEdgeTop()
        {
            Debug.Log("[AutoLayout] 到达顶部 —— 可加载更早消息");
            // 示例：在真实场景中调用 PrependData()
        }

        private void OnReachEdgeBottom()
        {
            Debug.Log("[AutoLayout] 到达底部 —— 可加载更多消息");
            // 示例：在真实场景中调用 AppendData()
        }

        private void OnUpdateItem(GameObject go, int index)
        {
            // 找到子节点中的 Text 并赋值——VerticalLayoutGroup + ContentSizeFitter 会自动撑开高度
            var texts = go.GetComponentsInChildren<Text>();
            foreach (var t in texts)
            {
                if (t.name.Contains("Body") || t.name.Contains("Content"))
                {
                    t.text = _data[index];
                }
                else
                {
                    t.text = $"Item #{index}";
                }
            }
            
            var imageChild = go.transform.Find("Image");
            if (imageChild != null)
            {
                var rect = imageChild.GetComponent<RectTransform>();
                if (rect != null)
                    rect.sizeDelta = new Vector2(100, Random.Range(100f, 300f));
            }

            // 背景色
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                img.color = (index % 2 == 0)
                    ? new Color(0.18f, 0.18f, 0.22f)
                    : new Color(0.22f, 0.22f, 0.26f);
            }

            // 不需要手动调用 LayoutRebuilder！组件在 _autoRebuildLayout=true 时会自动处理。
        }

        private string GetRandomText(int seed)
        {
            var rng = new System.Random((int)(seed * 2654435761));
            return SampleTexts[rng.Next(SampleTexts.Length)];
        }

        private void OnClickAppend()
        {
            for (int i = 0; i < 5; i++)
                _data.Add(GetRandomText(_data.Count));
            ScrollList.AppendData(5);
            UpdateInfoText();
        }

        private void OnClickPrepend()
        {
            for (int i = 0; i < 5; i++)
                _data.Insert(i, GetRandomText(i + 1000));
            ScrollList.PrependData(5);
            UpdateInfoText();
        }

        /// <summary>批处理演示：连续追加 3 次，只重建一次</summary>
        private void OnClickBatchAdd()
        {
            ScrollList.BeginUpdate();
            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < 5; i++)
                    _data.Add(GetRandomText(_data.Count));
                ScrollList.AppendData(5);
            }
            ScrollList.EndUpdate();
            UpdateInfoText();
        }

        private void UpdateInfoText()
        {
            if (TxtInfo != null)
                TxtInfo.text = $"Total: {ScrollList.TotalCount}  Vis: [{ScrollList.FirstVisibleIndex}..{ScrollList.LastVisibleIndex}]";
        }

        private void Update()
        {
            if (Time.frameCount % 30 == 0) UpdateInfoText();
        }

        private void OnDestroy()
        {
            if (ScrollList != null)
            {
                ScrollList.OnItemVisibilityChanged = null;
            }
            if (BtnAppend      != null) BtnAppend.onClick.RemoveAllListeners();
            if (BtnPrepend     != null) BtnPrepend.onClick.RemoveAllListeners();
            if (BtnScrollBottom!= null) BtnScrollBottom.onClick.RemoveAllListeners();
            if (BtnBatchAdd    != null) BtnBatchAdd.onClick.RemoveAllListeners();
        }
    }
}

// ================================================================
// ScrollListDemo_AutoLayout.cs
// OSA-style 演示：IScrollItem + 预计算累积位置 + 动态尺寸变更
// Prefab 需挂 DemoScrollItem（继承 ScrollItemBase）。
// ================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFExample
{
    public class ScrollListDemo_AutoLayout : MonoBehaviour
    {
        [Header("引用")]
        public InfiniteIrregularScrollList ScrollList;

        [Header("UI")]
        public Button BtnAppend;
        public Button BtnPrepend;
        public Button BtnScrollBottom;
        public Button BtnBatchAdd;
        public Button BtnExpandRandom; // OSA: 随机展开/折叠
        public Button BtnRefreshItem;  // OSA: 刷新首个可见 item
        public Text   TxtInfo;
        public Text   TxtEdgeHint;

        [Header("模拟数据")]
        public string[] SampleTexts = new string[]
        {
            "短文本",
            "这是一段中等长度的文本，用来模拟聊天消息的不同高度。",
            "这段文本比较长。当 VerticalLayoutGroup 和 ContentSizeFitter 协同工作时，item 的实际高度由子物体的累加高度决定。",
            "超短",
            "又是一段中等长度的文本，用来演示自适应布局。",
        };
        public string ExpandText = " [展开] 这里增加了很多内容来撑开高度。用于演示 OSA ChangeItemSize API。";

        private List<(string title, string body)> _data;
        private bool _expandedMode = false;

        private void Start()
        {
            if (ScrollList == null)
            {
                Debug.LogError("[ScrollListDemo_AutoLayout] 未找到 InfiniteIrregularScrollList。");
                return;
            }

            // ---- 初始化数据 ----
            _data = new List<(string, string)>();
            for (int i = 0; i < 30; i++)
                _data.Add(($"Item #{i}", GetRandomText(i)));

            // ---- 注入数据到 DemoScrollItem ----
            DemoScrollItem.SetDataProvider(index =>
            {
                if (index < _data.Count) return _data[index];
                return ($"Item #{index}", GenerateText(index));
            });

            // 估算值，真实尺寸由 IScrollItem.OnShow 内部测量
            ScrollList.OnGetItemSize = _ => 120f;

            ScrollList.OnReachTop    += OnReachEdgeTop;
            ScrollList.OnReachBottom += OnReachEdgeBottom;
            ScrollList.OnItemVisibilityChanged += (idx, visible) =>
            {
                if (TxtEdgeHint != null)
                    TxtEdgeHint.text = $"Item#{idx} {(visible ? "进入" : "离开")} | T={ScrollList.IsAtTop()} B={ScrollList.IsAtBottom()}";
            };

            ScrollList.Initialize(_data.Count);
            UpdateInfoText();

            // ---- 按钮 ----
            if (BtnAppend       != null) BtnAppend.onClick.AddListener(OnClickAppend);
            if (BtnPrepend      != null) BtnPrepend.onClick.AddListener(OnClickPrepend);
            if (BtnScrollBottom != null) BtnScrollBottom.onClick.AddListener(ScrollList.ScrollToBottom);
            if (BtnBatchAdd     != null) BtnBatchAdd.onClick.AddListener(OnClickBatchAdd);
            if (BtnExpandRandom != null) BtnExpandRandom.onClick.AddListener(OnClickExpandRandom);
            if (BtnRefreshItem  != null) BtnRefreshItem.onClick.AddListener(OnClickRefreshFirstVisible);
        }

        // ---- 边缘回调 ----
        private void OnReachEdgeTop()
        {
            Debug.Log("[AutoLayout-OSA] 到达顶部");
        }

        private void OnReachEdgeBottom()
        {
            Debug.Log("[AutoLayout-OSA] 到达底部");
        }

        // ---- 基础操作 ----
        private void OnClickAppend()
        {
            for (int i = 0; i < 5; i++)
                _data.Add(($"Item #{_data.Count}", GenerateText(_data.Count)));
            ScrollList.AppendData(5);
            UpdateInfoText();
        }

        private void OnClickPrepend()
        {
            for (int i = 0; i < 5; i++)
                _data.Insert(0, ($"Item #{i + 1000}", GenerateText(i + 1000)));
            ScrollList.PrependData(5);
            UpdateInfoText();
        }

        private void OnClickBatchAdd()
        {
            ScrollList.BeginUpdate();
            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < 5; i++)
                    _data.Add(($"Item #{_data.Count}", GenerateText(_data.Count)));
                ScrollList.AppendData(5);
            }
            ScrollList.EndUpdate();
            UpdateInfoText();
        }

        // ---- OSA-style: 动态尺寸变更 ----
        private void OnClickExpandRandom()
        {
            if (ScrollList.TotalCount == 0) return;
            int idx = 3;
            _expandedMode = !_expandedMode;

            // 改数据源 → 通知列表重新测量该 item
            if (_expandedMode)
                _data[idx] = (_data[idx].title, _data[idx].body + ExpandText);
            else
                _data[idx] = (_data[idx].title, _data[idx].body.Replace(ExpandText, ""));

            // 通知列表该 item 尺寸已变，自动重建累积位置
            ScrollList.RequestChangeItemSizeAndUpdateLayout(idx);
            UpdateInfoText();
        }

        private void OnClickRefreshFirstVisible()
        {
            if (ScrollList.FirstVisibleIndex < 0) return;
            int idx = ScrollList.FirstVisibleIndex;
            // 刷新该 item 的内容（触发 OnShow 重新填充+测量）
            ScrollList.RefreshItem(idx);
            UpdateInfoText();
        }

        // ---- 工具 ----
        private string GetRandomText(int seed)
        {
            var rng = new System.Random((int)(seed * 2654435761));
            return SampleTexts[rng.Next(SampleTexts.Length)];
        }

        private string GenerateText(int index) => GetRandomText(index);

        private void UpdateInfoText()
        {
            if (TxtInfo != null)
                TxtInfo.text = $"Total:{ScrollList.TotalCount} Vis:[{ScrollList.FirstVisibleIndex}..{ScrollList.LastVisibleIndex}] Expand:{(_expandedMode?"ON":"OFF")}";
        }

        private void Update()
        {
            if (Time.frameCount % 30 == 0) UpdateInfoText();
        }

        private void OnDestroy()
        {
            if (ScrollList != null)
                ScrollList.OnItemVisibilityChanged = null;
            if (BtnAppend       != null) BtnAppend.onClick.RemoveAllListeners();
            if (BtnPrepend      != null) BtnPrepend.onClick.RemoveAllListeners();
            if (BtnScrollBottom != null) BtnScrollBottom.onClick.RemoveAllListeners();
            if (BtnBatchAdd     != null) BtnBatchAdd.onClick.RemoveAllListeners();
            if (BtnExpandRandom != null) BtnExpandRandom.onClick.RemoveAllListeners();
            if (BtnRefreshItem  != null) BtnRefreshItem.onClick.RemoveAllListeners();
        }
    }
}

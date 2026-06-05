// ================================================================
// ScrollListDemo_AutoLayout.cs
// 演示 InfiniteIrregularScrollList 全部 27 个公开方法（不包括 DebugLogLayoutInfo）
// ================================================================

using System.Collections.Generic;
using EasyFramework;
using EasyFramework.Managers.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace EFExample
{
    public class ScrollListDemo_AutoLayout : MonoBehaviour
    {
        [Header("引用")]
        public ScrollListPro ScrollList;

        [Header("UI — 增删")] 
        public Button BtnAppend;
        public Button BtnPrepend;
        public Button BtnInsert;
        public Button BtnInsertRange;
        public Button BtnRemoveLast;
        public Button BtnRemoveAt;
        public Button BtnClear;

        [Header("UI — 批处理")]
        public Button BtnBatch;

        [Header("UI — 刷新")]
        public Button BtnRefreshVis;
        public Button BtnRefreshList;
        public Button BtnRefreshItem;
        public Button BtnRefreshRange;
        public Button BtnRefreshContent;
        public Button BtnScheduleTwinPass;
        public Button BtnSetTotalCount;

        [Header("UI — 尺寸")]
        public Button BtnExpand;
        public Button BtnUpdateSize;
        public Button BtnAnimateSize;

        [Header("UI — 滚动")]
        public Button BtnScrollTop;
        public Button BtnScrollBottom;
        public Button BtnScrollToIndex;
        public Button BtnScrollToIndexAnim;

        [Header("UI — 查询")]
        public Button BtnSnapshot;

        [Header("UI — 信息")]
        public Text TxtInfo;
        public Text TxtEdgeHint;
        
        [Header("UI — 输入框")]
        public InputField IptRemoveIndex;
        public InputField IptScrollToIndex;
        public InputField IptScrollToIndexAnimated;

        [Header("模拟数据")]
        public string[] SampleTexts = new string[]
        {
            "短文本",
            "这是一段中等长度的文本，用来模拟聊天消息的不同高度。",
            "这段文本比较长。当 VerticalLayoutGroup 和 ContentSizeFitter 协同工作时，item 的实际高度由子物体的累加高度决定。开启 Auto Rebuild Layout 后，组件会在 OnUpdateItem 填充完内容后自动调用 ForceRebuildLayoutImmediate 并测量真实高度，然后修正后续所有 item 的累积位置。",
            "超短",
            "又是一段中等长度的文本，演示自适应布局。",
        };

        public string ExpandText = " [ 展开演示数据 ] 这些内容会撑开高度，触发布局动画。";

        private bool _expanded;     // 展开
        private List<string> _data; // 实际数据

        private void Start()
        {
            if (ScrollList == null)
            {
                D.Error("未找到 InfiniteIrregularScrollList");
                return;
            }

            _data = new List<string>();
            for (int i = 0; i < 30; i++) _data.Add(GetRandomText(i));

            DemoScrollItem.GetData = idx => _data[idx];
            ScrollList.OnGetItemSize = _ => 120f;
            ScrollList.OnReachTop += () => D.Log("[边沿] 到达顶部");
            ScrollList.OnReachBottom += () => D.Log("[边沿] 到达底部");
            ScrollList.OnItemVisibilityChanged += (idx, v) =>
            {
                if (TxtEdgeHint)
                    TxtEdgeHint.text =$"#{idx}{(v ? "进" : "出")} | T={ScrollList.IsAtTop()} B={ScrollList.IsAtBottom()}";
            };

            ScrollList.Initialize(_data.Count);
            UpdateInfo();
            BindButtons();
        }

        private void Update()
        {
            if (Time.frameCount % 30 == 0) UpdateInfo();
        }

        private void OnDestroy()
        {
            if (ScrollList)
                ScrollList.OnItemVisibilityChanged = null;
            RemoveAllListeners();
        }

        private void BindButtons()
        {
            if (BtnAppend)            BtnAppend.onClick.AddListener(OnClickAppend);
            if (BtnPrepend)           BtnPrepend.onClick.AddListener(OnClickPrepend);
            if (BtnInsert)            BtnInsert.onClick.AddListener(OnClickInsert);
            if (BtnInsertRange)       BtnInsertRange.onClick.AddListener(OnClickInsertRange);
            if (BtnRemoveLast)        BtnRemoveLast.onClick.AddListener(OnClickRemoveLast);
            if (BtnRemoveAt)          BtnRemoveAt.onClick.AddListener(OnClickRemoveAt);
            if (BtnClear)             BtnClear.onClick.AddListener(OnClickClear);

            if (BtnBatch)             BtnBatch.onClick.AddListener(OnClickBatch);

            if (BtnRefreshVis)        BtnRefreshVis.onClick.AddListener(OnClickRefreshVis);
            if (BtnRefreshList)       BtnRefreshList.onClick.AddListener(OnClickRefreshList);
            if (BtnRefreshItem)       BtnRefreshItem.onClick.AddListener(OnClickRefreshItem);
            if (BtnRefreshRange)      BtnRefreshRange.onClick.AddListener(OnClickRefreshRange);
            if (BtnRefreshContent)    BtnRefreshContent.onClick.AddListener(OnClickRefreshContent);
            if (BtnScheduleTwinPass)  BtnScheduleTwinPass.onClick.AddListener(OnClickScheduleTwinPass);
            if (BtnSetTotalCount)     BtnSetTotalCount.onClick.AddListener(OnClickSetTotalCount);

            if (BtnExpand)            BtnExpand.onClick.AddListener(OnClickExpand);
            if (BtnUpdateSize)        BtnUpdateSize.onClick.AddListener(OnClickUpdateSize);
            if (BtnAnimateSize)       BtnAnimateSize.onClick.AddListener(OnClickAnimateSize);

            if (BtnScrollTop)         BtnScrollTop.onClick.AddListener(ScrollList.ScrollToTop);
            if (BtnScrollBottom)      BtnScrollBottom.onClick.AddListener(ScrollList.ScrollToBottom);
            if (BtnScrollToIndex)     BtnScrollToIndex.onClick.AddListener(OnClickScrollToIndex);
            if (BtnScrollToIndexAnim) BtnScrollToIndexAnim.onClick.AddListener(OnClickScrollToIndexAnim);

            if (BtnSnapshot)          BtnSnapshot.onClick.AddListener(OnClickSnapshot);
        }

        private void RemoveAllListeners()
        {
            BtnAppend?.onClick.RemoveAllListeners();
            BtnPrepend?.onClick.RemoveAllListeners();
            BtnInsert?.onClick.RemoveAllListeners();
            BtnInsertRange?.onClick.RemoveAllListeners();
            BtnRemoveLast?.onClick.RemoveAllListeners();
            BtnRemoveAt?.onClick.RemoveAllListeners();
            BtnClear?.onClick.RemoveAllListeners();
            BtnBatch?.onClick.RemoveAllListeners();
            BtnRefreshVis?.onClick.RemoveAllListeners();
            BtnRefreshList?.onClick.RemoveAllListeners();
            BtnRefreshItem?.onClick.RemoveAllListeners();
            BtnRefreshRange?.onClick.RemoveAllListeners();
            BtnRefreshContent?.onClick.RemoveAllListeners();
            BtnScheduleTwinPass?.onClick.RemoveAllListeners();
            BtnSetTotalCount?.onClick.RemoveAllListeners();
            BtnExpand?.onClick.RemoveAllListeners();
            BtnUpdateSize?.onClick.RemoveAllListeners();
            BtnAnimateSize?.onClick.RemoveAllListeners();
            BtnScrollTop?.onClick.RemoveAllListeners();
            BtnScrollBottom?.onClick.RemoveAllListeners();
            BtnScrollToIndex?.onClick.RemoveAllListeners();
            BtnScrollToIndexAnim?.onClick.RemoveAllListeners();
            BtnSnapshot?.onClick.RemoveAllListeners();
        }

        // ================================================================
        // 2. 增删 / Data Operations
        // ================================================================

        // AppendData
        private void OnClickAppend()
        {
            _data.Add(GetRandomText(_data.Count));
            ScrollList.AppendData(1);
            UpdateInfo();
        }

        // PrependData
        private void OnClickPrepend()
        {
            _data.Insert(0, GetRandomText(1000));
            ScrollList.PrependData(1);
            UpdateInfo();
        }

        // InsertAt
        private void OnClickInsert()
        {
            int idx = Mathf.Max(0, ScrollList.FirstVisibleIndex + 1);
            _data.Insert(idx, $"[Insert] {GetRandomText(idx)}");
            ScrollList.InsertAt(idx);
            UpdateInfo();
        }

        // InsertRange
        private void OnClickInsertRange()
        {
            int idx = Mathf.Max(0, ScrollList.FirstVisibleIndex);
            for (int i = 0; i < 3; i++) _data.Insert(idx, $"[InsertRange] {GetRandomText(idx + i)}");
            ScrollList.InsertRange(idx, 3);
            UpdateInfo();
        }

        // RemoveLast
        private void OnClickRemoveLast()
        {
            if (_data.Count == 0) return;
            if (!int.TryParse(IptRemoveIndex.text, out int idx) && idx >= 0 && idx < _data.Count)
                return;
            _data.RemoveAt(idx);
            ScrollList.RemoveLast();
            UpdateInfo();
        }

        // RemoveAt
        private void OnClickRemoveAt()
        {
            int idx = ScrollList.FirstVisibleIndex;
            if (idx < 0 || idx >= _data.Count) return;
            _data.RemoveAt(idx);
            ScrollList.RemoveAt(idx);
            UpdateInfo();
        }

        // Clear
        private void OnClickClear()
        {
            _data.Clear();
            ScrollList.Clear();
            D.Log("[Clear] 列表已清空。重新初始化 10 条数据。");
            for (int i = 0; i < 10; i++) _data.Add(GetRandomText(i));
            ScrollList.Initialize(_data.Count);
            UpdateInfo();
        }

        // ================================================================
        // 3. 批处理 / Batch
        // ================================================================

        private void OnClickBatch()
        {
            ScrollList.BeginUpdate();
            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < 5; i++) _data.Add(GetRandomText(_data.Count));
                ScrollList.AppendData(5);
            }

            ScrollList.EndUpdate(); // 一次性 FullRebuild
            UpdateInfo();
        }

        // ================================================================
        // 4. 刷新 / Refresh
        // ================================================================

        // RefreshVisibleItems — 强制刷新可见区
        private void OnClickRefreshVis()
        {
            ScrollList.RefreshVisibleItems(true);
            UpdateInfo();
        }

        // RefreshList — 全量重建尺寸 + 刷新
        private void OnClickRefreshList()
        {
            ScrollList.RefreshList();
            UpdateInfo();
        }

        // RefreshItem — 刷新单项
        private void OnClickRefreshItem()
        {
            int idx = ScrollList.FirstVisibleIndex;
            if (idx < 0) return;
            _data[idx] = "[刷新] " + GetRandomText(idx);
            ScrollList.RefreshItem(idx);
            UpdateInfo();
        }

        // RefreshRange — 刷新范围
        private void OnClickRefreshRange()
        {
            int from = ScrollList.FirstVisibleIndex;
            int to = Mathf.Min(from + 2, _data.Count - 1);
            if (from < 0) return;
            for (int i = from; i <= to; i++) _data[i] = "[Range刷新] " + GetRandomText(i);
            ScrollList.RefreshRange(from, to);
            UpdateInfo();
        }

        // RefreshContent — 仅刷新内容不重建
        private void OnClickRefreshContent()
        {
            ScrollList.RefreshContent();
            UpdateInfo();
        }

        // ScheduleComputeVisibilityTwinPass
        private void OnClickScheduleTwinPass()
        {
            ScrollList.ScheduleComputeVisibilityTwinPass();
            UpdateInfo();
        }

        // SetTotalCount
        private void OnClickSetTotalCount()
        {
            int newCount = Mathf.Max(5, _data.Count - 3);
            while (_data.Count > newCount) _data.RemoveAt(_data.Count - 1);
            ScrollList.SetTotalCount(newCount);
            UpdateInfo();
        }

        // ================================================================
        // 5. 尺寸变更 / Size Changes
        // ================================================================

        // RequestChangeItemSizeAndUpdateLayout — 重测当前尺寸
        private void OnClickExpand()
        {
            int idx = 3;
            _expanded = !_expanded;
            if (_expanded) _data[idx] += ExpandText;
            else _data[idx] = _data[idx].Replace(ExpandText, "");
            ScrollList.RequestChangeItemSizeAndUpdateLayout(idx);
            UpdateInfo();
        }

        // UpdateItemSize — 手动指定新尺寸
        private void OnClickUpdateSize()
        {
            int idx = 3;
            float newSize = _data[idx].Length > 50 ? 200f : 400f;
            _data[idx] = _data[idx].Length > 50
                ? "短"
                : "这段文本手动设了 400px 高度————— 但 CSF 不参与，纯粹靠 sizeDelta。";
            ScrollList.UpdateItemSize(idx, newSize);
            UpdateInfo();
        }

        // AnimateItemSize — 带动画的尺寸变更
        private void OnClickAnimateSize()
        {
            int idx = 3;
            float newSize = Random.Range(150f, 500f);
            D.Log($"[AnimateItemSize] idx={idx} → {newSize:F0}px");
            ScrollList.AnimateItemSize(idx, newSize);
            UpdateInfo();
        }

        // ================================================================
        // 6. 滚动 / Scroll
        // ================================================================

        // ScrollToIndex
        private void OnClickScrollToIndex()
        {
            if (!int.TryParse(IptScrollToIndex.text, out int idx) && idx >= 0 && idx < _data.Count)
                return;
            ScrollList.ScrollToIndex(idx, 0.5f); // 居中
            D.Log($"[ScrollToIndex] idx={idx} alignment=0.5");
        }

        // ScrollToIndexAnimated
        private void OnClickScrollToIndexAnim()
        {
            if (!int.TryParse(IptScrollToIndexAnimated.text, out int idx) && idx >= 0 && idx < _data.Count)
                return;
            ScrollList.ScrollToIndexAnimated(idx, 0f, 0.4f); // 顶部对齐，0.4s
            D.Log($"[ScrollToIndexAnimated] idx={idx} dur=0.4s");
        }

        // ================================================================
        // 7. 查询 / Query
        // ================================================================

        private void OnClickSnapshot()
        {
            float pos = ScrollList.GetNormalizedPosition();
            D.Log(
                $"[快照] Total={ScrollList.TotalCount} Vis=[{ScrollList.FirstVisibleIndex}..{ScrollList.LastVisibleIndex}] Norm={pos:F3} IsTop={ScrollList.IsAtTop()} IsBot={ScrollList.IsAtBottom()}");
        }

        // ================================================================
        // 工具
        // ================================================================

        private string GetRandomText(int seed)
        {
            var rng = new System.Random((int)(seed * 2654435761));
            return SampleTexts[rng.Next(SampleTexts.Length)];
        }

        private void UpdateInfo()
        {
            if (TxtInfo)
                TxtInfo.text =
                    $"Total:{ScrollList.TotalCount} Vis:[{ScrollList.FirstVisibleIndex}..{ScrollList.LastVisibleIndex}] Expand:{(_expanded ? "ON" : "OFF")}";
        }
    }
}
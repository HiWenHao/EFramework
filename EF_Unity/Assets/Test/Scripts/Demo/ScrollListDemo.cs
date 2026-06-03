// ================================================================
// ScrollListDemo.cs
// InfiniteIrregularScrollList 使用演示
// ================================================================

using UnityEngine;
using UnityEngine.UI;

namespace EFExample
{
    /// <summary>
    /// 演示 InfiniteIrregularScrollList 的全部功能。
    /// 挂在场景中的任意 GameObject 上，拖入对应的 ScrollRect 和 Item Prefab 即可运行。
    /// </summary>
    public class ScrollListDemo : MonoBehaviour
    {
        [Header("引用")]
        public InfiniteIrregularScrollList ScrollList;
        public GameObject ItemPrefab;

        [Header("UI（可选）")]
        public Button BtnRefresh;
        public Button BtnAppend;
        public Button BtnPrepend;
        public Button BtnRemoveLast;
        public Button BtnScrollTop;
        public Button BtnScrollBottom;
        public Button BtnRandomJump;
        public Button BtnInsertMid;
        public Text   TxtInfo;
        public Text   TxtEdgeHint;

        [Header("参数")]
        [Min(1)] public int InitialCount = 50;
        public float MinItemSize = 60f;
        public float MaxItemSize = 300f;
        public float AnimateScrollDuration = 0.35f;

        private void Start()
        {
            // 若 Inspector 未赋值，尝试自动查找
            if (ScrollList == null)
                ScrollList = GetComponent<InfiniteIrregularScrollList>();
            if (ScrollList == null)
                ScrollList = FindObjectOfType<InfiniteIrregularScrollList>();

            if (ScrollList == null)
            {
                Debug.LogError("[ScrollListDemo] 未找到 InfiniteIrregularScrollList，请在 Inspector 中拖入。");
                return;
            }

            // 绑定回调
            ScrollList.OnUpdateItem  = OnUpdateItem;
            ScrollList.OnGetItemSize = OnGetItemSize;

            // ---- 边缘回调：演示自动加载更多 ----
            ScrollList.OnReachTop    += () => Debug.Log("[ScrollListDemo] 到达顶部，可加载更早消息");
            ScrollList.OnReachBottom += () => Debug.Log("[ScrollListDemo] 到达底部，可加载更多");

            // ---- 可见性回调 ----
            ScrollList.OnItemVisibilityChanged += OnItemVisibilityChanged;

            // 初始化
            ScrollList.Initialize(InitialCount);
            UpdateInfoText();

            // 按钮事件
            if (BtnRefresh     != null) BtnRefresh.onClick.AddListener(OnClickRefresh);
            if (BtnAppend      != null) BtnAppend.onClick.AddListener(OnClickAppend);
            if (BtnPrepend     != null) BtnPrepend.onClick.AddListener(OnClickPrepend);
            if (BtnRemoveLast  != null) BtnRemoveLast.onClick.AddListener(OnClickRemoveLast);
            if (BtnScrollTop   != null) BtnScrollTop.onClick.AddListener(() => ScrollList.ScrollToTop());
            if (BtnScrollBottom!= null) BtnScrollBottom.onClick.AddListener(() => ScrollList.ScrollToBottom());
            if (BtnRandomJump  != null) BtnRandomJump.onClick.AddListener(OnClickRandomJump);
            if (BtnInsertMid   != null) BtnInsertMid.onClick.AddListener(OnClickInsertMid);
        }

        // ---- 回调 ----

        private float OnGetItemSize(int index)
        {
            // 随机尺寸，模拟不规则列表
            // 使用 index 做种子保证同一次运行中尺寸稳定
            var state = (uint)(index * 2654435761u); // Knuth multiplicative hash
            float t = (state % 1000) / 1000f;
            return Mathf.Lerp(MinItemSize, MaxItemSize, t);
        }

        private void OnUpdateItem(GameObject go, int index)
        {
            // 查找子节点中的 Text
            var label = go.GetComponentInChildren<Text>();
            if (label != null)
            {
                float size = OnGetItemSize(index);
                label.text = $"Item #{index}\nSize: {size:F0}";
            }

            // 按奇偶换背景色
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                img.color = (index % 2 == 0)
                    ? new Color(0.18f, 0.18f, 0.22f)
                    : new Color(0.22f, 0.22f, 0.26f);
            }
        }

        // ---- 按钮 ----

        private void OnClickRefresh()
        {
            ScrollList.RefreshList();
            UpdateInfoText();
        }

        private void OnClickAppend()
        {
            ScrollList.AppendData(20);
            UpdateInfoText();
        }

        private void OnClickPrepend()
        {
            ScrollList.PrependData(20);
            UpdateInfoText();
        }

        private void OnClickRemoveLast()
        {
            ScrollList.RemoveLast();
            UpdateInfoText();
        }

        private void OnClickRandomJump()
        {
            if (ScrollList.TotalCount == 0) return;
            int idx = Random.Range(0, ScrollList.TotalCount);
            ScrollList.ScrollToIndexAnimated(idx, 0.5f, AnimateScrollDuration);
        }

        private void OnClickInsertMid()
        {
            int mid = ScrollList.TotalCount / 2;
            ScrollList.InsertAt(mid);
            UpdateInfoText();
        }

        private void OnItemVisibilityChanged(int index, bool visible)
        {
            // 可用于播放进出动画、资源管理、统计曝光等
            if (TxtEdgeHint != null)
                TxtEdgeHint.text = $"{index} → {(visible ? "进入" : "离开")} | Edge:Top={ScrollList.IsAtTop()} Bot={ScrollList.IsAtBottom()}";
        }

        private void UpdateInfoText()
        {
            if (TxtInfo != null)
                TxtInfo.text = $"Total: {ScrollList.TotalCount}  Visible: [{ScrollList.FirstVisibleIndex}..{ScrollList.LastVisibleIndex}]";
        }

        private void Update()
        {
            // 实时更新 info（降低频率用每 10 帧更新）
            if (Time.frameCount % 30 == 0)
                UpdateInfoText();
        }

        private void OnDestroy()
        {
            if (ScrollList != null)
            {
                ScrollList.OnItemVisibilityChanged = null;
            }

            if (BtnRefresh     != null) BtnRefresh.onClick.RemoveAllListeners();
            if (BtnAppend      != null) BtnAppend.onClick.RemoveAllListeners();
            if (BtnPrepend     != null) BtnPrepend.onClick.RemoveAllListeners();
            if (BtnRemoveLast  != null) BtnRemoveLast.onClick.RemoveAllListeners();
            if (BtnScrollTop   != null) BtnScrollTop.onClick.RemoveAllListeners();
            if (BtnScrollBottom!= null) BtnScrollBottom.onClick.RemoveAllListeners();
            if (BtnRandomJump  != null) BtnRandomJump.onClick.RemoveAllListeners();
            if (BtnInsertMid   != null) BtnInsertMid.onClick.RemoveAllListeners();
        }
    }
}

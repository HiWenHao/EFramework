// ================================================================
// InfiniteIrregularScrollList.cs
// 无限不规则滚动列表 — 虚拟滚动 + 对象池，支持不等高/不等宽 item
// ================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace EFExample
{
    /// <summary>
    /// /// 无限不规则滚动列表。
    /// 基于 Unity ScrollRect 实现虚拟滚动（只渲染可视区域内的 item），
    /// 每个 item 可拥有独立的高度/宽度，支持运行时动态增删和双向无限加载。
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    [DisallowMultipleComponent]
    public class InfiniteIrregularScrollList : MonoBehaviour
    {
        [Header("引用")] [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private GameObject _itemPrefab;

        [Header("布局")] [SerializeField] private Direction _direction = Direction.Vertical;
        [SerializeField] [Min(0)] private float _itemSpacing = 0f;
        [SerializeField] [Min(1)] private int _poolPreAlloc = 3;
        [SerializeField] [Min(0)] private int _bufferCount = 2;

        [Header("预创建")] [Tooltip("在可视缓冲外提前创建并测量 item 的数量。\nitem 在进入视口之前就已布局锁定，避免滑动时卡顿。")] [SerializeField] [Min(0)]
        private int _preCreateBuffer = 3;

        [Header("自适应测量")]
        [Tooltip(
            "开启后，OnUpdateItem 填充完内容会自动 ForceRebuildLayoutImmediate 并测量真实尺寸，\n无需在 OnGetItemSize 中精确计算。OnGetItemSize 只需返回合理估算值即可。")]
        [SerializeField]
        private bool _autoRebuildLayout = false;

        [Header("动画")]
        [Tooltip("item 尺寸变更时的过渡动画时长（秒）。0 = 无动画，直接瞬变。")]
        [SerializeField][Range(0f, 1f)]
        private float _layoutAnimationDuration = 0.15f;

        [Header("吸附")]
        [Tooltip("滚动停止时自动吸附到最近 item 的顶部/中间/底部。None = 不吸附。")]
        [SerializeField]
        private SnapAlignment _snapAlignment = SnapAlignment.None;

        [SerializeField][Range(0.5f, 50f)]
        private float _snapVelocityThreshold = 5f;

        [SerializeField][Range(0.05f, 0.5f)]
        private float _snapDuration = 0.15f;

        // ================================================================
        // 公开回调
        // ================================================================

        /// <summary>
        /// 填充 item 内容
        /// <para>Fill item content. 参数：GameObject 实例、数据索引</para>
        /// </summary>
        public Action<GameObject, int> OnUpdateItem;

        /// <summary>
        /// 估算第 index 个 item 的尺寸
        /// <para>Estimate item size at index. 不设置则用 prefab 默认尺寸</para>
        /// </summary>
        public Func<int, float> OnGetItemSize;

        /// <summary>
        /// item 进入/离开可视区
        /// <para>Visibility changed callback. 参数：数据索引、是否进入(true)/离开(false)</para>
        /// </summary>
        public Action<int, bool> OnItemVisibilityChanged;

        /// <summary>
        /// 滚动到顶部边缘
        /// <para>Reached top edge. 用于"加载更早消息"等场景</para>
        /// </summary>
        public event Action OnReachTop;

        /// <summary>
        /// 滚动到底部边缘
        /// <para>Reached bottom edge. 用于"加载更多"等场景</para>
        /// </summary>
        public event Action OnReachBottom;

        // ================================================================
        // 公开属性
        // ================================================================

        /// <summary>
        /// 当前数据总量
        /// <para>Total item count</para>
        /// </summary>
        public int TotalCount => _itemSizes.Count;

        /// <summary>
        /// 当前可视区第一个 item 的索引
        /// <para>First visible index (-1 = none)</para>
        /// </summary>
        public int FirstVisibleIndex { get; private set; } = -1;

        /// <summary>
        /// 当前可视区最后一个 item 的索引
        /// <para>Last visible index (-1 = none)</para>
        /// </summary>
        public int LastVisibleIndex { get; private set; } = -1;

        /// <summary>
        /// 是否已初始化
        /// <para>Whether initialized</para>
        /// </summary>
        public bool IsInitialized { get; private set; }

        // ================================================================
        // 私有状态 / Private state
        // ================================================================

        private readonly List<float> _itemSizes = new List<float>(); // 每个 item 的尺寸缓存 / Per-item size cache

        private readonly List<float>
            _cumulativePositions =
                new List<float>(); // 预计算累积位置（OSA），[i]=item[i] 顶部偏移 / Pre-computed cumulative positions

        private readonly List<ActiveItem> _activeItems = new List<ActiveItem>(); // 当前活跃 item 列表 / Active items

        private readonly Dictionary<int, GameObject>
            _activeIndexMap = new Dictionary<int, GameObject>(); // dataIndex→GameObject O(1)查重 / Index lookup map

        private readonly Stack<GameObject> _pool = new Stack<GameObject>(); // 对象池 / Object pool

        private RectTransform _contentRect; // ScrollRect.content 的 RectTransform / Content rect
        private RectTransform _viewportRect; // 视口 RectTransform / Viewport rect
        private float _viewportSize; // 视口高度/宽度 / Viewport size (h or w)
        private float _prefabDefaultSize; // prefab 默认尺寸 / Prefab default size
        private int _estimatedVisibleCount; // 估算的可视 item 数量 / Estimated visible count
        private bool _scrollListenerAdded; // 是否已注册 scroll 监听 / Scroll listener added flag

        // ---- 批处理 / Batched operations ----
        private int _batchDepth; // 批处理嵌套深度 / Batch nesting depth
        private bool _batchDirty; // 批处理中是否有脏数据 / Dirty flag in batch

        // ---- 边缘检测 / Edge detection ----
        private bool _wasAtTop; // 上一帧是否在顶部 / Was at top in prev frame
        private bool _wasAtBottom; // 上一帧是否在底部 / Was at bottom in prev frame

        // ---- 平滑滚动 / Smooth scroll ----
        private CancellationTokenSource _scrollAnimCts; // 滚动动画取消令牌 / Scroll animation CTS
        private CancellationTokenSource _layoutAnimCts; // 布局动画取消令牌 / Layout animation CTS

        // ---- 防重入 / Re-entrancy guard ----
        private bool _refreshing; // 正在刷新中 / Refreshing flag

        // ================================================================
        // ================================================================

        public enum Direction
        {
            Vertical,
            Horizontal
        }

        /// <summary>
        /// 吸附对齐方式
        /// <para>Snap alignment mode.</para>
        /// </summary>
        public enum SnapAlignment
        {
            None,
            Start,
            Center,
            End
        }

        // 活跃 item 结构 / Active item struct
        private struct ActiveItem
        {
            public GameObject gameObject; // item GameObject / The item GO
            public IScrollItem scrollItem; // 缓存 IScrollItem 避免重复 GetComponent / Cached interface
            public int index; // 数据索引 / Data index
            public float size; // 当前尺寸 / Current size
            public bool justCreated; // 是否本帧新建，需要填充+测量 / Needs fill+measure this frame
        }

        // ================================================================
        // 生命周期
        // ================================================================

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            if (_scrollRect != null && !_scrollListenerAdded)
            {
                _scrollRect.onValueChanged.AddListener(OnScrollChanged);
                _scrollListenerAdded = true;
            }

            if (IsInitialized && _activeItems.Count == 0)
                RefreshVisibleItems(true);
        }

        private void OnDisable()
        {
            if (_scrollRect != null && _scrollListenerAdded)
            {
                _scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
                _scrollListenerAdded = false;
            }

            CancelAllAnimations();
        }

        private void OnDestroy()
        {
            if (_scrollRect != null && _scrollListenerAdded)
                _scrollRect.onValueChanged.RemoveListener(OnScrollChanged);

            CancelAllAnimations();
            Clear();
        }

        private void CancelAllAnimations()
        {
            _scrollAnimCts?.Cancel();
            _scrollAnimCts?.Dispose();
            _scrollAnimCts = null;
            _layoutAnimCts?.Cancel();
            _layoutAnimCts?.Dispose();
            _layoutAnimCts = null;
            _snapDebounceCts?.Cancel();
            _snapDebounceCts?.Dispose();
            _snapDebounceCts = null;
        }

        private void StopLayoutAnimation()
        {
            _layoutAnimCts?.Cancel();
            _layoutAnimCts?.Dispose();
            _layoutAnimCts = null;
        }

        // ================================================================
        // 初始化
        // ================================================================

        /// <summary>
        /// /// 初始化列表
        /// <para>Initialize the list.
        /// 首次设置数据后调用，或数量变更后调用 RefreshList。
        /// Call after first setting data, or use RefreshList after count changes.
        ///</para>
        /// </summary>
        /// <param name="totalCount">数据总量 / Total data count</param>
        public void Initialize(int totalCount)
        {
            EnsureReferences();
            ClearAll();

            for (int i = 0; i < totalCount; i++)
                _itemSizes.Add(GetItemSize(i));

            RebuildCumulativePositions();
            ApplyContentLayout(false);
            PreAllocPool();
            IsInitialized = true;
            RefreshVisibleItems(true);

            // 初始化边缘状态
            _wasAtTop = IsAtTop();
            _wasAtBottom = IsAtBottom();
        }

        private void EnsureReferences()
        {
            if (_scrollRect == null)
                _scrollRect = GetComponent<ScrollRect>();

            _contentRect = _scrollRect.content;
            if (_contentRect == null)
            {
                Debug.LogError("[InfiniteIrregularScrollList] ScrollRect.Content is null.", this);
                return;
            }

            _viewportRect = _scrollRect.viewport != null
                ? _scrollRect.viewport
                : _scrollRect.GetComponent<RectTransform>();

            _viewportSize = GetViewportSize();

            if (_itemPrefab != null)
            {
                var pfRt = _itemPrefab.transform as RectTransform;
                if (pfRt != null)
                    _prefabDefaultSize = (_direction == Direction.Vertical)
                        ? pfRt.rect.height
                        : pfRt.rect.width;
            }

            // ---- 防御：剥离 content 上的 LayoutGroup / ContentSizeFitter ----
            // 这些组件会覆盖我们手动计算的 sizeDelta，导致顶部空白 + 底部截断。
            StripConflictingLayoutComponents(_contentRect);
        }

        /// <summary>
        /// /// 移除 content 上的 LayoutGroup、ContentSizeFitter 和 AspectRatioFitter。
        /// 本组件完全手动管理 content 的布局，任何自动布局组件都会产生冲突。
        /// 注意：Unity 的 Destroy() 是延迟执行的（帧末才真正移除），
        ///       因此必须先 enabled=false 立即禁用，防止同帧 Initialize 时组件仍生效。
        ///
        /// </summary>
        private static void StripConflictingLayoutComponents(RectTransform content)
        {
            bool stripped = false;

            var csf = content.GetComponent<ContentSizeFitter>();
            if (csf != null)
            {
                csf.enabled = false; // 立即禁用，确保同帧不生效
                Destroy(csf); // 延迟销毁，下帧清理
                stripped = true;
            }

            var layout = content.GetComponent<LayoutGroup>();
            if (layout != null)
            {
                layout.enabled = false;
                Destroy(layout);
                stripped = true;
            }

            // AspectRatioFitter 也会覆盖 sizeDelta
            var arf = content.GetComponent<AspectRatioFitter>();
            if (arf != null)
            {
                arf.enabled = false;
                Destroy(arf);
                stripped = true;
            }

            if (stripped)
                Debug.LogWarning(
                    "[InfiniteIrregularScrollList] Content 上的 LayoutGroup/ContentSizeFitter/AspectRatioFitter 已自动移除。" +
                    "本组件手动管理 content 布局，这些组件会导致定位异常。");
        }

        private float GetItemSize(int index)
        {
            if (OnGetItemSize != null)
                return Mathf.Max(1f, OnGetItemSize(index));
            return _prefabDefaultSize;
        }

        // ================================================================
        // Content 布局设置
        // ================================================================

        private void ApplyContentLayout(bool preserveScroll = true)
        {
            float total = CalculateTotalSize();

            // 保存当前滚动偏移，避免 sizeDelta 变更时 ScrollRect 自动钳位丢失位置
            float savedOffset = preserveScroll ? GetContentScrollOffset() : 0f;

            if (_direction == Direction.Vertical)
            {
                _contentRect.anchorMin = new Vector2(0, 1);
                _contentRect.anchorMax = new Vector2(1, 1);
                _contentRect.pivot = new Vector2(0.5f, 1);
                _contentRect.sizeDelta = new Vector2(_contentRect.sizeDelta.x, total);

                // 恢复滚动位置（钳位到新的有效范围）
                if (preserveScroll)
                {
                    float maxOff = Mathf.Max(0, total - _viewportSize);
                    _contentRect.anchoredPosition = new Vector2(
                        _contentRect.anchoredPosition.x,
                        Mathf.Clamp(savedOffset, 0, maxOff));
                }
                else
                {
                    _contentRect.anchoredPosition = Vector2.zero;
                }
            }
            else
            {
                _contentRect.anchorMin = new Vector2(0, 1);
                _contentRect.anchorMax = new Vector2(0, 1);
                _contentRect.pivot = new Vector2(0, 0.5f);
                _contentRect.sizeDelta = new Vector2(total, _contentRect.sizeDelta.y);

                if (preserveScroll)
                {
                    float maxOff = Mathf.Max(0, total - _viewportSize);
                    _contentRect.anchoredPosition = new Vector2(
                        -Mathf.Clamp(savedOffset, 0, maxOff),
                        _contentRect.anchoredPosition.y);
                }
                else
                {
                    _contentRect.anchoredPosition = Vector2.zero;
                }
            }
        }

        private float CalculateTotalSize()
        {
            float total = 0f;
            for (int i = 0; i < _itemSizes.Count; i++)
            {
                float s = _itemSizes[i];
                // 未测量过的 item 用 OnGetItemSize 估算，避免 total = 0 导致滚动范围错误
                if (s <= 0.5f) s = GetItemSize(i);
                total += s;
                if (i < _itemSizes.Count - 1) total += _itemSpacing;
            }

            return total;
        }

        /// <summary>
        /// /// 计算从索引 0 到 index（含）的累积偏移（含间距），O(1) 查表，未初始化时线性 fallback
        ///
        /// </summary>
        private float GetCumulativePosition(int index)
        {
            if (index < _cumulativePositions.Count)
                return _cumulativePositions[index];
            // 防御：_itemSizes 新增但 _cumulativePositions 尚未重建时，回退到线性计算
            float pos = 0f;
            for (int i = 0; i < index && i < _itemSizes.Count; i++)
                pos += _itemSizes[i] + _itemSpacing;
            return pos;
        }

        /// <summary>
        /// /// 重建累积位置数组（OSA 风格）。_itemSizes 变更后调用，fromIndex=0 表示全量重建。
        ///
        /// </summary>
        private void RebuildCumulativePositions(int fromIndex = 0)
        {
            if (_cumulativePositions.Count != _itemSizes.Count)
            {
                _cumulativePositions.Clear();
                for (int i = 0; i < _itemSizes.Count; i++)
                    _cumulativePositions.Add(0f);
            }

            if (_cumulativePositions.Count == 0) return;

            if (fromIndex <= 0)
            {
                _cumulativePositions[0] = 0f;
                fromIndex = 1;
            }

            for (int i = fromIndex; i < _cumulativePositions.Count; i++)
            {
                float prevSize = _itemSizes[i - 1];
                if (prevSize <= 0.5f) prevSize = GetItemSize(i - 1);
                _cumulativePositions[i] = _cumulativePositions[i - 1] + prevSize + _itemSpacing;
            }
        }

        /// <summary>
        /// /// 二分查找第一个可见 item 索引。返回第一个结束位置 > scrollOffset 的索引。
        ///
        /// </summary>
        private int BinarySearchVisibleIndex(float scrollOffset)
        {
            int lo = 0, hi = _cumulativePositions.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                float itemEnd = _cumulativePositions[mid] + _itemSizes[mid] + _itemSpacing;
                if (itemEnd <= scrollOffset)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }

            return Mathf.Min(lo, _cumulativePositions.Count - 1);
        }

        // ================================================================
        // 可视区域计算
        // ================================================================

        private float GetViewportSize()
        {
            if (_viewportRect == null) return 0f;
            return _direction == Direction.Vertical
                ? _viewportRect.rect.height
                : _viewportRect.rect.width;
        }

        private float GetContentScrollOffset()
        {
            return _direction == Direction.Vertical
                ? _contentRect.anchoredPosition.y
                : -_contentRect.anchoredPosition.x;
        }

        private (int firstIndex, int lastIndex) CalculateVisibleRange()
        {
            int count = _itemSizes.Count;
            if (count == 0) return (-1, -1);

            _viewportSize = GetViewportSize();

            float rawOffset = GetContentScrollOffset();
            float maxScroll = Mathf.Max(0, CalculateTotalSize() - _viewportSize);
            float scrollOffset = Mathf.Clamp(rawOffset, 0, maxScroll);

            // ---- 二分查找第一个可见 item（OSA 风格） ----
            int first = BinarySearchVisibleIndex(scrollOffset);

            // 加缓冲
            int totalBuffer = _bufferCount + _preCreateBuffer;
            first = Mathf.Max(0, first - totalBuffer);

            // ---- 查找最后一个可见 item ----
            float visibleEnd = scrollOffset + _viewportSize;
            int last = first;
            for (int i = first; i < count; i++)
            {
                float itemEnd = _cumulativePositions[i] + _itemSizes[i] + _itemSpacing;
                last = i;
                if (itemEnd >= visibleEnd) break;
            }

            last = Mathf.Min(count - 1, last + totalBuffer);
            return (first, last);
        }

        // ================================================================
        // Item 渲染
        // ================================================================

        private void OnScrollChanged(Vector2 _)
        {
            // 用户拖动时立即终止动画，避免视觉冲突
            if (_scrollRect != null && _scrollRect.velocity.sqrMagnitude > 10f)
                StopLayoutAnimation();
            RefreshVisibleItems(false);
            CheckEdgeReached();
            CheckSnap();
        }
        private CancellationTokenSource _snapDebounceCts; // 吸附防抖令牌 / Snap debounce CTS

        private void CheckSnap()
        {
            if (_snapAlignment == SnapAlignment.None || _scrollRect == null) return;

            float vel = _direction == Direction.Vertical
                ? Mathf.Abs(_scrollRect.velocity.y)
                : Mathf.Abs(_scrollRect.velocity.x);

            if (vel > _snapVelocityThreshold)
            {
                // 仍在滚动 → 取消上次防抖
                _snapDebounceCts?.Cancel();
                return;
            }

            // 速度低于阈值 → 启动防抖，等惯性彻底停下再吸附
            if (_snapDebounceCts == null && !_refreshing)
            {
                var cts = new CancellationTokenSource();
                _snapDebounceCts = cts;
                SnapDebounceAsync(cts.Token).Forget();
            }
        }

        private async UniTaskVoid SnapDebounceAsync(CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.12), cancellationToken: ct);
            if (!ct.IsCancellationRequested)
                PerformSnap();
            _snapDebounceCts?.Dispose();
            _snapDebounceCts = null;
        }

        private void PerformSnap()
        {
            if (!IsInitialized || _itemSizes.Count == 0) return;

            float alignment = _snapAlignment switch
            {
                SnapAlignment.Start  => 0f,
                SnapAlignment.Center => 0.5f,
                SnapAlignment.End    => 1f,
                _ => float.NaN
            };
            if (float.IsNaN(alignment)) return;

            // 找到最接近视口对齐点的 item
            float viewAnchor = GetContentScrollOffset() + GetViewportSize() * alignment;
            int bestIdx = -1;
            float bestDist = float.MaxValue;

            for (int i = FirstVisibleIndex; i <= LastVisibleIndex; i++)
            {
                if (i < 0 || i >= _cumulativePositions.Count) continue;
                float itemAnchor = _cumulativePositions[i] + _itemSizes[i] * alignment;
                float dist = Mathf.Abs(itemAnchor - viewAnchor);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            if (bestIdx >= 0)
            {
                float target = GetCumulativePosition(bestIdx) + _itemSizes[bestIdx] * alignment - GetViewportSize() * alignment;
                float total = CalculateTotalSize();
                float maxOff = Mathf.Max(0, total - GetViewportSize());
                float clamped = Mathf.Clamp(target, 0, maxOff);

                var cts = new CancellationTokenSource();
                _scrollAnimCts?.Cancel();
                _scrollAnimCts = cts;
                AnimateScrollAsync(GetContentScrollOffset(), clamped, _snapDuration, null, cts.Token).Forget();
            }
        }

        private void CheckEdgeReached()
        {
            bool atTop = IsAtTop();
            bool atBottom = IsAtBottom();

            if (!_wasAtTop && atTop)
                OnReachTop?.Invoke();

            if (!_wasAtBottom && atBottom)
                OnReachBottom?.Invoke();

            _wasAtTop = atTop;
            _wasAtBottom = atBottom;
        }

        /// <summary>
        /// /// 刷新可视区 item。
        ///
        /// </summary>
        /// <param name="force">true 时跳过范围比较，强制全部重建</param>
        public void RefreshVisibleItems(bool force = false)
        {
            if (!IsInitialized || _itemSizes.Count == 0 || _refreshing) return;
            _refreshing = true;
            try
            {
                RefreshVisibleItemsInternal(force);
            }
            finally
            {
                _refreshing = false;
            }
        }

        private void RefreshVisibleItemsInternal(bool force)
        {
            if (!IsInitialized || _itemSizes.Count == 0) return;

            // ---- 始终同步 content sizeDelta（AppendData 后即使范围不变也要更新） ----
            if (_autoRebuildLayout)
                SyncContentSizeDelta();

            var (newFirst, newLast) = CalculateVisibleRange();
            if (newFirst < 0) return;

            // force 或 范围变化 才重建
            if (!force && newFirst == FirstVisibleIndex && newLast == LastVisibleIndex)
                return;

            // 回收不再可见的 item
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                var item = _activeItems[i];
                if (item.index < newFirst || item.index > newLast)
                {
                    _activeIndexMap.Remove(item.index);
                    OnItemVisibilityChanged?.Invoke(item.index, false);
                    item.scrollItem?.OnHide();
                    Recycle(item.gameObject);
                    _activeItems.RemoveAt(i);
                }
            }

            // 为缺失索引补齐 item
            for (int i = newFirst; i <= newLast; i++)
            {
                if (!IsActiveAt(i))
                {
                    var go = Rent();
                    go.SetActive(false); // 先隐藏，填充完再显示
                    var si = go.GetComponent<IScrollItem>();
                    _activeItems.Add(new ActiveItem
                        { gameObject = go, scrollItem = si, index = i, size = _itemSizes[i], justCreated = true });
                    _activeIndexMap[i] = go;
                    OnItemVisibilityChanged?.Invoke(i, true);
                }
            }

            // ---- 保存锚点（在填充前记录，填充后 justCreated 全被清除无法区分） ----
            int anchorIdx = -1;
            for (int i = 0; i < _activeItems.Count; i++)
            {
                var ai = _activeItems[i];
                if (ai.gameObject != null && !ai.justCreated && ai.index >= newFirst && ai.index <= newLast)
                {
                    anchorIdx = ai.index;
                    break;
                }
            }

            if (anchorIdx < 0) anchorIdx = newFirst; // 首次加载无已活跃 item
            float oldAnchorPos = anchorIdx < _itemSizes.Count ? GetCumulativePosition(anchorIdx) : 0f;

            // ---- 在填充之前禁用 content 布局（防冒泡篡改 item 位置） ----
            if (_autoRebuildLayout)
            {
                var contentLayout = _contentRect.GetComponent<LayoutGroup>();
                if (contentLayout != null) contentLayout.enabled = false;
                var contentCsf = _contentRect.GetComponent<ContentSizeFitter>();
                if (contentCsf != null) contentCsf.enabled = false;
                var contentArf = _contentRect.GetComponent<AspectRatioFitter>();
                if (contentArf != null) contentArf.enabled = false;
            }

            // ---- 填充新 item（FillAndMeasureNew 内完成测量、锁定） ----
            bool anyFilled = false;
            for (int i = 0; i < _activeItems.Count; i++)
            {
                var ai = _activeItems[i];
                if (ai.justCreated)
                {
                    _activeItems[i] = FillAndMeasureNew(ai);
                    anyFilled = true;
                }
            }

            // ---- Batch flush + 重建累积位置 ----
            if (anyFilled)
            {
                Canvas.ForceUpdateCanvases();
                RebuildCumulativePositions();
            }

            // ---- 补偿顶部尺寸变化导致的视觉偏移 ----
            if (anyFilled && anchorIdx >= 0 && anchorIdx < _itemSizes.Count)
            {
                float newAnchorPos = GetCumulativePosition(anchorIdx);
                float delta = newAnchorPos - oldAnchorPos;
                if (Mathf.Abs(delta) > 0.5f)
                {
                    if (_direction == Direction.Vertical)
                        _contentRect.anchoredPosition = new Vector2(_contentRect.anchoredPosition.x,
                            _contentRect.anchoredPosition.y + delta);
                    else
                        _contentRect.anchoredPosition = new Vector2(_contentRect.anchoredPosition.x - delta,
                            _contentRect.anchoredPosition.y);
                }
            }

            // ---- 验证 content sizeDelta ----
            if (anyFilled && _autoRebuildLayout)
            {
                float expectedTotal = CalculateTotalSize();
                if (_direction == Direction.Vertical)
                {
                    if (Mathf.Abs(_contentRect.sizeDelta.y - expectedTotal) > 0.5f)
                        _contentRect.sizeDelta = new Vector2(_contentRect.sizeDelta.x, expectedTotal);
                }
                else
                {
                    if (Mathf.Abs(_contentRect.sizeDelta.x - expectedTotal) > 0.5f)
                        _contentRect.sizeDelta = new Vector2(expectedTotal, _contentRect.sizeDelta.y);
                }
            }

            // ---- OSA 风格定位：直接从预计算累积位置数组 O(1) 查表 ----
            // 不再需要种子锚点链，所有 item 的 anchoredPosition 由 _cumulativePositions 统一决定。
            for (int i = newFirst; i <= newLast; i++)
            {
                var go = FindActiveByIndex(i);
                if (go == null) continue;
                var rt = go.transform as RectTransform;
                if (rt == null) continue;
                float pos = _cumulativePositions[i];
                rt.anchoredPosition = (_direction == Direction.Vertical)
                    ? new Vector2(0, -pos)
                    : new Vector2(pos, 0);
            }

            FirstVisibleIndex = newFirst;
            LastVisibleIndex = newLast;

            // 定位后再次同步 content 尺寸（防 ScrollRect 篡改）
            SyncContentSizeDelta();

            // ---- 最终保护：flush + 强制修正漂移 ----
            // 双 flush 确保 ForceRebuildLayoutImmediate 冒泡 dirty 在同一帧全部结算。
            if (anyFilled)
            {
                Canvas.ForceUpdateCanvases();
                Canvas.ForceUpdateCanvases(); // 双 flush：清理首次 flush 产生的延迟注册

                // 强制修正所有 item 位置（防止 layout 系统篡改）
                for (int i = newFirst; i <= newLast; i++)
                {
                    var go = FindActiveByIndex(i);
                    if (go == null) continue;
                    var rt = go.transform as RectTransform;
                    if (rt == null) continue;
                    float expectedPos = GetCumulativePosition(i);
                    float actualPos = (_direction == Direction.Vertical)
                        ? -rt.anchoredPosition.y
                        : rt.anchoredPosition.x;
                    if (Mathf.Abs(actualPos - expectedPos) > 0.5f)
                    {
                        if (_direction == Direction.Vertical)
                            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -expectedPos);
                        else
                            rt.anchoredPosition = new Vector2(expectedPos, rt.anchoredPosition.y);
                    }
                }

                // 最终 content sizeDelta 校验
                float expectedTotal = CalculateTotalSize();
                if (_direction == Direction.Vertical)
                {
                    if (Mathf.Abs(_contentRect.sizeDelta.y - expectedTotal) > 0.5f)
                        _contentRect.sizeDelta = new Vector2(_contentRect.sizeDelta.x, expectedTotal);
                }
                else
                {
                    if (Mathf.Abs(_contentRect.sizeDelta.x - expectedTotal) > 0.5f)
                        _contentRect.sizeDelta = new Vector2(expectedTotal, _contentRect.sizeDelta.y);
                }
            }
        }

        private bool IsActiveAt(int index)
        {
            return _activeIndexMap.ContainsKey(index);
        }

        private GameObject FindActiveByIndex(int index)
        {
            _activeIndexMap.TryGetValue(index, out var go);
            return go;
        }

        /// <summary>
        /// /// 仅对新创建的 item 填充内容 + 测量布局。不设置位置（由统一的锚点链 pass 处理）。
        ///
        /// </summary>
        private ActiveItem FillAndMeasureNew(ActiveItem ai)
        {
            var go = ai.gameObject;
            int index = ai.index;
            var rt = go.transform as RectTransform;
            if (rt == null)
            {
                ai.justCreated = false;
                return ai;
            }

            // 设置锚点（位置稍后在锚点链 pass 统一设）
            if (_direction == Direction.Vertical)
            {
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
            }
            else
            {
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 0.5f);
            }

            // 把新 item 放在内容底部（远低于视口），锚点链定位前绝对不可见。
            // (0,0) 在顶部时恰好落在视口内 → 用户看到闪烁 → 感觉卡顿。
            float offScreenY = -CalculateTotalSize() - _viewportSize;
            rt.anchoredPosition = (_direction == Direction.Vertical)
                ? new Vector2(0, offScreenY)
                : new Vector2(offScreenY, 0);

            go.name = $"Item[{index}]";

            // ---- 首选 IScrollItem：同步测量 + 锁定 ----
            if (ai.scrollItem != null && _autoRebuildLayout)
            {
                go.SetActive(true);
                float measured = ai.scrollItem.OnShow(index);
                _itemSizes[index] = Mathf.Max(1f, measured);
                ai.size = measured;
            }
            else if (_autoRebuildLayout)
            {
                OnUpdateItem?.Invoke(go, index);
                if (_direction == Direction.Vertical)
                    rt.sizeDelta = new Vector2(rt.sizeDelta.x, Mathf.Max(1f, _itemSizes[index]));
                else
                    rt.sizeDelta = new Vector2(Mathf.Max(1f, _itemSizes[index]), rt.sizeDelta.y);

                var rootCsf = rt.GetComponent<ContentSizeFitter>();
                if (rootCsf != null) rootCsf.enabled = true;
                go.SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

                float actualSize = (_direction == Direction.Vertical) ? rt.rect.height : rt.rect.width;
                _itemSizes[index] = actualSize;
                ai.size = actualSize;

                rt.sizeDelta = (_direction == Direction.Vertical)
                    ? new Vector2(rt.sizeDelta.x, actualSize)
                    : new Vector2(actualSize, rt.sizeDelta.y);
                if (rootCsf != null) rootCsf.enabled = false;
            }
            else
            {
                OnUpdateItem?.Invoke(go, index);
                if (_direction == Direction.Vertical)
                    rt.sizeDelta = new Vector2(rt.sizeDelta.x, ai.size);
                else
                    rt.sizeDelta = new Vector2(ai.size, rt.sizeDelta.y);
                go.SetActive(true);
            }

            ai.justCreated = false;
            return ai;
        }

        // ================================================================
        // 对象池
        // ================================================================

        private void PreAllocPool()
        {
            // 基于视口估算至少需要多少 item（一屏 + 缓冲）
            float avgSize = _prefabDefaultSize > 0 ? _prefabDefaultSize : 100f;
            _estimatedVisibleCount =
                Mathf.CeilToInt((_viewportSize + _viewportSize * 0.5f) / (avgSize + _itemSpacing)) +
                (_bufferCount + _preCreateBuffer) * 2;

            int need = Mathf.Max(_poolPreAlloc, _estimatedVisibleCount);
            int current = _pool.Count + _activeItems.Count;

            for (int i = current; i < need; i++)
            {
                var go = Instantiate(_itemPrefab, _contentRect);
                go.SetActive(false);
                var item = go.GetComponent<IScrollItem>();
                item?.OnCreate(go.transform as RectTransform);
                _pool.Push(go);
            }
        }

        private GameObject Rent()
        {
            if (_pool.Count > 0)
                return _pool.Pop();

            var go = Instantiate(_itemPrefab, _contentRect);
            go.SetActive(false);
            var item = go.GetComponent<IScrollItem>();
            item?.OnCreate(go.transform as RectTransform);
            return go;
        }

        private void Recycle(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            go.name = "[Pooled]";
            _pool.Push(go);
        }

        // ================================================================
        // 公共 API — 数据操作
        // ================================================================

        /// <summary>
        /// 重新计算所有尺寸并刷新
        /// <para>Refresh all sizes and rebuild layout</para>
        /// </summary>
        public void RefreshList()
        {
            if (!IsInitialized) return;

            // 重新采集尺寸
            for (int i = 0; i < _itemSizes.Count; i++)
                _itemSizes[i] = GetItemSize(i);

            ApplyAndRebuildOrMarkDirty();
        }

        /// <summary>
        /// 设置总量（会清空旧数据重新初始化）/ Set total count, clears old data
        /// </summary>
        public void SetTotalCount(int count)
        {
            ClearAll();
            Initialize(count);
        }

        /// <summary>
        /// 追加数据到末尾
        /// <para>Append data at end. 不触发 FullRebuild，只更新累积位置。</para>
        /// </summary>
        public void AppendData(int count)
        {
            for (int i = 0; i < count; i++)
                _itemSizes.Add(GetItemSize(_itemSizes.Count));

            RebuildCumulativePositions();
            RefreshVisibleItems(false);
        }

        /// <summary>
        /// 头部插入数据，自动保持滚动位置
        /// <para>Prepend data at top, maintain scroll position</para>
        /// </summary>
        public void PrependData(int count)
        {
            if (count <= 0) return;

            float originalScroll = GetContentScrollOffset();

            for (int i = 0; i < count; i++)
                _itemSizes.Insert(i, GetItemSize(i));

            if (!IsInitialized)
            {
                IsInitialized = true;
            }

            RebuildCumulativePositions();

            // First FullRebuild: measure new items
            FullRebuild();

            // Compensate scroll with actual sizes
            float actualPrepend = 0f;
            for (int i = 0; i < count; i++)
                actualPrepend += _itemSizes[i] + _itemSpacing;

            float maxOff = GetMaxScrollOffset();
            float newScroll = Mathf.Clamp(originalScroll + actualPrepend, 0, maxOff);
            SetContentScroll(newScroll);

            RebuildCumulativePositions();
            RefreshVisibleItems(true);
        }

        /// <summary>
        /// 移除末尾 item
        /// <para>Remove last item</para>
        /// </summary>
        public void RemoveLast()
        {
            if (_itemSizes.Count == 0) return;
            _itemSizes.RemoveAt(_itemSizes.Count - 1);
            ApplyAndRebuildOrMarkDirty();
        }

        /// <summary>
        /// 按索引移除 item
        /// <para>Remove item at index</para>
        /// </summary>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _itemSizes.Count) return;
            _itemSizes.RemoveAt(index);
            ApplyAndRebuildOrMarkDirty();
        }

        /// <summary>
        /// 更新单个 item 的尺寸
        /// <para>Update item size. 手动覆盖，触发重建</para>
        /// </summary>
        public void UpdateItemSize(int index, float newSize)
        {
            if (index < 0 || index >= _itemSizes.Count) return;
            _itemSizes[index] = Mathf.Max(1f, newSize);
            ApplyAndRebuildOrMarkDirty();
        }

        /// <summary>
        /// OSA-style：更改 item 尺寸
        /// <para>Change item size with optional animation. 动画时长由 _layoutAnimationDuration 控制，0=瞬变。</para>
        /// </summary>
        public void ChangeItemSize(int index, float newSize)
        {
            UpdateItemSize(index, newSize);
        }

        /// <summary>
        /// 更新 item 尺寸并平滑过渡动画
        /// <para>Animate item size change. 仅对当前可视 item 生效。</para>
        /// </summary>
        public void AnimateItemSize(int index, float newSize)
        {
            if (!IsInitialized || index < 0 || index >= _itemSizes.Count) return;
            if (_layoutAnimationDuration <= 0f)
            {
                ChangeItemSize(index, newSize);
                return;
            }

            float oldSize = _itemSizes[index];
            float target = Mathf.Max(1f, newSize);
            StopLayoutAnimation();
            var cts = new CancellationTokenSource();
            _layoutAnimCts = cts;
            LayoutAnimationAsync(index, oldSize, target, _layoutAnimationDuration, cts.Token).Forget();
        }

        private async UniTaskVoid LayoutAnimationAsync(int index, float from, float to, float duration, CancellationToken ct)
        {
            float elapsed = 0f;
            while (elapsed < duration && !ct.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = Mathf.SmoothStep(0f, 1f, t);
                _itemSizes[index] = Mathf.Max(1f, Mathf.Lerp(from, to, t));
                RebuildCumulativePositions();
                RefreshVisibleItems(true);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            if (!ct.IsCancellationRequested)
            {
                _itemSizes[index] = Mathf.Max(1f, to);
                RebuildCumulativePositions();
                RefreshVisibleItems(true);
            }
            _layoutAnimCts?.Dispose();
            _layoutAnimCts = null;
        }

        /// <summary>
        /// 刷新指定索引的内容并重新测量
        /// <para>Refresh single item, re-measure</para>
        /// </summary>
        public void RefreshItem(int index)
        {
            if (!IsInitialized || index < 0 || index >= _itemSizes.Count) return;
            var go = FindActiveByIndex(index);
            if (go == null) return;
            var ai = new ActiveItem
                { gameObject = go, scrollItem = go.GetComponent<IScrollItem>(), index = index, justCreated = true };
            var rt = go.transform as RectTransform;
            if (rt != null)
            {
                float offScreenY = -CalculateTotalSize() - _viewportSize;
                rt.anchoredPosition = (_direction == Direction.Vertical)
                    ? new Vector2(0, offScreenY)
                    : new Vector2(offScreenY, 0);
            }

            _activeItems[_activeItems.FindIndex(a => a.index == index)] = FillAndMeasureNew(ai);
            RebuildCumulativePositions();
            RefreshVisibleItems(true);
        }

        /// <summary>
        /// OSA-style：刷新指定范围
        /// <para>Refresh range and rebuild</para>
        /// </summary>
        public void RefreshRange(int fromIndex, int toIndex)
        {
            if (!IsInitialized) return;
            toIndex = Mathf.Min(toIndex, _itemSizes.Count - 1);
            for (int i = fromIndex; i <= toIndex; i++)
            {
                var go = FindActiveByIndex(i);
                if (go == null) continue;
                Recycle(go);
            }

            RefreshVisibleItems(true);
        }

        /// <summary>
        /// OSA-style：双通 rebuild
        /// <para>Schedule twin-pass visibility rebuild</para>
        /// </summary>
        public void ScheduleComputeVisibilityTwinPass()
        {
            RebuildCumulativePositions();
            RefreshVisibleItems(true);
        }

        /// <summary>
        /// 手动通知累积位置重建
        /// <para>Re-measure item content, animate to new size if _layoutAnimationDuration > 0.</para>
        /// </summary>
        public void RequestChangeItemSizeAndUpdateLayout(int index)
        {
            if (!IsInitialized || index < 0 || index >= _itemSizes.Count) return;
            var go = FindActiveByIndex(index);

            float oldSize = _itemSizes[index];
            if (go != null)
            {
                var si = go.GetComponent<IScrollItem>();
                if (si != null)
                {
                    si.OnShow(index);
                    _itemSizes[index] = si.MeasuredSize;
                }
            }

            float newSize = _itemSizes[index];
            if (_layoutAnimationDuration > 0f && Mathf.Abs(newSize - oldSize) > 1f)
            {
                _itemSizes[index] = oldSize;
                RebuildCumulativePositions();
                RefreshVisibleItems(true);
                StopLayoutAnimation();
                var cts = new CancellationTokenSource();
                _layoutAnimCts = cts;
                LayoutAnimationAsync(index, oldSize, newSize, _layoutAnimationDuration, cts.Token).Forget();
            }
            else
            {
                RebuildCumulativePositions();
                RefreshVisibleItems(true);
            }
        }

        /// <summary>
        /// 在指定位置插入一项
        /// <para>Insert item at index</para>
        /// </summary>
        public void InsertAt(int index)
        {
            if (index < 0 || index > _itemSizes.Count) return;
            _itemSizes.Insert(index, GetItemSize(index));
            ApplyAndRebuildOrMarkDirty();
        }

        /// <summary>
        /// 在指定位置批量插入
        /// <para>Insert range at index</para>
        /// </summary>
        public void InsertRange(int index, int count)
        {
            if (index < 0 || index > _itemSizes.Count || count <= 0) return;
            for (int i = 0; i < count; i++)
                _itemSizes.Insert(index + i, GetItemSize(index + i));
            ApplyAndRebuildOrMarkDirty();
        }

        /// <summary>
        /// 强制刷新所有活跃 item 内容
        /// <para>Refresh all active item content. 尺寸不变时用。</para>
        /// </summary>
        public void RefreshContent()
        {
            if (!IsInitialized) return;
            for (int i = 0; i < _activeItems.Count; i++)
            {
                var item = _activeItems[i];
                if (item.gameObject != null)
                    OnUpdateItem?.Invoke(item.gameObject, item.index);
            }
        }

        /// <summary>
        /// 清空所有数据和对象池
        /// <para>Clear all data and pool</para>
        /// </summary>
        public void Clear()
        {
            ClearAll();
            IsInitialized = false;
        }

        // ================================================================
        // 批处理 — 多次数据操作合并为一次 FullRebuild / Batch ops
        // ================================================================

        /// <summary>
        /// 开始批量更新
        /// <para>Begin batch update. 在 EndUpdate 时一次性重建。</para>
        /// </summary>
        public void BeginUpdate()
        {
            _batchDepth++;
        }

        /// <summary>
        /// 结束批量更新
        /// <para>End batch update. 应用所有变更并重建列表。</para>
        /// </summary>
        public void EndUpdate()
        {
            _batchDepth = Mathf.Max(0, _batchDepth - 1);
            if (_batchDepth == 0 && _batchDirty)
            {
                _batchDirty = false;
                ApplyContentLayout();
                FullRebuild();
            }
        }

        /// <summary>
        /// /// 如果在批处理中则标记脏位，否则立即应用布局+重建
        ///
        /// </summary>
        private void ApplyAndRebuildOrMarkDirty()
        {
            if (_batchDepth > 0)
            {
                _batchDirty = true;
            }
            else
            {
                ApplyContentLayout();
                if (!IsInitialized)
                {
                    IsInitialized = true;
                    _wasAtTop = true;
                    _wasAtBottom = IsAtBottom();
                }

                FullRebuild();
            }
        }

        private void SyncContentSizeDelta()
        {
            float total = CalculateTotalSize();
            if (_direction == Direction.Vertical)
                _contentRect.sizeDelta = new Vector2(_contentRect.sizeDelta.x, total);
            else
                _contentRect.sizeDelta = new Vector2(total, _contentRect.sizeDelta.y);
        }

        // 公共 API — 滚动控制 / Scroll control
        // ================================================================

        /// <summary>
        /// 滚动到指定索引
        /// <para>Scroll to index instantly. 两趟式：估算→实测→精确。</para>
        /// </summary>
        /// <param name="index">目标索引 / Target index</param>
        /// <param name="alignment">0=顶部对齐，0.5=居中，1=底部对齐 / 0=top, 0.5=center, 1=bottom</param>
        public void ScrollToIndex(int index, float alignment = 0f)
        {
            if (!IsInitialized || index < 0 || index >= _itemSizes.Count) return;

            float viewSize = GetViewportSize();

            // Pass 1: scroll with estimated sizes, trigger item measurement
            RebuildCumulativePositions();
            float p1Target = GetCumulativePosition(index);
            float p1Item = _itemSizes[index];
            float p1Offset = Mathf.Max(0, viewSize - p1Item) * alignment;
            float p1Total = CalculateTotalSize();
            float p1Max = Mathf.Max(0, p1Total - viewSize);
            float p1Clamped = Mathf.Clamp(p1Target - p1Offset, 0, p1Max);

            SetContentScroll(p1Clamped);
            _scrollRect.StopMovement();
            RefreshVisibleItems(true);

            // Pass 2: recalculate with actual measured sizes
            RebuildCumulativePositions();
            float p2Target = GetCumulativePosition(index);
            float p2Item = _itemSizes[index];
            float p2Offset = Mathf.Max(0, viewSize - p2Item) * alignment;
            float p2Total = CalculateTotalSize();
            float p2Max = Mathf.Max(0, p2Total - viewSize);
            float p2Clamped = Mathf.Clamp(p2Target - p2Offset, 0, p2Max);

            if (Mathf.Abs(p2Clamped - p1Clamped) > 1f)
            {
                SetContentScroll(p2Clamped);
                Canvas.ForceUpdateCanvases();
                RefreshVisibleItems(true);
            }
        }

        // 直接设 content anchoredPosition / Set content scroll directly
        private void SetContentScroll(float clamped)
        {
            if (_direction == Direction.Vertical)
                _contentRect.anchoredPosition = new Vector2(_contentRect.anchoredPosition.x, clamped);
            else
                _contentRect.anchoredPosition = new Vector2(-clamped, _contentRect.anchoredPosition.y);
        }

        /// <summary>
        /// 平滑滚动到指定索引
        /// <para>Smooth animated scroll to index.</para>
        /// </summary>
        /// <param name="index">目标索引 / Target index</param>
        /// <param name="alignment">0=top, 0.5=center, 1=bottom</param>
        /// <param name="duration">动画时长 / Animation duration (s)</param>
        /// <param name="easeCurve">缓动曲线 / Ease curve, null=linear</param>
        public void ScrollToIndexAnimated(int index, float alignment = 0f, float duration = 0.3f,
            AnimationCurve easeCurve = null)
        {
            if (!IsInitialized || index < 0 || index >= _itemSizes.Count) return;

            // Measure first
            float viewSize = GetViewportSize();
            RebuildCumulativePositions();
            float p1Clamped = Mathf.Clamp(
                GetCumulativePosition(index) - Mathf.Max(0, viewSize - _itemSizes[index]) * alignment,
                0, Mathf.Max(0, CalculateTotalSize() - viewSize));
            SetContentScroll(p1Clamped);
            RefreshVisibleItems(true);

            // Exact position with measured sizes
            RebuildCumulativePositions();
            float itemSize = _itemSizes[index];
            float totalSize = CalculateTotalSize();
            float maxScroll = Mathf.Max(0, totalSize - viewSize);
            float target = Mathf.Clamp(GetCumulativePosition(index) - Mathf.Max(0, viewSize - itemSize) * alignment, 0,
                maxScroll);

            float startPos = GetContentScrollOffset();

            _scrollAnimCts?.Cancel();
            _scrollAnimCts?.Dispose();
            var cts = new CancellationTokenSource();
            _scrollAnimCts = cts;
            AnimateScrollAsync(startPos, target, duration, easeCurve, cts.Token).Forget();
        }

        // 滚动动画 / Scroll animation async
        private async UniTaskVoid AnimateScrollAsync(float from, float to, float duration,
            AnimationCurve curve, CancellationToken ct)
        {
            float elapsed = 0f;
            while (elapsed < duration && !ct.IsCancellationRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (curve != null && curve.length > 0)
                    t = curve.Evaluate(t);

                float currentPos = Mathf.Lerp(from, to, t);
                SetContentScrollOffset(currentPos);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            if (!ct.IsCancellationRequested)
            {
                SetContentScrollOffset(to);
                RefreshVisibleItems(true);
            }
            _scrollAnimCts?.Dispose();
            _scrollAnimCts = null;
        }

        // 设 content 滚动偏移 / Set content scroll offset
        private void SetContentScrollOffset(float offset)
        {
            if (_direction == Direction.Vertical)
                _contentRect.anchoredPosition = new Vector2(_contentRect.anchoredPosition.x, offset);
            else
                _contentRect.anchoredPosition = new Vector2(-offset, _contentRect.anchoredPosition.y);
        }

        /// <summary>
        /// 滚动到底部
        /// <para>Scroll to bottom</para>
        /// </summary>
        public void ScrollToBottom()
        {
            ScrollToIndex(_itemSizes.Count - 1, 1f);
        }

        /// <summary>
        /// 滚动到顶部
        /// <para>Scroll to top</para>
        /// </summary>
        public void ScrollToTop()
        {
            ScrollToIndex(0, 0f);
        }

        /// <summary>
        /// 获取归一化滚动位置 [0,1]
        /// <para>Get normalized scroll position. 0=top, 1=bottom</para>
        /// </summary>
        public float GetNormalizedPosition()
        {
            float viewSize = GetViewportSize();
            float totalSize = CalculateTotalSize();
            if (totalSize <= viewSize) return 0f;
            float offset = GetContentScrollOffset();
            float maxOff = totalSize - viewSize;
            return Mathf.Clamp01(offset / maxOff);
        }

        /// <summary>
        /// 是否已滚动到顶部
        /// <para>Whether at top (0.001 tolerance)</para>
        /// </summary>
        public bool IsAtTop()
        {
            return GetNormalizedPosition() <= 0.001f;
        }

        /// <summary>
        /// 是否已滚动到底部
        /// <para>Whether at bottom (0.001 tolerance)</para>
        /// </summary>
        public bool IsAtBottom()
        {
            return GetNormalizedPosition() >= 0.999f;
        }

        // ================================================================
        // 内部工具 / Internal helpers
        // ================================================================

        // 计算最大滚动偏移 / Calculate max scroll offset
        private float GetMaxScrollOffset()
        {
            float totalSize = CalculateTotalSize();
            return Mathf.Max(0, totalSize - _viewportSize);
        }

        // 全量重建 / Full rebuild: recycle all, recreate
        private void FullRebuild()
        {
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                Recycle(_activeItems[i].gameObject);
            }

            _activeItems.Clear();
            _activeIndexMap.Clear();
            FirstVisibleIndex = -1;
            LastVisibleIndex = -1;

            RebuildCumulativePositions(); // 确保 ScrollToIndex/ScrollToBottom 能读到最新位置
            RefreshVisibleItems(true);
        }

        private void ClearAll()
        {
            // 销毁活跃 item
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                if (_activeItems[i].gameObject != null)
                {
                    _activeItems[i].scrollItem?.OnDestroyed();
                    Destroy(_activeItems[i].gameObject);
                }
            }

            _activeItems.Clear();
            _activeIndexMap.Clear();

            // 销毁池中 item
            while (_pool.Count > 0)
            {
                var go = _pool.Pop();
                if (go != null)
                {
                    go.GetComponent<IScrollItem>()?.OnDestroyed();
                    Destroy(go);
                }
            }

            _itemSizes.Clear();
            _cumulativePositions.Clear();
            FirstVisibleIndex = -1;
            LastVisibleIndex = -1;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_scrollRect == null)
                _scrollRect = GetComponent<ScrollRect>();
        }
#endif

        /// <summary>
        /// /// 诊断方法
        /// <para>Debug method: 输出布局状态到 Console。
        /// 运行时右键 InfiniteIrregularScrollList 组件 → Debug/Log Layout Info。
        ///</para>
        /// </summary>
        [ContextMenu("Debug/Log Layout Info")]
        public void DebugLogLayoutInfo()
        {
            Debug.Log($"[ScrollList Debug] " +
                      $"Total={_itemSizes.Count} " +
                      $"Active={_activeItems.Count} " +
                      $"Pool={_pool.Count} " +
                      $"Viewport={GetViewportSize():F1} " +
                      $"ContentH={_contentRect?.sizeDelta.y:F1} " +
                      $"ContentPos={_contentRect?.anchoredPosition.y:F1} " +
                      $"CalcTotal={CalculateTotalSize():F1} " +
                      $"FirstVis={FirstVisibleIndex} " +
                      $"LastVis={LastVisibleIndex} " +
                      $"Offset={GetContentScrollOffset():F1} " +
                      $"MaxScroll={GetMaxScrollOffset():F1} " +
                      $"NormPos={GetNormalizedPosition():F4}");

            // Content anchors/pivot
            if (_contentRect != null)
            {
                Debug.Log(
                    $"[ScrollList Debug] Content anchor=({_contentRect.anchorMin.x:F2},{_contentRect.anchorMin.y:F2})-({_contentRect.anchorMax.x:F2},{_contentRect.anchorMax.y:F2}) pivot=({_contentRect.pivot.x:F2},{_contentRect.pivot.y:F2}) sizeDelta=({_contentRect.sizeDelta.x:F1},{_contentRect.sizeDelta.y:F1})");
            }

            // 逐 item 诊断
            for (int i = 0; i < _activeItems.Count; i++)
            {
                var item = _activeItems[i];
                var rt = item.gameObject?.transform as RectTransform;
                if (rt == null) continue;
                float expectedPos = GetCumulativePosition(item.index);
                float actualPos = (_direction == Direction.Vertical)
                    ? -rt.anchoredPosition.y
                    : rt.anchoredPosition.x;
                float sizeDelta = (_direction == Direction.Vertical) ? rt.sizeDelta.y : rt.sizeDelta.x;
                float rectSize = (_direction == Direction.Vertical) ? rt.rect.height : rt.rect.width;
                float diff = Mathf.Abs(actualPos - expectedPos);

                // 检查 item 上的 VLGroup padding
                string paddingInfo = "";
                var lg = rt.GetComponent<LayoutGroup>();
                if (lg != null)
                    paddingInfo =
                        $" VLGPad=({lg.padding.left},{lg.padding.right},{lg.padding.top},{lg.padding.bottom})";

                Debug.Log($"[ScrollList Item] idx={item.index} " +
                          $"pos={actualPos:F1}(expect={expectedPos:F1},diff={diff:F1}) " +
                          $"sizeDelta={sizeDelta:F1} rectH={rectSize:F1} _itemSize={_itemSizes[item.index]:F1}" +
                          $"{paddingInfo}");
            }

            // 检查 content 上的冲突组件
            if (_contentRect != null)
            {
                var csf = _contentRect.GetComponent<ContentSizeFitter>();
                var lg = _contentRect.GetComponent<LayoutGroup>();
                if (csf != null || lg != null)
                    Debug.LogError(
                        $"[ScrollList Debug] ⚠️ Content 上检测到冲突组件！CSF={csf != null} LayoutGroup={lg != null} — 这会导致定位异常。");
            }
        }
    }
}
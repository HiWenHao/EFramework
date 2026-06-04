// ================================================================
// InfiniteIrregularScrollList.cs
// 无限不规则滚动列表 — 虚拟滚动 + 对象池，支持不等高/不等宽 item
// ================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.Edit;
using EasyFramework.Managers.Pool;
using UnityEngine;
using UnityEngine.UI;

namespace EFExample
{
    /// <summary>
    /// 无限不规则滚动列表
    /// <br/> 基于 Unity ScrollRect 实现虚拟滚动（只渲染可视区域内的 item）
    /// <br/> 每个 item 可拥有独立的高度/宽度，支持运行时动态增删和双向无限加载。
    /// <para>Infinite irregular scrolling list.
    /// <br/> Based on Unity ScrollRect, it implements virtual scrolling (only rendering items within the visible area)
    /// <br/> Each item can have its own height/width. It supports dynamic addition/deletion at runtime and bidirectional infinite loading.</para>
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    [DisallowMultipleComponent]
    public class InfiniteIrregularScrollList : MonoBehaviour
    {
        #region 内部类

        /// <summary>
        /// 滚动方向
        /// <para>Scroll direction</para>
        /// </summary>
        private enum Direction
        {
            Vertical,
            Horizontal
        }

        /// <summary>
        /// 吸附对齐
        /// <para>Snap alignment</para>
        /// </summary>
        private enum SnapAlignment
        {
            None,
            Start,
            Center,
            End
        }

        /// <summary>
        /// 活跃的元素结构
        /// <para>Active element structure</para>
        /// </summary>
        private struct ActiveItem
        {
            public int Index;               // 数据索引
            public float Size;              // 当前尺寸
            public bool JustCreated;        // 是否本帧新建，需要填充+测量
            public GameObject Target;       // 对应的 GameObject 实例
            public IScrollItem ScrollItem;  // 缓存 IScrollItem 避免重复 GetComponent
        }

        #endregion

        #region 序列化字段

        [HeaderPro("引用", "quote")] [SerializeField]
        private ScrollRect scrollRect;

        [SerializeField] private GameObject itemPrefab;

        [HeaderPro("布局", "Layout")] [SerializeField]
        private Direction direction = Direction.Vertical;

        [SerializeField] [Min(0)] private float itemSpacing = 20f;
        [SerializeField] [Min(1)] private int poolPreAlloc = 3;
        [SerializeField] [Min(0)] private int bufferCount = 2;

        [HeaderPro("预创建", "Pre-creation")]
        [Tooltip("在可视缓冲外提前创建并测量 item 的数量。\nitem 在进入视口之前就已布局锁定，避免滑动时卡顿。")]
        [SerializeField]
        [Min(0)]
        private int preCreateBuffer = 3;

        [Header("自适应测量")]
        [Tooltip(
            "开启后，OnUpdateItem 填充完内容会自动 ForceRebuildLayoutImmediate 并测量真实尺寸，\n无需在 OnGetItemSize 中精确计算。OnGetItemSize 只需返回合理估算值即可。")]
        [SerializeField]
        private bool autoRebuildLayout;

        [HeaderPro("动画", "Animation")] [Tooltip("item 尺寸变更时的过渡动画时长（秒）。0 = 无动画，直接瞬变。")] [SerializeField] [Range(0f, 1f)]
        private float layoutAnimationDuration = 0.15f;

        [HeaderPro("吸附", "Adsorb")] [Tooltip("滚动停止时自动吸附到最近 item 的顶部/中间/底部。None = 不吸附。")] [SerializeField]
        private SnapAlignment snapAlignment = SnapAlignment.None;

        [SerializeField] [Range(0.5f, 50f)] private float snapVelocityThreshold = 5f;

        [SerializeField] [Range(0.05f, 0.5f)] private float snapDuration = 0.15f;

        [Header("对象池")] [Tooltip("最大空闲对象数（超过则直接销毁）。≤0 = 无上限。")] [SerializeField]
        private int poolMaxSize = 20;

        [Tooltip("空闲超时销毁（秒）。≤0 = 永不销毁。PoolManager 每 5 秒自动清理一次。")] [SerializeField]
        private float poolIdleTimeout = 30f;

        #endregion

        #region 公开回调

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

        #endregion

        #region 公开属性

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

        #endregion

        #region 私有字段

        private float _viewportSize;        // 视口高度 or 宽度
        private float _prefabDefaultSize;   // prefab 默认尺寸
        private int _estimatedVisibleCount; // 估算的可视 item 数量
        private int _batchDepth;            // 批处理嵌套深度
        private bool _batchDirty;           // 批处理中是否有脏数据
        private bool _wasAtTop;             // 上一帧是否在顶部
        private bool _wasAtBottom;          // 上一帧是否在底部
        private bool _refreshing;           // 正在刷新中
        private bool _scrollListenerAdded;  // 是否已注册 scroll 监听
        private bool _poolInitialized;      // 是否已向 PoolManager 注册池
        private bool _wasMoving;            // 滚动中

        private RectTransform _contentRect;     // ScrollRect 的 content
        private RectTransform _viewportRect;    // ScrollRect 的 viewport

        private CancellationTokenSource _scrollAnimCts;     // 滚动动画取消令牌
        private CancellationTokenSource _layoutAnimCts;     // 布局动画取消令牌
        private CancellationTokenSource _snapDebounceCts;   // 吸附动画取消令牌

        private readonly List<float> _itemSizes = new List<float>(); // 每个 item 的尺寸缓存
        private readonly List<float> _cumulativePositions = new List<float>(); // 预计算累积位置 [i]=item[i] 顶部偏移
        private readonly List<ActiveItem> _activeItems = new List<ActiveItem>(); // 当前活跃 item 列表
        private readonly Dictionary<int, GameObject> _activeIndexMap = new Dictionary<int, GameObject>(); // 查重
        private readonly HashSet<GameObject> _onCreateCalled = new HashSet<GameObject>(); // 跟踪已调用 OnCreate 的 GO

        #endregion

        #region 生命周期

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            if (scrollRect != null && !_scrollListenerAdded)
            {
                scrollRect.onValueChanged.AddListener(OnScrollChanged);
                _scrollListenerAdded = true;
            }

            if (IsInitialized && _activeItems.Count == 0)
                RefreshVisibleItems(true);
        }

        private void OnDisable()
        {
            if (scrollRect != null && _scrollListenerAdded)
            {
                scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
                _scrollListenerAdded = false;
            }

            CancelAllAnimations();
        }

        private void OnDestroy()
        {
            if (scrollRect != null && _scrollListenerAdded)
                scrollRect.onValueChanged.RemoveListener(OnScrollChanged);

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

        #endregion

        // 确保必要引用全都存在
        private void EnsureReferences()
        {
            if (scrollRect == null)
                scrollRect = GetComponent<ScrollRect>();

            _contentRect = scrollRect.content;
            if (_contentRect == null)
            {
                D.Error("[ UI ] [ InfiniteIrregularScrollList ] ScrollRect.Content is null.");
                return;
            }

            _viewportRect = scrollRect.viewport != null
                ? scrollRect.viewport
                : scrollRect.GetComponent<RectTransform>();

            _viewportSize = GetViewportSize();

            var pfRt = itemPrefab.transform as RectTransform;
            if (itemPrefab != null && pfRt != null)
            {
                _prefabDefaultSize = (direction == Direction.Vertical)
                    ? pfRt.rect.height
                    : pfRt.rect.width;
            }

            StripConflictingLayoutComponents(_contentRect);
        }

        // 清空全部
        private void ClearAll()
        {
            // 销毁活跃 item（Despawn 归还池，池会自动管理生命周期）
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                if (_activeItems[i].Target == null) continue;
                _activeItems[i].ScrollItem?.OnDestroyed();
                PoolManager.Instance.Despawn(_activeItems[i].Target);
            }

            _activeItems.Clear();
            _activeIndexMap.Clear();

            DestroyPool();

            _onCreateCalled.Clear();
            _itemSizes.Clear();
            _cumulativePositions.Clear();
            FirstVisibleIndex = -1;
            LastVisibleIndex = -1;
        }

        // 移除 content 上的 LayoutGroup、ContentSizeFitter 和 AspectRatioFitter。
        // 本组件完全手动管理 content 的布局，任何自动布局组件都会产生冲突。
        private static void StripConflictingLayoutComponents(RectTransform content)
        {
            bool stripped = false;

            var csf = content.GetComponent<ContentSizeFitter>();
            if (csf != null)
            {
                csf.enabled = false;
                Destroy(csf);
                stripped = true;
            }

            var layout = content.GetComponent<LayoutGroup>();
            if (layout != null)
            {
                layout.enabled = false;
                Destroy(layout);
                stripped = true;
            }

            var arf = content.GetComponent<AspectRatioFitter>();
            if (arf != null)
            {
                arf.enabled = false;
                Destroy(arf);
                stripped = true;
            }

            if (stripped)
                D.Warning(
                    "[InfiniteIrregularScrollList] Content 上的 LayoutGroup/ContentSizeFitter/AspectRatioFitter 已自动移除。" +
                    "本组件手动管理 content 布局，这些组件会导致定位异常。");
        }

        // 获取元素尺寸
        private float GetItemSize(int index)
        {
            return OnGetItemSize != null ? Mathf.Max(1f, OnGetItemSize(index)) : _prefabDefaultSize;
        }

        // 计算最大滚动偏移 
        private float GetMaxScrollOffset()
        {
            float totalSize = CalculateTotalSize();
            return Mathf.Max(0, totalSize - _viewportSize);
        }

        // 全量重建 
        private void FullRebuild()
        {
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                Recycle(_activeItems[i].Target);
            }

            _activeItems.Clear();
            _activeIndexMap.Clear();
            FirstVisibleIndex = -1;
            LastVisibleIndex = -1;

            RebuildCumulativePositions(); // 确保 ScrollToIndex/ScrollToBottom 能读到最新位置
            RefreshVisibleItems(true);
        }

        // Content 布局设置 ================================================================

        // 应用content布局
        private void ApplyContentLayout(bool preserveScroll = true)
        {
            float total = CalculateTotalSize();

            // 保存当前滚动偏移，避免 sizeDelta 变更时 ScrollRect 自动钳位丢失位置
            float savedOffset = preserveScroll ? GetContentScrollOffset() : 0f;

            if (direction == Direction.Vertical)
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

        // 计算总尺寸
        private float CalculateTotalSize()
        {
            float total = 0f;
            for (int i = 0; i < _itemSizes.Count; i++)
            {
                float s = _itemSizes[i];
                // 未测量过的 item 用 OnGetItemSize 估算，避免 total = 0 导致滚动范围错误
                if (s <= 0.5f) s = GetItemSize(i);
                total += s;
                if (i < _itemSizes.Count - 1) total += itemSpacing;
            }

            return total;
        }

        // 计算索引从 0 到index的累积偏移（含间距），未初始化时回退到线性计算
        private float GetCumulativePosition(int index)
        {
            if (index < _cumulativePositions.Count)
                return _cumulativePositions[index];
            // 防御：_itemSizes 新增但 _cumulativePositions 尚未重建时，回退到线性计算
            float pos = 0f;
            for (int i = 0; i < index && i < _itemSizes.Count; i++)
                pos += _itemSizes[i] + itemSpacing;
            return pos;
        }

        // 重建累积位置数组 _itemSizes 变更后调用，fromIndex=0 表示全量重建。
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
                _cumulativePositions[i] = _cumulativePositions[i - 1] + prevSize + itemSpacing;
            }
        }

        // 二分查找第一个可见 item 索引。返回第一个结束位置 > scrollOffset 的索引。
        private int BinarySearchVisibleIndex(float scrollOffset)
        {
            int lo = 0, hi = _cumulativePositions.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                float itemEnd = _cumulativePositions[mid] + _itemSizes[mid] + itemSpacing;
                if (itemEnd <= scrollOffset)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }

            return Mathf.Min(lo, _cumulativePositions.Count - 1);
        }

        // 可视区域计算 ================================================================

        // 获取视窗尺寸
        private float GetViewportSize()
        {
            if (_viewportRect == null) return 0f;
            return direction == Direction.Vertical
                ? _viewportRect.rect.height
                : _viewportRect.rect.width;
        }

        // 获取内容滚动偏移
        private float GetContentScrollOffset()
        {
            return direction == Direction.Vertical
                ? _contentRect.anchoredPosition.y
                : -_contentRect.anchoredPosition.x;
        }

        // 计算可见范围
        private (int firstIndex, int lastIndex) CalculateVisibleRange()
        {
            int count = _itemSizes.Count;
            if (count == 0) return (-1, -1);

            _viewportSize = GetViewportSize();

            float rawOffset = GetContentScrollOffset();
            float maxScroll = Mathf.Max(0, CalculateTotalSize() - _viewportSize);
            float scrollOffset = Mathf.Clamp(rawOffset, 0, maxScroll);

            int first = BinarySearchVisibleIndex(scrollOffset);
            int totalBuffer = bufferCount + preCreateBuffer;
            first = Mathf.Max(0, first - totalBuffer);

            float visibleEnd = scrollOffset + _viewportSize;
            int last = first;
            for (int i = first; i < count; i++)
            {
                float itemEnd = _cumulativePositions[i] + _itemSizes[i] + itemSpacing;
                last = i;
                if (itemEnd >= visibleEnd) break;
            }

            last = Mathf.Min(count - 1, last + totalBuffer);
            return (first, last);
        }

        // Item 渲染 ================================================================

        // 当滚动位置改变时
        private void OnScrollChanged(Vector2 _)
        {
            // 用户拖动时立即终止动画，避免视觉冲突
            if (scrollRect != null && scrollRect.velocity.sqrMagnitude > 10f)
                StopLayoutAnimation();
            RefreshVisibleItems(false);
            CheckEdgeReached();
            CheckSnap();
        }

        // 检查吸附状态
        private void CheckSnap()
        {
            if (snapAlignment == SnapAlignment.None || scrollRect == null) return;

            float vel = direction == Direction.Vertical
                ? Mathf.Abs(scrollRect.velocity.y)
                : Mathf.Abs(scrollRect.velocity.x);

            bool moving = vel > snapVelocityThreshold;

            if (_wasMoving && !moving && !_refreshing)
            {
                _snapDebounceCts?.Cancel();
                var cts = new CancellationTokenSource();
                _snapDebounceCts = cts;
                SnapDebounceAsync(cts.Token).Forget();
            }

            _wasMoving = moving;
        }

        // 吸附防抖
        private async UniTaskVoid SnapDebounceAsync(CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.12), cancellationToken: ct);
            if (!ct.IsCancellationRequested)
                PerformSnap();
            _snapDebounceCts?.Dispose();
            _snapDebounceCts = null;
        }

        // 执行吸附
        private void PerformSnap()
        {
            if (!IsInitialized || _itemSizes.Count == 0) return;

            float alignment = snapAlignment switch
            {
                SnapAlignment.Start => 0f,
                SnapAlignment.Center => 0.5f,
                SnapAlignment.End => 1f,
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
                float target = GetCumulativePosition(bestIdx) + _itemSizes[bestIdx] * alignment -
                               GetViewportSize() * alignment;
                float total = CalculateTotalSize();
                float maxOff = Mathf.Max(0, total - GetViewportSize());
                float clamped = Mathf.Clamp(target, 0, maxOff);

                var cts = new CancellationTokenSource();
                _scrollAnimCts?.Cancel();
                _scrollAnimCts = cts;
                AnimateScrollAsync(GetContentScrollOffset(), clamped, snapDuration, null, cts.Token).Forget();
            }
        }

        // 检查是否已到达边缘
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

        // 刷新可见 item
        private void RefreshVisibleItemsInternal(bool force)
        {
            if (!IsInitialized || _itemSizes.Count == 0) return;

            if (autoRebuildLayout) SyncContentSizeDelta();

            var (newFirst, newLast) = CalculateVisibleRange();
            if (newFirst < 0) return;

            // force 或 范围变化 才重建
            if (!force && newFirst == FirstVisibleIndex && newLast == LastVisibleIndex)
                return;

            // 回收不再可见的 item
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                var item = _activeItems[i];
                if (item.Index >= newFirst && item.Index <= newLast) continue;
                _activeIndexMap.Remove(item.Index);
                OnItemVisibilityChanged?.Invoke(item.Index, false);
                item.ScrollItem?.OnHide();
                Recycle(item.Target);
                _activeItems.RemoveAt(i);
            }

            // 为缺失索引补齐 item
            for (int i = newFirst; i <= newLast; i++)
            {
                if (_activeIndexMap.ContainsKey(i)) continue;
                var go = Rent();
                go.SetActive(false);
                var si = go.GetComponent<IScrollItem>();
                _activeItems.Add(
                    new ActiveItem
                        { Target = go, ScrollItem = si, Index = i, Size = _itemSizes[i], JustCreated = true });
                _activeIndexMap[i] = go;
                OnItemVisibilityChanged?.Invoke(i, true);
            }

            // 保存锚点, 在填充前记录，填充后 justCreated 全被清除无法区分
            int anchorIdx = -1;
            foreach (var ai in _activeItems)
            {
                if (ai.Target != null && !ai.JustCreated && ai.Index >= newFirst && ai.Index <= newLast)
                {
                    anchorIdx = ai.Index;
                    break;
                }
            }

            if (anchorIdx < 0) anchorIdx = newFirst;
            float oldAnchorPos = anchorIdx < _itemSizes.Count ? GetCumulativePosition(anchorIdx) : 0f;

            // 在填充之前禁用 content 布局, 防冒泡篡改 item 位置
            if (autoRebuildLayout)
            {
                var contentLayout = _contentRect.GetComponent<LayoutGroup>();
                if (contentLayout != null) contentLayout.enabled = false;
                var contentCsf = _contentRect.GetComponent<ContentSizeFitter>();
                if (contentCsf != null) contentCsf.enabled = false;
                var contentArf = _contentRect.GetComponent<AspectRatioFitter>();
                if (contentArf != null) contentArf.enabled = false;
            }

            // 填充新 item, FillAndMeasureNew 内完成测量、锁定
            bool anyFilled = false;
            for (int i = 0; i < _activeItems.Count; i++)
            {
                var ai = _activeItems[i];
                if (!ai.JustCreated) continue;
                _activeItems[i] = FillAndMeasureNew(ai);
                anyFilled = true;
            }

            // Batch flush + 重建累积位置
            if (anyFilled)
            {
                Canvas.ForceUpdateCanvases();
                RebuildCumulativePositions();
            }

            // 补偿顶部尺寸变化导致的视觉偏移
            if (anyFilled && anchorIdx < _itemSizes.Count)
            {
                float delta = GetCumulativePosition(anchorIdx) - oldAnchorPos;
                if (Mathf.Abs(delta) > 0.5f)
                {
                    if (direction == Direction.Vertical)
                        _contentRect.anchoredPosition = new Vector2(_contentRect.anchoredPosition.x,
                            _contentRect.anchoredPosition.y + delta);
                    else
                        _contentRect.anchoredPosition = new Vector2(_contentRect.anchoredPosition.x - delta,
                            _contentRect.anchoredPosition.y);
                }
            }

            // 验证 content sizeDelta
            if (anyFilled && autoRebuildLayout)
            {
                float expectedTotal = CalculateTotalSize();
                if (direction == Direction.Vertical)
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

            for (int i = newFirst; i <= newLast; i++)
            {
                var go = FindActiveByIndex(i);
                if (go == null) continue;
                var rt = go.transform as RectTransform;
                if (rt == null) continue;
                float pos = _cumulativePositions[i];
                rt.anchoredPosition = (direction == Direction.Vertical)
                    ? new Vector2(0, -pos)
                    : new Vector2(pos, 0);
            }

            FirstVisibleIndex = newFirst;
            LastVisibleIndex = newLast;

            // 定位后再次同步 content 尺寸, 以防 ScrollRect 篡改
            SyncContentSizeDelta();

            // 最终保护：flush + 强制修正漂移
            // 双 flush 确保 ForceRebuildLayoutImmediate 冒泡 dirty 在同一帧全部结算。
            if (!anyFilled) return;
            Canvas.ForceUpdateCanvases();
            Canvas.ForceUpdateCanvases();

            // 强制修正所有 item 位置
            for (int i = newFirst; i <= newLast; i++)
            {
                var go = FindActiveByIndex(i);
                if (go == null) continue;
                var rt = go.transform as RectTransform;
                if (rt == null) continue;
                float expectedPos = GetCumulativePosition(i);
                float actualPos = (direction == Direction.Vertical)
                    ? -rt.anchoredPosition.y
                    : rt.anchoredPosition.x;
                if (Mathf.Abs(actualPos - expectedPos) > 0.5f)
                {
                    rt.anchoredPosition = direction == Direction.Vertical
                        ? new Vector2(rt.anchoredPosition.x, -expectedPos)
                        : new Vector2(expectedPos, rt.anchoredPosition.y);
                }
            }

            // 最终 content sizeDelta 校验
            float expectedTotalValue = CalculateTotalSize();
            if (direction == Direction.Vertical)
            {
                if (Mathf.Abs(_contentRect.sizeDelta.y - expectedTotalValue) > 0.5f)
                    _contentRect.sizeDelta = new Vector2(_contentRect.sizeDelta.x, expectedTotalValue);
            }
            else
            {
                if (Mathf.Abs(_contentRect.sizeDelta.x - expectedTotalValue) > 0.5f)
                    _contentRect.sizeDelta = new Vector2(expectedTotalValue, _contentRect.sizeDelta.y);
            }
        }

        // 查找指定索引的 item
        private GameObject FindActiveByIndex(int index)
        {
            _activeIndexMap.TryGetValue(index, out var go);
            return go;
        }

        // 仅对新创建的 item 填充内容 + 测量布局。不设置位置，由统一的锚点链 pass 处理
        private ActiveItem FillAndMeasureNew(ActiveItem ai)
        {
            var go = ai.Target;
            int index = ai.Index;
            var rt = go.transform as RectTransform;
            if (rt == null)
            {
                ai.JustCreated = false;
                return ai;
            }

            // 设置锚点
            if (direction == Direction.Vertical)
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

            // 把新 item 放在内容底部，锚点链定位前绝对不可见。
            float offScreenY = -CalculateTotalSize() - _viewportSize;
            rt.anchoredPosition = (direction == Direction.Vertical)
                ? new Vector2(0, offScreenY)
                : new Vector2(offScreenY, 0);

            go.name = $"Item[{index}]";

            // 延迟 OnCreate：对象池只做 Instantiate，不调用 IScrollItem.OnCreate
            if (ai.ScrollItem != null && !_onCreateCalled.Contains(go))
            {
                ai.ScrollItem.OnCreate(rt);
                _onCreateCalled.Add(go);
            }

            // 首选 IScrollItem：同步测量 + 锁定
            if (ai.ScrollItem != null && autoRebuildLayout)
            {
                go.SetActive(true);
                float measured = ai.ScrollItem.OnShow(index);
                _itemSizes[index] = Mathf.Max(1f, measured);
                ai.Size = measured;
            }
            else if (autoRebuildLayout)
            {
                OnUpdateItem?.Invoke(go, index);
                rt.sizeDelta = direction == Direction.Vertical
                    ? new Vector2(rt.sizeDelta.x, Mathf.Max(1f, _itemSizes[index]))
                    : new Vector2(Mathf.Max(1f, _itemSizes[index]), rt.sizeDelta.y);

                var rootCsf = rt.GetComponent<ContentSizeFitter>();
                if (rootCsf != null) rootCsf.enabled = true;
                go.SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

                float actualSize = (direction == Direction.Vertical) ? rt.rect.height : rt.rect.width;
                _itemSizes[index] = actualSize;
                ai.Size = actualSize;

                rt.sizeDelta = (direction == Direction.Vertical)
                    ? new Vector2(rt.sizeDelta.x, actualSize)
                    : new Vector2(actualSize, rt.sizeDelta.y);
                if (rootCsf != null) rootCsf.enabled = false;
            }
            else
            {
                OnUpdateItem?.Invoke(go, index);
                rt.sizeDelta = direction == Direction.Vertical
                    ? new Vector2(rt.sizeDelta.x, ai.Size)
                    : new Vector2(ai.Size, rt.sizeDelta.y);
                go.SetActive(true);
            }

            ai.JustCreated = false;
            return ai;
        }

        // 对象池 ================================================================

        // 初始化对象池
        private void InitPool()
        {
            if (_poolInitialized || itemPrefab == null) return;
            _poolInitialized = true;

            float avgSize = _prefabDefaultSize > 0 ? _prefabDefaultSize : 100f;
            _estimatedVisibleCount =
                Mathf.CeilToInt((_viewportSize + _viewportSize * 0.5f) / (avgSize + itemSpacing)) +
                (bufferCount + preCreateBuffer) * 2;

            int initial = Mathf.Max(poolPreAlloc, _estimatedVisibleCount);
            PoolManager.Instance.CreateGameObjectPool(itemPrefab, _contentRect, initial, poolMaxSize, poolIdleTimeout);
        }

        // 获取一个游戏物体
        private GameObject Rent()
        {
            var go = PoolManager.Instance.Spawn(itemPrefab);
            if (go != null)
            {
                go.transform.SetParent(_contentRect, false);
                go.SetActive(false);
            }

            return go;
        }

        // 回收一个游戏物体
        private void Recycle(GameObject go)
        {
            if (go == null) return;
            PoolManager.Instance.Despawn(go);
        }

        // 销毁对象池
        private void DestroyPool()
        {
            if (!_poolInitialized) return;
            PoolManager.Instance.DestroyGameObjectPool(itemPrefab);
            _poolInitialized = false;
        }

        // 布局 ================================================================

        // 异步布局动画
        private async UniTaskVoid LayoutAnimationAsync(int index, float from, float to, float duration,
            CancellationToken ct)
        {
            float elapsed = 0f;
            while (elapsed < duration && !ct.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = Mathf.SmoothStep(0f, 1f, t);
                _itemSizes[index] = Mathf.Max(1f, Mathf.Lerp(from, to, t));
                RebuildCumulativePositions();
                RepositionAllActive(); // 轻量重定位，不重建 item
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            if (!ct.IsCancellationRequested)
            {
                _itemSizes[index] = Mathf.Max(1f, to);
                RebuildCumulativePositions();
                RefreshVisibleItems(true); // 动画结束，全量同步
            }

            _layoutAnimCts?.Dispose();
            _layoutAnimCts = null;
        }

        // 仅重新定位所有活跃 item + 同步 sizeDelta, 用于动画期间轻量更新
        private void RepositionAllActive()
        {
            SyncContentSizeDelta();
            for (int i = 0; i < _activeItems.Count; i++)
            {
                var ai = _activeItems[i];
                if (ai.Target == null || ai.Index < 0 || ai.Index >= _cumulativePositions.Count) continue;
                var rt = ai.Target.transform as RectTransform;
                if (rt == null) continue;

                // 同步 sizeDelta：动画期间 _itemSizes 在 lerp，GO 的高度必须跟上
                float cachedSize = _itemSizes[ai.Index];
                ai.Size = cachedSize;
                _activeItems[i] = ai;
                rt.sizeDelta = (direction == Direction.Vertical)
                    ? new Vector2(rt.sizeDelta.x, cachedSize)
                    : new Vector2(cachedSize, rt.sizeDelta.y);

                float pos = _cumulativePositions[ai.Index];
                rt.anchoredPosition = (direction == Direction.Vertical)
                    ? new Vector2(0, -pos)
                    : new Vector2(pos, 0);
            }
        }

        // 如果在批处理中则标记脏位，否则立即应用布局+重建
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

        // 同步内容尺寸
        private void SyncContentSizeDelta()
        {
            float total = CalculateTotalSize();
            _contentRect.sizeDelta = direction == Direction.Vertical
                ? new Vector2(_contentRect.sizeDelta.x, total)
                : new Vector2(total, _contentRect.sizeDelta.y);
        }

        // 设置 content 的 anchoredPosition
        private void SetContentScroll(float clamped)
        {
            _contentRect.anchoredPosition = direction == Direction.Vertical
                ? new Vector2(_contentRect.anchoredPosition.x, clamped)
                : new Vector2(-clamped, _contentRect.anchoredPosition.y);
        }

        // 同步滚动动画
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

        // 设置 content 滚动偏移
        private void SetContentScrollOffset(float offset)
        {
            if (direction == Direction.Vertical)
                _contentRect.anchoredPosition = new Vector2(_contentRect.anchoredPosition.x, offset);
            else
                _contentRect.anchoredPosition = new Vector2(-offset, _contentRect.anchoredPosition.y);
        }

        #region 全部公开函数

        /// <summary>
        /// 初始化列表，首次设置数据后调用或数量变更后调用 RefreshList 函数
        /// <para>Initialize the list. Call after first setting data, or use RefreshList after count changes.</para>
        /// </summary>
        /// <param name="totalCount">数据总量<para>Total data count</para></param>
        public void Initialize(int totalCount)
        {
            EnsureReferences();
            ClearAll();

            for (int i = 0; i < totalCount; i++)
                _itemSizes.Add(GetItemSize(i));

            RebuildCumulativePositions();
            ApplyContentLayout(false);
            InitPool();
            IsInitialized = true;
            RefreshVisibleItems(true);

            // 初始化边缘状态
            _wasAtTop = IsAtTop();
            _wasAtBottom = IsAtBottom();
        }

        /// <summary>
        /// 刷新可视区 item。
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

        /// <summary>
        /// 重新计算所有尺寸并刷新
        /// <para>Refresh all sizes and rebuild layout</para>
        /// </summary>
        public void RefreshList()
        {
            if (!IsInitialized) return;

            for (int i = 0; i < _itemSizes.Count; i++)
            {
                _itemSizes[i] = GetItemSize(i);
            }

            ApplyAndRebuildOrMarkDirty();
        }

        /// <summary>
        /// 设置总量（会清空旧数据重新初始化）
        /// <para>Set total count, clears old data</para>
        /// </summary>
        public void SetTotalCount(int count)
        {
            ClearAll();
            Initialize(count);
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
            {
                _itemSizes.Insert(i, GetItemSize(i));
            }

            if (!IsInitialized)
                IsInitialized = true;

            RebuildCumulativePositions();
            FullRebuild();

            float actualPrepend = 0f;
            for (int i = 0; i < count; i++)
            {
                actualPrepend += _itemSizes[i] + itemSpacing;
            }

            float newScroll = Mathf.Clamp(originalScroll + actualPrepend, 0, GetMaxScrollOffset());
            SetContentScroll(newScroll);
            RebuildCumulativePositions();
            RefreshVisibleItems(true);
        }

        /// <summary>
        /// 追加数据到末尾. 不触发 FullRebuild，只更新累积位置。
        /// <para>Append data at end. Do not trigger FullRebuild function, only update the cumulative position.</para>
        /// </summary>
        public void AppendData(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _itemSizes.Add(GetItemSize(_itemSizes.Count));
            }

            RebuildCumulativePositions();
            RefreshVisibleItems(false);
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
        /// 更新单个 item 的尺寸. 手动覆盖，触发重构
        /// <para>Update item size, Manual override, triggering reconstruction</para>
        /// </summary>
        public void UpdateItemSize(int index, float newSize)
        {
            if (index < 0 || index >= _itemSizes.Count) return;
            _itemSizes[index] = Mathf.Max(1f, newSize);
            ApplyAndRebuildOrMarkDirty();
        }

        /// <summary>
        /// 更改 item 尺寸, 动画时长由 layoutAnimationDuration 控制，0=瞬变。
        /// <para>Change item size with optional animation, duration controlled by layoutAnimationDuration, 0=instant. </para>
        /// </summary>
        public void ChangeItemSize(int index, float newSize)
        {
            UpdateItemSize(index, newSize);
        }

        /// <summary>
        /// 更新 item 尺寸并平滑过渡动画. 仅对当前可视 item 生效。
        /// <para>Animate item size change, only affects currently visible items</para>
        /// </summary>
        public void AnimateItemSize(int index, float newSize)
        {
            if (!IsInitialized || index < 0 || index >= _itemSizes.Count) return;
            if (layoutAnimationDuration <= 0f)
            {
                ChangeItemSize(index, newSize);
                return;
            }

            scrollRect.StopMovement(); // 立即停止滚动，防止与动画冲突
            float oldSize = _itemSizes[index];
            float target = Mathf.Max(1f, newSize);
            StopLayoutAnimation();
            var cts = new CancellationTokenSource();
            _layoutAnimCts = cts;
            LayoutAnimationAsync(index, oldSize, target, layoutAnimationDuration, cts.Token).Forget();
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
                { Target = go, ScrollItem = go.GetComponent<IScrollItem>(), Index = index, JustCreated = true };
            var rt = go.transform as RectTransform;
            if (rt != null)
            {
                float offScreenY = -CalculateTotalSize() - _viewportSize;
                rt.anchoredPosition = (direction == Direction.Vertical)
                    ? new Vector2(0, offScreenY)
                    : new Vector2(offScreenY, 0);
            }

            _activeItems[_activeItems.FindIndex(a => a.Index == index)] = FillAndMeasureNew(ai);
            RebuildCumulativePositions();
            RefreshVisibleItems(true);
        }

        /// <summary>
        /// 刷新指定范围
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
        /// 全量重建，并刷新可视区 item。
        /// <para>Perform a full reconstruction and refresh the visible area items.</para>
        /// </summary>
        public void ScheduleComputeVisibilityTwinPass()
        {
            RebuildCumulativePositions();
            RefreshVisibleItems(true);
        }

        /// <summary>
        /// 更改指定索引的元素大小并更新布局，如果 layoutAnimationDuration > 0，则会进行动画过渡。
        /// <para>Change the size of the element at the specified index and update the layout, animate to new size if layoutAnimationDuration > 0.</para>
        /// </summary>
        public void RequestChangeItemSizeAndUpdateLayout(int index)
        {
            if (!IsInitialized || index < 0 || index >= _itemSizes.Count) return;
            scrollRect.StopMovement();

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
            if (layoutAnimationDuration > 0f && Mathf.Abs(newSize - oldSize) > 1f)
            {
                _itemSizes[index] = oldSize;
                RebuildCumulativePositions();
                RefreshVisibleItems(true);
                StopLayoutAnimation();
                var cts = new CancellationTokenSource();
                _layoutAnimCts = cts;
                LayoutAnimationAsync(index, oldSize, newSize, layoutAnimationDuration, cts.Token).Forget();
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
            {
                _itemSizes.Insert(index + i, GetItemSize(index + i));
            }

            ApplyAndRebuildOrMarkDirty();
        }

        /// <summary>
        /// 强制刷新所有活跃 item 内容
        /// <para>Refresh all active item content. 尺寸不变时用。</para>
        /// </summary>
        public void RefreshContent()
        {
            if (!IsInitialized) return;
            foreach (var item in _activeItems)
            {
                if (item.Target != null)
                    OnUpdateItem?.Invoke(item.Target, item.Index);
            }
        }

        /// <summary>
        /// 清空所有数据和对象池， 再次使用时需重新初始化
        /// <para>Clear all data and pool, It needs to be reinitialized when used again.</para>
        /// </summary>
        public void Clear()
        {
            ClearAll();
            IsInitialized = false;
        }

        // 批处理 ================================================================

        /// <summary>
        /// 开始批量更新，在 EndUpdate 时一次性重建。
        /// <para>Begin batch update, rebuild once in EndUpdate.</para>
        /// </summary>
        public void BeginUpdate()
        {
            _batchDepth++;
        }

        /// <summary>
        /// 结束批量更新，应用所有变更并重建列表。
        /// <para>End batch update, apply all changes and rebuild list.</para>
        /// </summary>
        public void EndUpdate()
        {
            _batchDepth = Mathf.Max(0, _batchDepth - 1);
            if (_batchDepth != 0 || !_batchDirty) return;
            _batchDirty = false;
            ApplyContentLayout();
            FullRebuild();
        }

        // 滚动控制 ================================================================
        /// <summary>
        /// 滚动到指定索引
        /// <para>Scroll to index instantly. </para>
        /// </summary>
        /// <param name="index">目标索引<para>Target index</para></param>
        /// <param name="alignment">0 = top, 0.5 = center, 1 = bottom</param>
        public void ScrollToIndex(int index, float alignment = 0f)
        {
            if (!IsInitialized || index < 0 || index >= _itemSizes.Count) return;

            float viewSize = GetViewportSize();
            RebuildCumulativePositions();
            float p1Target = GetCumulativePosition(index);
            float p1Item = _itemSizes[index];
            float p1Offset = Mathf.Max(0, viewSize - p1Item) * alignment;
            float p1Total = CalculateTotalSize();
            float p1Max = Mathf.Max(0, p1Total - viewSize);
            float p1Clamped = Mathf.Clamp(p1Target - p1Offset, 0, p1Max);

            SetContentScroll(p1Clamped);
            scrollRect.StopMovement();
            RefreshVisibleItems(true);
            RebuildCumulativePositions();
            float p2Target = GetCumulativePosition(index);
            float p2Item = _itemSizes[index];
            float p2Offset = Mathf.Max(0, viewSize - p2Item) * alignment;
            float p2Total = CalculateTotalSize();
            float p2Max = Mathf.Max(0, p2Total - viewSize);
            float p2Clamped = Mathf.Clamp(p2Target - p2Offset, 0, p2Max);

            if (!(Mathf.Abs(p2Clamped - p1Clamped) > 1f)) return;
            SetContentScroll(p2Clamped);
            Canvas.ForceUpdateCanvases();
            RefreshVisibleItems(true);
        }

        /// <summary>
        /// 平滑滚动到指定索引
        /// <para>Smooth animated scroll to index.</para>
        /// </summary>
        /// <param name="index">目标索引<para>Target index</para></param>
        /// <param name="alignment">0=top, 0.5=center, 1=bottom</param>
        /// <param name="duration">动画时长<para>Animation duration</para></param>
        /// <param name="easeCurve">缓动曲线, null = 线性<para>Ease curve, null = linear</para></param>
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

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (scrollRect == null)
                scrollRect = GetComponent<ScrollRect>();
        }

        /// <summary>
        /// 诊断方法：输出布局状态到 Console
        /// <para>Debug method: dump layout state. Right-click component → Debug/Log Layout Info.</para>
        /// </summary>
        [ContextMenu("Debug/Log Layout Info")]
        public void DebugLogLayoutInfo()
        {
            Debug.Log($"[ScrollList Debug] " +
                      $"Total={_itemSizes.Count} " +
                      $"Active={_activeItems.Count} " +
                      $"PoolInit={_poolInitialized} " +
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
                var rt = item.Target?.transform as RectTransform;
                if (rt == null) continue;
                float expectedPos = GetCumulativePosition(item.Index);
                float actualPos = (direction == Direction.Vertical)
                    ? -rt.anchoredPosition.y
                    : rt.anchoredPosition.x;
                float sizeDelta = (direction == Direction.Vertical) ? rt.sizeDelta.y : rt.sizeDelta.x;
                float rectSize = (direction == Direction.Vertical) ? rt.rect.height : rt.rect.width;
                float diff = Mathf.Abs(actualPos - expectedPos);

                // 检查 item 上的 VLGroup padding
                string paddingInfo = "";
                var lg = rt.GetComponent<LayoutGroup>();
                if (lg != null)
                    paddingInfo =
                        $" VLGPad=({lg.padding.left},{lg.padding.right},{lg.padding.top},{lg.padding.bottom})";

                Debug.Log($"[ScrollList Item] idx={item.Index} " +
                          $"pos={actualPos:F1}(expect={expectedPos:F1},diff={diff:F1}) " +
                          $"sizeDelta={sizeDelta:F1} rectH={rectSize:F1} _itemSize={_itemSizes[item.Index]:F1}" +
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
#endif
    }
}
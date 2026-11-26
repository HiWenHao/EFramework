/*
 * ================================================
 * Describe:        无限不规则滚动列表 — 虚拟滚动 + 对象池，支持不等高/不等宽
 * Author:          Alvin8412
 * CreationTime:    2026-06-04 19:44:18
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-06-04 19:44:18
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasyFramework.Ui
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
    public partial class ScrollListPro : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        #region 常量

        private const float PositionEpsilon = 0.5f;          // 位置/尺寸比较容差（像素）
        private const float UnmeasuredSizeThreshold = 0.5f;  // 小于此值视为未测量，走估算
        private const float FallbackItemSize = 100f;         // prefab 无尺寸时的兜底高度
        private const float ViewportEstimateFactor = 0.5f;   // 可视数量估算的视口余量系数
        private const float SnapDebounceSeconds = 0.12f;     // 吸附触发的防抖时长
        private const float EdgeEpsilon = 0.001f;            // 归一化边缘判定容差
        private const float DefaultScrollDuration = 0.3f;    // 跳转动画默认时长

        #endregion

        #region 内部类

        /// <summary>
        /// 活跃的元素结构
        /// <para>Active element structure</para>
        /// </summary>
        private struct ActiveItem
        {
            public int Index;                   // 数据索引
            public float Size;                  // 当前尺寸
            public bool JustCreated;            // 是否本帧新建，需要填充+测量
            public GameObject Target;           // 对应的 GameObject 实例
            public IScrollProItem ScrollItem;   // 缓存 item 契约，避免重复 GetComponent
        }

        #endregion

        #region 序列化字段

        [HeaderPro("引用", "References", HeaderProType.Title)]
        [HeaderPro("滚动区域", "Scroll Rect", HeaderProType.Note)]
        [SerializeField]
        private ScrollRect scrollRect;

        [HeaderPro("元素预制体", "Item Prefab", HeaderProType.Note)]
        [SerializeField] private GameObject itemPrefab;

        [HeaderPro("布局", "Layout", HeaderProType.Title)]
        [HeaderPro("滚动方向", "Direction", HeaderProType.Note)]
        [SerializeField]
        private Direction direction = Direction.Vertical;

        [HeaderPro("元素间距", "Item Spacing", HeaderProType.Note)]
        [SerializeField] [Min(0)] private float itemSpacing = 20f;
        [HeaderPro("预分配数量", "Pool Pre-Alloc", HeaderProType.Note)]
        [SerializeField] [Min(1)] private int poolPreAlloc = 3;
        [HeaderPro("视口缓冲数量", "Buffer Count", HeaderProType.Note)]
        [SerializeField] [Min(0)] private int bufferCount = 2;

        [HeaderPro("预创建", "Pre-creation", HeaderProType.Title)]
        [HeaderPro("预创建缓冲数量", "Pre-Create Buffer", HeaderProType.Note)]
        [Tooltip("在可视缓冲外提前创建并测量 item 的数量。\nitem 在进入视口之前就已布局锁定，避免滑动时卡顿。")]
        [SerializeField]
        [Min(0)]
        private int preCreateBuffer = 3;

        [HeaderPro("自适应测量", "Auto Measure", HeaderProType.Title)]
        [HeaderPro("自动重建布局", "Auto Rebuild Layout", HeaderProType.Note)]
        [Tooltip("开启后，item 的 OnShowContent 填充完内容会自动 ForceRebuildLayoutImmediate 并测量真实尺寸，" +
                 "\n无需在 OnGetItemSize 中精确计算。OnGetItemSize 只需返回合理估算值即可。")]
        [SerializeField]
        private bool autoRebuildLayout;

        [HeaderPro("动画", "Animation", HeaderProType.Title)]
        [HeaderPro("布局动画时长（秒）", "Layout Animation Duration (s)", HeaderProType.Note)]
        [Tooltip("item 尺寸变更时的过渡动画时长（秒）。0 = 无动画，直接瞬变。")]
        [SerializeField] [Range(0f, 1f)]
        private float layoutAnimationDuration = 0.15f;

        [HeaderPro("吸附", "Snap", HeaderProType.Title)]
        [HeaderPro("吸附模式", "Snap Alignment", HeaderProType.Note)]
        [Tooltip("滚动停止时自动吸附到最近 item 的顶部/中间/底部。None = 不吸附。")]
        [SerializeField]
        private SnapAlignment snapAlignment = SnapAlignment.None;

        [HeaderPro("吸附速度阈值", "Snap Velocity Threshold", HeaderProType.Note)]
        [SerializeField] [Range(0.5f, 50f)] private float snapVelocityThreshold = 5f;

        [HeaderPro("吸附时长（秒）", "Snap Duration (s)", HeaderProType.Note)]
        [SerializeField] [Range(0.05f, 0.5f)] private float snapDuration = 0.15f;

        [HeaderPro("对象池", "Object Pool", HeaderProType.Title)]
        [HeaderPro("最大空闲数量", "Max Pool Size", HeaderProType.Note)]
        [Tooltip("最大空闲对象数（超过则直接销毁）。≤0 = 无上限。")]
        [SerializeField]
        private int poolMaxSize = 20;

        [HeaderPro("空闲超时（秒）", "Idle Timeout (s)", HeaderProType.Note)]
        [Tooltip("空闲超时销毁（秒）。≤0 = 永不销毁。PoolManager 每 5 秒自动清理一次。")]
        [SerializeField]
        private float poolIdleTimeout = 30f;

        #endregion

        #region 公开回调

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

        private float _viewportSize; // 视口高度 or 宽度
        private float _prefabDefaultSize; // prefab 默认尺寸
        private int _estimatedVisibleCount; // 估算的可视 item 数量
        private int _batchDepth; // 批处理嵌套深度
        private bool _batchDirty; // 批处理中是否有脏数据
        private bool _wasAtTop; // 上一帧是否在顶部
        private bool _wasAtBottom; // 上一帧是否在底部
        private bool _notifyingUnderfilled; // 不足一屏补发中，防止回调内 AppendData 同步递归
        private bool _layoutAnimSkipSizeRestore; // 结构变更取消布局动画时，catch 不回退尺寸（索引已失效）
        private bool _refreshing; // 正在刷新中
        private bool _scrollListenerAdded; // 是否已注册 scroll 监听
        private bool _poolInitialized; // 是否已向 PoolManager 注册池
        private bool _wasMoving; // 滚动中
        private bool _animating; // 动画中，抑制 OnScrollChanged 刷新

        private RectTransform _contentRect; // ScrollRect 的 content
        private RectTransform _viewportRect; // ScrollRect 的 viewport

        private CancellationTokenSource _scrollAnimCts; // 滚动动画取消令牌
        private CancellationTokenSource _layoutAnimCts; // 布局动画取消令牌
        private CancellationTokenSource _snapDebounceCts; // 吸附动画取消令牌
        private ISnapStrategy _snapStrategy; // 用户注入的吸附策略，null 时按 snapAlignment 枚举回退默认

        // 用户是否正在真实拖拽（由 IBegin/IEndDragHandler 维护）。
        // 不能用 scrollRect.velocity 判断：动画自身改 anchoredPosition 也会产生速度，
        // 长距离动画（大列表跨屏跳转）的 velocity 会超阈值，导致动画被自己的回调取消。
        private bool _isDragging;

        private readonly List<float> _itemSizes = new List<float>(); // 每个 item 的尺寸缓存
        private readonly List<float> _cumulativePositions = new List<float>(); // 预计算累积位置 [i]=item[i] 顶部偏移
        private readonly List<ActiveItem> _activeItems = new List<ActiveItem>(); // 当前活跃 item 列表
        private readonly Dictionary<int, GameObject> _activeIndexMap = new Dictionary<int, GameObject>(); // 查重
        // 存 InstanceID 而非 GameObject 引用：池销毁 GO 后不保留强引用，避免已销毁对象泄漏
        private readonly HashSet<int> _onCreateCalled = new HashSet<int>();

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

        /// <summary>
        /// 用户开始拖拽：置位真实拖拽标志。
        /// <para>Called when the user starts dragging; sets the real-drag flag.</para>
        /// <br/> 用标志而非 velocity 区分"用户拖动"与"动画自驱动"，避免长距离动画被误判取消。
        /// <para>Uses a flag instead of velocity to tell user dragging from animation-driven movement,
        /// preventing long animations from being falsely cancelled.</para>
        /// </summary>
        /// <param name="eventData">事件数据<para>Event data</para></param>
        public void OnBeginDrag(PointerEventData eventData) => _isDragging = true;

        /// <summary>
        /// 用户结束拖拽：清除真实拖拽标志。
        /// <para>Called when the user ends dragging; clears the real-drag flag.</para>
        /// </summary>
        /// <param name="eventData">事件数据<para>Event data</para></param>
        public void OnEndDrag(PointerEventData eventData) => _isDragging = false;

        private void OnDisable()
        {
            _isDragging = false;
            if (scrollRect != null && _scrollListenerAdded)
            {
                scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
                _scrollListenerAdded = false;
            }

            CancelAllAnimations();
            // 复位瞬时状态，避免禁用/启用窗口内动画标志残留吞掉首个滚动刷新
            _animating = false;
            _refreshing = false;
        }

        private void OnDestroy()
        {
            _isDragging = false;
            if (scrollRect != null && _scrollListenerAdded)
            {
                scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
                _scrollListenerAdded = false;
            }

            CancelAllAnimations();
            Clear();

            // HybridCLR 域重载安全：切断公开委托/事件链，避免持有旧域对象形成泄漏与悬空引用
            OnGetItemSize = null;
            OnItemVisibilityChanged = null;
            OnReachTop = null;
            OnReachBottom = null;
            _snapStrategy = null;
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
            _layoutAnimSkipSizeRestore = false;
        }

        private void StopLayoutAnimation()
        {
            _layoutAnimCts?.Cancel();
            _layoutAnimCts?.Dispose();
            _layoutAnimCts = null;
        }

        // 结构变更（插入/删除）前取消在飞的布局动画：动画只捕获 index，
        // 数据变更后索引语义已变，若让 catch 回退尺寸会串写到别的 item。
        // 只 Cancel 不 Dispose，令牌清理交给动画自身的 finally（A6 归属判断）。
        private void CancelLayoutAnimationForStructuralChange()
        {
            if (_layoutAnimCts == null) return;
            _layoutAnimSkipSizeRestore = true;
            _layoutAnimCts.Cancel();
        }

        #endregion

        #region 全部公开函数

        /// <summary>
        /// 设置自定义吸附策略。注入后优先于 Inspector 的 Snap Alignment 枚举；
        /// 传 null 恢复为按枚举使用内置策略（Top/Center/Bottom）。
        /// <para>Sets a custom snap strategy. When injected it takes precedence over the Snap Alignment
        /// enum; pass null to fall back to the built-in strategy selected by the enum.</para>
        /// </summary>
        /// <param name="strategy">自定义策略或 null<para>Custom strategy or null</para></param>
        public void SetSnapStrategy(ISnapStrategy strategy)
        {
            _snapStrategy = strategy;
        }

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

            // 初始化边缘状态：不足一屏时先置底部基线为 false，让补发逻辑能触发首次 OnReachBottom；
            // NotifyUnderfilledBottom 内部 finally 会按补发后的真实数据重算基线。
            _wasAtTop = IsAtTop();
            _wasAtBottom = false;
            NotifyUnderfilledBottom();
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

        /// <summary>
        /// 刷新可视区 item。
        /// <para>Refreshes the visible items.</para>
        /// </summary>
        /// <param name="force">true 时跳过范围比较，强制全部重建<para>True skips range comparison and forces a full rebuild</para></param>
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

            float actualPrepend = 0f;
            for (int i = 0; i < count; i++)
            {
                actualPrepend += _itemSizes[i] + itemSpacing;
            }

            // 先按头部新增的高度把滚动位置补偿到正确处，再做唯一一次全量重建。
            // 原实现先 FullRebuild（在旧偏移下按错位数据渲染）再改偏移又重建，
            // 造成两轮全回收/新建、闪烁与位置跳变。
            float newScroll = Mathf.Clamp(originalScroll + actualPrepend, 0, GetMaxScrollOffset());
            SetContentScroll(newScroll);
            FullRebuild();
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
            // 追加后仍不足一屏时，确保 OnReachBottom 有机会触发（业务可继续加载）
            NotifyUnderfilledBottom();
        }

        /// <summary>
        /// 按索引移除 item
        /// <para>Remove item at index</para>
        /// </summary>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _itemSizes.Count) return;
            CancelLayoutAnimationForStructuralChange();
            float removedSize = _itemSizes[index];
            _itemSizes.RemoveAt(index);
            RebuildCumulativePositions();
            CompensateOffsetForStructuralChange(index, -(removedSize + itemSpacing));
            ApplyAndRebuildOrMarkDirty();
        }

        /// <summary>
        /// 移除末尾 item
        /// <para>Remove last item</para>
        /// </summary>
        public void RemoveLast()
        {
            if (_itemSizes.Count == 0) return;
            CancelLayoutAnimationForStructuralChange();
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
        /// 更新 item 尺寸并平滑过渡动画. 仅对当前可视 item 生效。
        /// <para>Animate item size change, only affects currently visible items</para>
        /// </summary>
        public void AnimateItemSize(int index, float newSize)
        {
            if (!IsInitialized || index < 0 || index >= _itemSizes.Count) return;
            if (layoutAnimationDuration <= 0f)
            {
                UpdateItemSize(index, newSize);
                return;
            }

            scrollRect.StopMovement(); // 立即停止滚动，防止与动画冲突
            StopLayoutAnimation();
            _layoutAnimSkipSizeRestore = false; // 新动画启动，清除可能残留的结构变更标志
            var cts = new CancellationTokenSource();
            _layoutAnimCts = cts;
            LayoutAnimationAsync(index, _itemSizes[index], Mathf.Max(1f, newSize), layoutAnimationDuration, cts.Token).Forget();
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
                { Target = go, ScrollItem = go.GetComponent<IScrollProItem>(), Index = index, JustCreated = true };
            var rt = go.transform as RectTransform;
            if (rt != null)
            {
                float offScreenY = -CalculateTotalSize() - _viewportSize;
                rt.anchoredPosition = (direction == Direction.Vertical)
                    ? new Vector2(0, offScreenY)
                    : new Vector2(offScreenY, 0);
            }

            // FindIndex 找不到时返回 -1，直接索引会抛异常：已存在则原地替换，不存在则追加（保证项被追踪）
            int slot = _activeItems.FindIndex(a => a.Index == index);
            var measured = FillAndMeasureNew(ai);
            if (slot >= 0)
                _activeItems[slot] = measured;
            else
                _activeItems.Add(measured);
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
            // 倒序遍历，避免移除 _activeItems 元素时索引错位
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                var item = _activeItems[i];
                if (item.Index < fromIndex || item.Index > toIndex) continue;
                _activeIndexMap.Remove(item.Index);
                OnItemVisibilityChanged?.Invoke(item.Index, false);
                item.ScrollItem?.OnHide();
                Recycle(item.Target);
                _activeItems.RemoveAt(i);
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
                var si = go.GetComponent<IScrollProItem>();
                if (si != null)
                {
                    si.OnShow(index, true);
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
            CancelLayoutAnimationForStructuralChange();
            float insertedSize = GetItemSize(index);
            _itemSizes.Insert(index, insertedSize);
            RebuildCumulativePositions();
            CompensateOffsetForStructuralChange(index, insertedSize + itemSpacing);
            ApplyAndRebuildOrMarkDirty();
        }

        /// <summary>
        /// 在指定位置批量插入
        /// <para>Insert range at index</para>
        /// </summary>
        public void InsertRange(int index, int count)
        {
            if (index < 0 || index > _itemSizes.Count || count <= 0) return;
            CancelLayoutAnimationForStructuralChange();
            float insertedTotal = 0f;
            for (int i = 0; i < count; i++)
            {
                float size = GetItemSize(index + i);
                insertedTotal += size;
                _itemSizes.Insert(index + i, size);
            }

            RebuildCumulativePositions();
            CompensateOffsetForStructuralChange(index, insertedTotal + count * itemSpacing);
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
                    item.ScrollItem?.OnShow(item.Index, false);
            }
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

            if (!(Mathf.Abs(p2Clamped - p1Clamped) > 1f))
            {
                SyncEdgeBaseline();
                return;
            }

            SetContentScroll(p2Clamped);
            Canvas.ForceUpdateCanvases();
            RefreshVisibleItems(true);
            SyncEdgeBaseline();
        }

        // 跳跃定位后同步边缘基线，避免 OnReachTop/OnReachBottom 因状态滞后而重复或漏发
        private void SyncEdgeBaseline()
        {
            _wasAtTop = IsAtTop();
            _wasAtBottom = IsAtBottom();
        }

        /// <summary>
        /// 平滑滚动到指定索引
        /// <para>Smooth animated scroll to index.</para>
        /// </summary>
        /// <param name="index">目标索引<para>Target index</para></param>
        /// <param name="alignment">0=top, 0.5=center, 1=bottom</param>
        /// <param name="duration">动画时长<para>Animation duration</para></param>
        /// <param name="easeCurve">缓动曲线, null = 线性<para>Ease curve, null = linear</para></param>
        public void ScrollToIndexAnimated(int index, float alignment = 0f, float duration = DefaultScrollDuration,
            AnimationCurve easeCurve = null)
        {
            if (!IsInitialized || index < 0 || index >= _itemSizes.Count) return;

            // 保存用户当前真实位置：动画必须从"用户看到的位置"起步，不能从测量瞬移后的位置起步，
            // 否则测量瞬移会被渲染出来（弹飞），动画第一帧又把它拉回起点（先弹飞后铆钉）。
            float startPos = GetContentScrollOffset();
            float viewSize = GetViewportSize();

            // 测量阶段抑制 onValueChanged 回调：SetContentScroll 会让 ScrollRect 派发事件，
            // 若此时 CheckSnap 判定未吸附，会再启动一个 snap 动画与本动画打架。
            // 用 _animating 复用同一抑制位，异常时复位，正常时交由动画 finally 复位。
            _animating = true;
            float target;
            try
            {
                // 瞬移到目标附近，强制目标 item 进入视口完成实测（_itemSizes 从估算变实测）
                RebuildCumulativePositions();
                float p1Clamped = Mathf.Clamp(
                    GetCumulativePosition(index) - Mathf.Max(0, viewSize - _itemSizes[index]) * alignment,
                    0, Mathf.Max(0, CalculateTotalSize() - viewSize));
                SetContentScroll(p1Clamped);
                scrollRect.StopMovement();
                RefreshVisibleItems(true);

                // 用实测尺寸计算精确目标
                RebuildCumulativePositions();
                float itemSize = _itemSizes[index];
                float totalSize = CalculateTotalSize();
                float maxScroll = Mathf.Max(0, totalSize - viewSize);
                target = Mathf.Clamp(
                    GetCumulativePosition(index) - Mathf.Max(0, viewSize - itemSize) * alignment, 0, maxScroll);

                // 测量完成后把可见状态还原到起点：content 位置 + item 集合都回到 startPos。
                // 尺寸已实测，此次非强制刷新不会再次大幅改变尺寸，锚点补偿 ≈ 0，画面稳定。
                SetContentScroll(startPos);
                RefreshVisibleItems(false);
                Canvas.ForceUpdateCanvases();
            }
            catch
            {
                // 测量异常且未启动动画时，必须清除抑制位，避免 _animating 永久为 true。
                _animating = false;
                throw;
            }

            // 正常路径：抑制位保持 true，交由下面启动的动画在 finally 中复位
            try
            {
                _scrollAnimCts?.Cancel();
                _scrollAnimCts?.Dispose();
                var cts = new CancellationTokenSource();
                _scrollAnimCts = cts;
                AnimateScrollAsync(startPos, target, duration, easeCurve, cts.Token).Forget();
            }
            catch
            {
                // 动画未真正启动时必须交还抑制位，避免 _animating 永久为 true
                _animating = false;
                throw;
            }
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
            return GetNormalizedPosition() <= EdgeEpsilon;
        }

        /// <summary>
        /// 是否已滚动到底部
        /// <para>Whether at bottom (0.001 tolerance)</para>
        /// </summary>
        public bool IsAtBottom()
        {
            if (CalculateTotalSize() <= GetViewportSize()) return true;
            return GetNormalizedPosition() >= 1f - EdgeEpsilon;
        }

        // 不足一屏时补发 OnReachBottom：这种状态滚不动，OnScrollChanged 永远不会触发，
        // 需在初始化/追加后主动通知业务加载更多。先提交基线再回调，避免回调内 AppendData
        // 更新基线后被旧快照覆盖；_notifyingUnderfilled 防止同步递归。
        private void NotifyUnderfilledBottom()
        {
            if (!IsInitialized || _notifyingUnderfilled) return;
            float viewSize = GetViewportSize();
            if (viewSize <= 0f || CalculateTotalSize() > viewSize) return;
            if (_wasAtBottom) return; // 已通知过，避免重复

            _notifyingUnderfilled = true;
            try
            {
                // 回调前提交基线：回调内可能 AppendData → SyncEdgeBaseline 更新基线
                _wasAtBottom = true;
                _wasAtTop = IsAtTop();
                OnReachBottom?.Invoke();
            }
            finally
            {
                _notifyingUnderfilled = false;
                // 按当前真实状态重算：既保留回调内的数据变更结果，又能检测后续离开边缘
                _wasAtTop = IsAtTop();
                _wasAtBottom = IsAtBottom();
            }
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
            D.Log("[ UiManager ] [ ScrollListPro ] " +
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
                D.Log(
                    $"[ UiManager ] [ ScrollListPro ] Content anchor=({_contentRect.anchorMin.x:F2},{_contentRect.anchorMin.y:F2})-({_contentRect.anchorMax.x:F2},{_contentRect.anchorMax.y:F2}) pivot=({_contentRect.pivot.x:F2},{_contentRect.pivot.y:F2}) sizeDelta=({_contentRect.sizeDelta.x:F1},{_contentRect.sizeDelta.y:F1})");
            }

            // 逐 item 诊断
            foreach (var item in _activeItems)
            {
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

                D.Log($"[ UiManager ] [ ScrollListPro ] idx={item.Index} " +
                      $"pos={actualPos:F1}(expect={expectedPos:F1},diff={diff:F1}) " +
                      $"sizeDelta={sizeDelta:F1} rectH={rectSize:F1} " +
                      $"_itemSize={(item.Index >= 0 && item.Index < _itemSizes.Count ? _itemSizes[item.Index] : -1f):F1}" +
                      $"{paddingInfo}");
            }

            // 检查 content 上的冲突组件
            if (_contentRect == null) return;
            var csf = _contentRect.GetComponent<ContentSizeFitter>();
            var lg2 = _contentRect.GetComponent<LayoutGroup>();
            if (csf != null || lg2 != null)
                D.Error(
                    $"[ UiManager ] [ ScrollListPro ] ⚠️ Content 上检测到冲突组件！CSF={csf != null} LayoutGroup={lg2 != null} — 这会导致定位异常。");
        }
#endif
    }
}
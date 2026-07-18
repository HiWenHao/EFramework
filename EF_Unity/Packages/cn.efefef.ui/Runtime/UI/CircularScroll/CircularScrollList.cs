/*
 * ================================================
 * Describe:        循环滚动列表 - 核心控制器
 * Author:          Alvin8412
 * CreationTime:    2026-06-18 16:45:51
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-06-18 16:45:51
 * ScriptVersion:   0.1
 * ================================================
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyFramework.Edit;
using EasyFramework.Managers.Pool;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// 循环滚动列表 — 核心控制器
    /// <para>Circular scrolling list - core controller</para>
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class CircularScrollList : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        #region Inspector - 序列化字段

        [HeaderPro("======基础配置======", "======Base Config======")]
        [SerializeField, Tooltip("ScrollRect 组件（自动获取）")]
        private ScrollRect _scrollRect;

        [SerializeField, Tooltip("Content 节点（自动获取）")]
        private RectTransform _content;

        [SerializeField, Tooltip("列表项 Prefab，需挂载 CircularScrollItem 或其子类")]
        private GameObject _itemPrefab;

        [SerializeField, Tooltip("单个 Item 的高度（垂直模式）或宽度（水平模式）")]
        private float _itemSize = 100f;

        [SerializeField, Tooltip("数据总条数")] private int _totalCount;

        [SerializeField, Tooltip("额外缓冲的 Item 数量（视口外预创建，减少闪烁）")]
        private int _bufferCount = 2;

        [HeaderPro("======方向======", "======Direction======")]
        [SerializeField, Tooltip("滚动方向，选择后自动配置 ScrollRect")]
        private Direction _direction = Direction.Vertical;

        [HeaderPro("======循环模式======", "======Loop Mode======")]
        [SerializeField, Tooltip("是否开启循环滚动")]
        private bool _enableLoop = true;

        [HeaderPro("======吸附配置======", "======Snap Config======")]
        [SerializeField, Tooltip("吸附模式")]
        private SnapAlignment _snapMode = SnapAlignment.None;

        [SerializeField, Tooltip("吸附动画时长（秒）")] private float _snapDuration = 0.25f;

        [SerializeField, Tooltip("吸附缓动曲线")]
        private AnimationCurve _snapCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField, Tooltip("ScrollRect 惯性速度低于此值（像素/秒）时触发自动吸附，让滑动先自然减速再贴靠")]
        private float _snapVelocityThreshold = 50f;

        [HeaderPro("======滚动动画======", "======Scroll Animation======")]
        [SerializeField, Tooltip("ScrollToIndex 默认动画时长（秒）")]
        private float _defaultScrollDuration = 0.4f;

        [SerializeField, Tooltip("ScrollToIndex 默认缓动曲线")]
        private AnimationCurve _defaultScrollCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [HeaderPro("======事件======", "======Events======")]
        [SerializeField, Tooltip("开始拖拽/滚动时触发")]
        private UnityEvent _onScrollStarted = new UnityEvent();

        [SerializeField, Tooltip("滚动停止（含吸附完成）时触发，参数为当前选中索引")]
        private UnityEvent<int> _onScrollEnded = new UnityEvent<int>();

        [SerializeField, Tooltip("Item 被创建/复用时触发，参数：Item GameObject、数据索引")]
        private UnityEvent<GameObject, int> _onItemCreated = new UnityEvent<GameObject, int>();

        [SerializeField, Tooltip("Item 被回收时触发，参数：Item GameObject、数据索引")]
        private UnityEvent<GameObject, int> _onItemRecycled = new UnityEvent<GameObject, int>();

        #endregion

        #region Event - 订阅事件

        /// <summary>开始滚动</summary>
        public UnityEvent OnScrollStarted => _onScrollStarted;

        /// <summary>滚动结束 [int selectedIndex]</summary>
        public UnityEvent<int> OnScrollEnded => _onScrollEnded;

        /// <summary>Item 创建/复用</summary>
        public UnityEvent<GameObject, int> OnItemCreated => _onItemCreated;

        /// <summary>Item 回收</summary>
        public UnityEvent<GameObject, int> OnItemRecycled => _onItemRecycled;

        #endregion

        #region 公开属性

        /// <summary>当前选中的数据索引</summary>
        public int SelectedIndex { get; private set; }

        /// <summary>当前数据总量</summary>
        public int TotalCount => _totalCount;

        /// <summary>开启循环</summary>
        public bool EnableLoop
        {
            get => _enableLoop;
            set
            {
                if (_enableLoop == value) return;
                _enableLoop = value;
                ApplyLoopMode();
                Reinitialize(_totalCount);
            }
        }

        /// <summary>滚动方向</summary>
        public Direction ScrollDirection
        {
            get => _direction;
            set
            {
                if (_direction == value) return;
                _direction = value;
                ApplyDirection();
                Reinitialize(_totalCount);
            }
        }

        /// <summary>当前吸附模式</summary>
        public SnapAlignment CurrentSnapMode
        {
            get => _snapMode;
            set => _snapMode = value;
        }

        /// <summary>是否正在吸附动画中</summary>
        public bool IsSnapping { get; private set; }

        /// <summary>是否正在滚动动画中</summary>
        public bool IsScrollingToTarget { get; private set; }

        /// <summary>当前活跃的 Item 数量</summary>
        public int ActiveItemCount => _activeSlots.Count;

        #endregion

        #region 内部数据

        // 列表项槽位：记录虚拟索引与 Item 实例的映射
        private struct ItemSlot
        {
            public int VirtualIndex;
            public CircularScrollItem Item;
        }

        // PoolManager 对象池是否已初始化
        private bool _poolInitialized;
        private int _lastPoolPrefabId;

        // 当前滚动动画协程
        private Coroutine _snapCoroutine;
        private Coroutine _scrollToCoroutine;

        // 拖拽松手后等待 ScrollRect 惯性减速再触发吸附
        private bool _pendingSnap;

        // ScrollRect 的值变化监听
        private bool _isScrollListenerRegistered;

        // 标记：是否需要在下一帧刷新可见项（用于跳转后延迟刷新）
        private bool _pendingRefresh;

        // 当前活跃的 Item 槽位列表
        private readonly List<ItemSlot> _activeSlots = new List<ItemSlot>();

        // 缓存HashSet，避免每帧GC分配（替代 UpdateVisibleItems 中的 new HashSet<int>()）
        private readonly HashSet<int> _neededIndices = new HashSet<int>();

        #endregion

        #region Life Cycle - 生命周期

        private void Awake() => ApplyDirection();

        private void OnEnable()
        {
            if (_scrollRect == null) return;
            _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            _isScrollListenerRegistered = true;
        }

        private void OnDisable()
        {
            if (_scrollRect != null && _isScrollListenerRegistered)
            {
                _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
                _isScrollListenerRegistered = false;
            }

            StopAllSnapAnimations();
        }

        private void OnDestroy()
        {
            ClearAllItems();

            if (_poolInitialized && _itemPrefab != null)
            {
                PoolManager.Instance.DestroyGameObjectPool(_itemPrefab);
                _poolInitialized = false;
            }

            if (_scrollRect == null || !_isScrollListenerRegistered) return;
            _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            _isScrollListenerRegistered = false;
        }

        private void Update()
        {
            if (_pendingSnap && !IsSnapping && !IsScrollingToTarget)
            {
                bool atLimit = IsAtScrollBoundary();
                if (_scrollRect != null && (_scrollRect.velocity.magnitude <= _snapVelocityThreshold || atLimit))
                {
                    _pendingSnap = false;
                    _scrollRect.velocity = Vector2.zero;
                    TrySnap();
                }
            }

            if (!_pendingRefresh) return;
            _pendingRefresh = false;
            Canvas.ForceUpdateCanvases();
            UpdateVisibleItems();
        }

        #endregion

        #region Public Function - 公开函数

        /// <summary>
        /// 初始化列表
        /// </summary>
        /// <param name="totalCount">数据总条数</param>
        public void Initialize(int totalCount)
        {
            _totalCount = Mathf.Max(0, totalCount);
            ClearAllItems();
            ApplyContentSize();
            ApplyLoopMode();
            ResetScrollPosition();
            SelectedIndex = 0;
            UpdateVisibleItems();
        }

        /// <summary>
        /// 重新初始化列表（数据条目数变更时调用）
        /// </summary>
        /// <param name="totalCount">新的数据总条数</param>
        public void Reinitialize(int totalCount)
        {
            int oldSelected = SelectedIndex;
            Initialize(totalCount);

            // 尝试恢复选中状态
            if (_totalCount > 0)
            {
                int targetIndex = Mathf.Clamp(oldSelected, 0, _totalCount - 1);
                JumpToIndex(targetIndex);
            }
        }

        /// <summary>
        /// 瞬间跳转到指定索引（无动画）
        /// </summary>
        /// <param name="index">目标数据索引</param>
        public void JumpToIndex(int index)
        {
            if (_totalCount <= 0) return;

            StopAllSnapAnimations();

            index = ClampOrWrapIndex(index);
            SelectedIndex = index;

            float targetPos = CalculateSnapPosition(index);
            SetScrollPosition(targetPos);

            _pendingRefresh = true;
            _scrollRect.velocity = Vector2.zero;
        }

        /// <summary>
        /// 动画滚动到指定索引
        /// </summary>
        /// <param name="index">目标数据索引</param>
        /// <param name="duration">动画时长（秒），-1 使用默认值</param>
        /// <param name="curve">缓动曲线，null 使用默认曲线</param>
        public void ScrollToIndex(int index, float duration = -1f, AnimationCurve curve = null)
        {
            if (_totalCount <= 0) return;

            StopAllSnapAnimations();

            index = ClampOrWrapIndex(index);
            float targetPos = CalculateSnapPosition(index);
            float startPos = GetScrollPosition();

            // 循环模式下选择最近的路径
            if (_enableLoop)
            {
                targetPos = GetNearestLoopPosition(startPos, targetPos);
            }

            float animDuration = duration > 0f ? duration : _defaultScrollDuration;
            AnimationCurve animCurve = curve ?? _defaultScrollCurve;

            IsScrollingToTarget = true;
            _onScrollStarted?.Invoke();

            _scrollToCoroutine = StartCoroutine(AnimateScrollTo(startPos, targetPos, index, animDuration, animCurve));
        }

        /// <summary>
        /// 获取指定数据索引的 Item（可能为 null，如果不在当前可见范围内）
        /// </summary>
        public CircularScrollItem GetItemAtDataIndex(int dataIndex)
        {
            foreach (var slot in _activeSlots)
            {
                if (slot.Item.DataIndex == dataIndex)
                    return slot.Item;
            }

            return null;
        }

        /// <summary>
        /// 刷新全部可见 Item（重新走 OnSetup）
        /// </summary>
        public void RefreshAllItems()
        {
            foreach (var slot in _activeSlots)
            {
                slot.Item.OnSetup(slot.Item.DataIndex);
            }
        }

        /// <summary>
        /// 刷新指定索引的 Item
        /// </summary>
        public void RefreshItem(int dataIndex)
        {
            var item = GetItemAtDataIndex(dataIndex);
            if (item != null)
            {
                item.OnSetup(dataIndex);
            }
        }

        /// <summary>
        /// 滚动到目标位置（像素），可用于微调
        /// </summary>
        public void ScrollToPosition(float position)
        {
            StopAllSnapAnimations();
            SetScrollPosition(position);
            UpdateVisibleItems();
            UpdateSelectedFromPosition();
        }

        #endregion

        // ==================== IBeginDragHandler / IEndDragHandler ====================

        public void OnBeginDrag(PointerEventData eventData)
        {
            _pendingSnap = false;

            StopAllSnapAnimations();

            _onScrollStarted?.Invoke();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // 不立即吸附 — 等 Update 中 velocity 自然衰减到阈值以下
            if (_snapMode != SnapAlignment.None)
            {
                _pendingSnap = true;
            }
        }

        // ==================== ScrollRect 事件 ====================

        private void OnScrollValueChanged(Vector2 normalizedPosition)
        {
            if (!IsSnapping && !IsScrollingToTarget && !_pendingRefresh)
            {
                UpdateVisibleItems();
            }
        }

        // ==================== 核心逻辑：可见项更新 ====================

        /// <summary>
        /// 计算当前应显示哪些虚拟索引的 Item，回收不可见的，创建/定位可见的
        /// </summary>
        private void UpdateVisibleItems()
        {
            if (_content == null || _itemPrefab == null || _totalCount <= 0 || _itemSize <= 0f) return;

            float viewportSize = GetViewportSize();

            // Canvas 布局未完成时视口尺寸为 0，延迟到下一帧重试
            if (viewportSize <= 0f)
            {
                _pendingRefresh = true;
                return;
            }

            float scrollPos = GetScrollPosition();

            // 计算可见虚拟索引范围
            int firstVirtualIndex = Mathf.FloorToInt(scrollPos / _itemSize) - _bufferCount;
            int lastVirtualIndex = Mathf.CeilToInt((scrollPos + viewportSize) / _itemSize) + _bufferCount;

            // 非循环模式下钳制范围
            if (!_enableLoop)
            {
                firstVirtualIndex = Mathf.Max(0, firstVirtualIndex);
                lastVirtualIndex = Mathf.Min(_totalCount - 1, lastVirtualIndex);
            }

            // 构建当前需要的虚拟索引集合（使用缓存 HashSet 避免每帧 GC 分配）
            _neededIndices.Clear();
            for (int vi = firstVirtualIndex; vi <= lastVirtualIndex; vi++)
            {
                if (!_enableLoop && (vi < 0 || vi >= _totalCount)) continue;

                int dataIdx = _enableLoop ? ModPositive(vi, _totalCount) : vi;
                if (dataIdx >= 0 && dataIdx < _totalCount || _enableLoop)
                    _neededIndices.Add(vi);
            }

            // 回收不再需要的 Item
            for (int i = _activeSlots.Count - 1; i >= 0; i--)
            {
                if (!_neededIndices.Contains(_activeSlots[i].VirtualIndex))
                {
                    RecycleSlot(i);
                }
            }

            // 为新需要的虚拟索引分配 Item
            foreach (int vi in _neededIndices)
            {
                if (!HasSlotForVirtualIndex(vi))
                {
                    CreateSlot(vi);
                }
            }
        }

        // ==================== Item 槽位管理 ====================

        private bool HasSlotForVirtualIndex(int virtualIndex)
        {
            foreach (var slot in _activeSlots)
            {
                if (slot.VirtualIndex == virtualIndex)
                    return true;
            }

            return false;
        }

        private void CreateSlot(int virtualIndex)
        {
            int dataIndex = _enableLoop ? ModPositive(virtualIndex, _totalCount) : virtualIndex;
            if (dataIndex < 0 || dataIndex >= _totalCount) return;

            CircularScrollItem item = GetFromPool();
            if (item == null) return;

            RectTransform rt = item.RectTransform;
            rt.SetParent(_content, false);

            if (_direction == Direction.Vertical)
            {
                // 垂直模式：顶部拉伸锚点，pivot 顶部对齐
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(0f, _itemSize);
                rt.anchoredPosition = new Vector2(0f, -virtualIndex * _itemSize);
            }
            else
            {
                // 水平模式：左侧拉伸锚点，pivot 左侧对齐
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(_itemSize, 0f);
                rt.anchoredPosition = new Vector2(virtualIndex * _itemSize, 0f);
            }

            rt.localScale = Vector3.one;
            item.gameObject.SetActive(true);

            item.OnSetup(dataIndex);

            _activeSlots.Add(new ItemSlot { VirtualIndex = virtualIndex, Item = item });
            _onItemCreated?.Invoke(item.gameObject, dataIndex);
        }

        private void RecycleSlot(int slotIndex)
        {
            var slot = _activeSlots[slotIndex];
            int dataIndex = slot.Item.DataIndex;
            var itemGo = slot.Item.gameObject;

            _activeSlots.RemoveAt(slotIndex);

            // 先通知外部（在 Item 状态被重置前，确保 DataIndex 等数据仍可读）
            _onItemRecycled?.Invoke(itemGo, dataIndex);

            // 通过 PoolManager 回收（PooledObject 自动处理 SetActive+SetParent）
            // IPoolable.OnDespawn → CircularScrollItem.OnRecycle 自动清理业务状态
            var pooledObj = slot.Item.GetComponent<PooledObject>();
            if (pooledObj != null)
            {
                PoolManager.Instance.Despawn(pooledObj);
            }
            else
            {
                // 安全回退：非池对象直接销毁
                Destroy(itemGo);
            }
        }

        private void ClearAllItems()
        {
            StopAllSnapAnimations();

            // 回收所有活跃 Item 回到对象池（RecycleSlot 内部 RemoveAt，从尾部逐个回收）
            while (_activeSlots.Count > 0)
            {
                RecycleSlot(_activeSlots.Count - 1);
            }
        }

        // ==================== 对象池 ====================

        private CircularScrollItem GetFromPool()
        {
            if (_itemPrefab == null) return null;

            EnsurePoolCreated();

            var go = PoolManager.Instance.Spawn(_itemPrefab);
            if (go == null) return null;

            return go.GetComponent<CircularScrollItem>();
        }

        /// <summary>
        /// 确保 PoolManager 中已注册当前 prefab 的对象池（首次或 prefab 变更时重建）
        /// </summary>
        private void EnsurePoolCreated()
        {
            int prefabId = _itemPrefab.GetInstanceID();

            // prefab 引用变更 → 销毁旧池
            if (_poolInitialized && _lastPoolPrefabId != prefabId)
            {
                // 先回收所有活跃 Item（RecycleSlot 内部 RemoveAt，从尾部逐个回收）
                while (_activeSlots.Count > 0)
                {
                    RecycleSlot(_activeSlots.Count - 1);
                }

                PoolManager.Instance.DestroyGameObjectPool(_itemPrefab);
                _poolInitialized = false;
            }

            if (!_poolInitialized)
            {
                int visibleCount = Mathf.CeilToInt(GetViewportSize() / Mathf.Max(1f, _itemSize));
                int initialCount = Mathf.Max(visibleCount + _bufferCount * 2 + 2, 4);

                PoolManager.Instance.CreateGameObjectPool(
                    _itemPrefab,
                    _content,
                    initialCount,
                    -1,
                    -1f
                );
                _poolInitialized = true;
                _lastPoolPrefabId = prefabId;
            }
        }

        // ==================== 吸附逻辑 ====================

        /// <summary>
        /// 吸附到最近的项（仅在速度已降至阈值以下时调用）
        /// </summary>
        private void TrySnap()
        {
            if (_snapMode == SnapAlignment.None) return;
            if (_totalCount <= 0) return;

            int nearestIndex = FindNearestSnapIndex();
            float targetPos = CalculateSnapPosition(nearestIndex);

            if (_enableLoop)
                targetPos = GetNearestLoopPosition(GetScrollPosition(), targetPos);

            StartSnapAnimation(targetPos, nearestIndex);
        }

        /// <summary>
        /// 找到最近的吸附目标索引
        /// </summary>
        private int FindNearestSnapIndex()
        {
            float scrollPos = GetScrollPosition();
            float viewportSize = GetViewportSize();

            // 计算视口中吸附点的内容坐标
            float snapPointInContent;
            switch (_snapMode)
            {
                case SnapAlignment.Top:
                    snapPointInContent = scrollPos;
                    break;
                case SnapAlignment.Center:
                    snapPointInContent = scrollPos + (viewportSize - _itemSize) * 0.5f;
                    break;
                case SnapAlignment.Bottom:
                    snapPointInContent = scrollPos + viewportSize - _itemSize;
                    break;
                default:
                    snapPointInContent = scrollPos;
                    break;
            }

            int nearestIndex = Mathf.RoundToInt(snapPointInContent / _itemSize);
            return _enableLoop ? ModPositive(nearestIndex, _totalCount) : Mathf.Clamp(nearestIndex, 0, _totalCount - 1);
        }

        /// <summary>
        /// 计算指定数据索引的吸附位置
        /// </summary>
        private float CalculateSnapPosition(int dataIndex)
        {
            float viewportSize = GetViewportSize();

            return _snapMode switch
            {
                SnapAlignment.Center =>
                    dataIndex * _itemSize - (viewportSize - _itemSize) * 0.5f,
                SnapAlignment.Bottom =>
                    dataIndex * _itemSize - (viewportSize - _itemSize),
                _ => dataIndex * _itemSize
            };
        }

        /// <summary>
        /// 循环模式下找到距离当前位置最近的目标位置
        /// </summary>
        private float GetNearestLoopPosition(float currentPos, float targetPos)
        {
            float contentSize = _totalCount * _itemSize;
            float halfContent = contentSize * 0.5f;

            // 将 targetPos 调整到 currentPos 附近（在 ± halfContent 范围内）
            float diff = targetPos - currentPos;
            // 将 diff 包装到 [-halfContent, halfContent]
            diff = ((diff + halfContent) % contentSize + contentSize) % contentSize - halfContent;
            return currentPos + diff;
        }

        // ==================== 吸附/滚动动画 ====================

        private void StartSnapAnimation(float targetPos, int targetIndex)
        {
            if (_snapCoroutine != null)
                StopCoroutine(_snapCoroutine);

            IsSnapping = true;
            _snapCoroutine = StartCoroutine(AnimateSnapCoroutine(targetPos, targetIndex));
        }

        private IEnumerator AnimateSnapCoroutine(float targetPos, int targetIndex)
        {
            float startPos = GetScrollPosition();
            float smoothFps = Time.smoothDeltaTime > 0f ? 1f / Time.smoothDeltaTime : 60f;
            int totalFrames = Mathf.Max(1, Mathf.RoundToInt(_snapDuration * smoothFps));
            int frame = 0;

            for (frame = 0; frame < totalFrames; frame++)
            {
                float t = _snapCurve.Evaluate((float)(frame + 1) / totalFrames);
                SetScrollPositionRaw(Mathf.LerpUnclamped(startPos, targetPos, t));
                UpdateVisibleItems();
                yield return null;
            }

            // 动画结束后规范化位置（循环模式取模），同时刷新可见项
            SetScrollPosition(targetPos);
            UpdateVisibleItems();

            SelectedIndex = targetIndex;
            IsSnapping = false;
            _snapCoroutine = null;

            _onScrollEnded?.Invoke(SelectedIndex);
        }

        private IEnumerator AnimateScrollTo(float startPos, float targetPos, int targetIndex, float duration,
            AnimationCurve curve)
        {
            float smoothFps = Time.smoothDeltaTime > 0f ? 1f / Time.smoothDeltaTime : 60f;
            int totalFrames = Mathf.Max(1, Mathf.RoundToInt(duration * smoothFps));

            for (int frame = 0; frame < totalFrames; frame++)
            {
                float t = curve.Evaluate((float)(frame + 1) / totalFrames);
                SetScrollPositionRaw(Mathf.LerpUnclamped(startPos, targetPos, t));
                UpdateVisibleItems();
                yield return null;
            }

            // 动画结束后规范化位置
            SetScrollPosition(targetPos);
            UpdateVisibleItems();

            SelectedIndex = targetIndex;
            IsScrollingToTarget = false;
            _scrollToCoroutine = null;

            _onScrollEnded?.Invoke(SelectedIndex);
        }

        private void StopAllSnapAnimations()
        {
            _pendingSnap = false;

            if (_snapCoroutine != null)
            {
                StopCoroutine(_snapCoroutine);
                _snapCoroutine = null;
                IsSnapping = false;
            }

            if (_scrollToCoroutine != null)
            {
                StopCoroutine(_scrollToCoroutine);
                _scrollToCoroutine = null;
                IsScrollingToTarget = false;
            }
        }

        // ==================== 滚动位置辅助方法 ====================

        /// <summary>
        /// 获取标准化滚动位置（永远 >= 0，越大越靠近列表末尾）
        /// 水平模式做符号归一化：scrollPos = -content.anchoredPosition.x
        /// </summary>
        private float GetScrollPosition()
        {
            if (_content == null) return 0f;
            return _direction == Direction.Vertical
                ? _content.anchoredPosition.y
                : -_content.anchoredPosition.x;
        }

        private void SetScrollPosition(float position)
        {
            if (_content == null) return;

            float contentSize = _totalCount * _itemSize;
            float viewportSize = GetViewportSize();

            if (!_enableLoop)
            {
                float maxPos = Mathf.Max(0f, contentSize - viewportSize);
                position = Mathf.Clamp(position, 0f, maxPos);
            }
            else if (contentSize > 0)
            {
                position = ModPositiveFloat(position, contentSize);
            }

            if (_direction == Direction.Vertical)
                _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, position);
            else
                _content.anchoredPosition = new Vector2(-position, _content.anchoredPosition.y);
        }

        /// <summary>
        /// 直接设置滚动位置，无取模无钳制（用于动画中间帧）
        /// </summary>
        private void SetScrollPositionRaw(float position)
        {
            if (_content == null) return;
            _content.anchoredPosition = _direction == Direction.Vertical
                ? new Vector2(_content.anchoredPosition.x, position)
                : new Vector2(-position, _content.anchoredPosition.y);
        }

        private void ResetScrollPosition()
        {
            SetScrollPosition(0f);
        }

        private float GetViewportSize()
        {
            if (_scrollRect != null && _scrollRect.viewport != null)
                return _direction == Direction.Vertical
                    ? _scrollRect.viewport.rect.height
                    : _scrollRect.viewport.rect.width;
            return 0f;
        }

        // ==================== 配置应用 ====================

        private void ApplyLoopMode()
        {
            if (_scrollRect == null) return;

            _scrollRect.movementType =
                _enableLoop ? ScrollRect.MovementType.Unrestricted : ScrollRect.MovementType.Clamped;
        }

        private void ApplyContentSize()
        {
            if (_content == null) return;

            float totalSize = _totalCount * _itemSize;
            // 非滚动轴设 0 配合 stretch anchor 自动适配视口尺寸
            if (_direction == Direction.Vertical)
                _content.sizeDelta = new Vector2(0f, totalSize);
            else
                _content.sizeDelta = new Vector2(totalSize, 0f);
        }

        /// <summary>
        /// 根据当前滚动方向配置 ScrollRect 和 Content 锚点
        /// </summary>
        private void ApplyDirection()
        {
            if (_scrollRect == null)
                _scrollRect = GetComponent<ScrollRect>();

            if (_scrollRect != null)
            {
                _scrollRect.horizontal = _direction == Direction.Horizontal;
                _scrollRect.vertical = _direction == Direction.Vertical;
            }

            if (_content == null) return;
            if (_direction == Direction.Vertical)
            {
                _content.anchorMin = new Vector2(0f, 1f);
                _content.anchorMax = new Vector2(1f, 1f);
                _content.pivot = new Vector2(0.5f, 1f);
            }
            else
            {
                _content.anchorMin = new Vector2(0f, 0f);
                _content.anchorMax = new Vector2(0f, 1f);
                _content.pivot = new Vector2(0f, 0.5f);
            }
        }

        // ==================== 工具方法 ====================

        private int ClampOrWrapIndex(int index)
        {
            return _enableLoop ? ModPositive(index, _totalCount) : Mathf.Clamp(index, 0, _totalCount - 1);
        }

        /// <summary>
        /// 非循环模式：内容是否已到边界（忽略速度直接吸附）
        /// </summary>
        private bool IsAtScrollBoundary()
        {
            if (_enableLoop || _totalCount <= 0) return false;
            float pos = GetScrollPosition();
            float maxPos = _totalCount * _itemSize - GetViewportSize();
            return pos <= 0f || pos >= maxPos - 0.01f;
        }

        private void UpdateSelectedFromPosition()
        {
            if (_totalCount <= 0) return;

            int index = FindNearestSnapIndex();
            if (index != SelectedIndex)
            {
                SelectedIndex = index;
            }
        }

        /// <summary>
        /// 正数取模（C# 的 % 对负数不友好）—— 整数版本
        /// </summary>
        private static int ModPositive(int value, int mod)
        {
            if (mod <= 0) return 0;
            int r = value % mod;
            return r < 0 ? r + mod : r;
        }

        /// <summary>
        /// 正数取模 —— 浮点版本
        /// </summary>
        private static float ModPositiveFloat(float value, float mod)
        {
            if (mod <= 0f) return 0f;
            float r = value % mod;
            return r < 0f ? r + mod : r;
        }

        // ==================== Editor 校验 ====================

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 自动获取组件引用
            if (_scrollRect == null)
                _scrollRect = GetComponent<ScrollRect>();
            if (_scrollRect != null && _content == null)
                _content = _scrollRect.content;

            // 确保参数合法
            _itemSize = Mathf.Max(1f, _itemSize);
            _snapDuration = Mathf.Max(0f, _snapDuration);
            _bufferCount = Mathf.Max(0, _bufferCount);

            // 滚动方向变更时自动配置 ScrollRect + Content 锚点
            if (_scrollRect != null)
            {
                ApplyDirection();
            }
        }

        private void Reset()
        {
            _scrollRect = GetComponent<ScrollRect>();
            if (_scrollRect != null)
            {
                _content = _scrollRect.content;
            }

            _direction = Direction.Vertical;
            ApplyDirection();
        }
#endif
    }
}
/*
 * ================================================
 * Describe:        固定尺寸循环滚动列表 — 首尾相连，无限循环
 *                  Circular scroll list with fixed item sizes.
 * Author:          Alvin8412
 * CreationTime:    2026-06-05 15:30:00
 * ScriptVersion:   0.6
 * ================================================
 */

using System;
using System.Collections.Generic;
using EasyFramework.Edit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// 固定尺寸循环滚动列表。首尾相连，无限循环。
    /// <para>Circular (infinite-loop) scroll list with fixed item dimensions.</para>
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    [DisallowMultipleComponent]
    public class CircularScrollListPro : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        /// <summary>
        /// 滚动方向
        /// <para>Scroll direction.</para>
        /// </summary>
        private enum Direction { Vertical, Horizontal }

        /// <summary>
        /// 吸附对齐方式
        /// <para>Snap alignment mode.</para>
        /// </summary>
        public enum SnapAlign
        {
            /// <summary>吸附到视口顶部 / Snap to top of viewport</summary>
            Top,
            /// <summary>吸附到视口中央 / Snap to center of viewport</summary>
            Center,
            /// <summary>吸附到视口底部 / Snap to bottom of viewport</summary>
            Bottom
        }

        #region 序列化字段 / Serialized Fields

        [Header("引用 / References")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform itemPrefab;

        [Header("布局 / Layout")]
        [SerializeField] private Direction direction = Direction.Vertical;
        [Tooltip("项间距 / Item spacing")]
        [SerializeField] private float itemSpacing;

        [Header("缓冲 / Buffer")]
        [Tooltip("视口外每侧额外预建数量 / Extra items per side outside viewport")]
        [SerializeField] private int bufferCount = 2;

        [Header("吸附 / Snap")]
        [Tooltip("停止滚动后吸附到最近整项 / Snap to nearest item on stop")]
        [SerializeField] private bool snapToItem = true;
        [Tooltip("吸附对齐位置 / Snap alignment")]
        [SerializeField] private SnapAlign snapAlignment = SnapAlign.Center;
        [Tooltip("判定停止的速度阈值 / Velocity threshold to trigger snap")]
        [SerializeField] private float snapVelocityThreshold = 50f;
        [Tooltip("吸附动画时长(秒) / Snap animation duration in seconds")]
        [SerializeField] private float snapDuration = 0.15f;

        #endregion

        #region 公开回调 / Public Callbacks

        /// <summary>
        /// 填充 item。参数: dataIndex, GameObject实例。
        /// <para>Fills item content. Parameters: dataIndex, GameObject instance.</para>
        /// </summary>
        public Action<int, GameObject> OnFillItem;

        /// <summary>
        /// 选中项变化回调。参数: 新的 dataIndex。
        /// <para>Fires when the snapped/selected dataIndex changes.</para>
        /// </summary>
        public Action<int> OnSelectedIndexChanged;

        #endregion

        #region 公开属性 / Public Properties

        /// <summary>
        /// 数据总量。设置后自动重建。
        /// <para>Total item count. Setting triggers a full rebuild.</para>
        /// </summary>
        public int ItemCount
        {
            get => _itemCount;
            set { _itemCount = value; Rebuild(); }
        }

        /// <summary>
        /// 当前最近吸附项的 dataIndex；无数据返回 -1。
        /// <para>Nearest snapped dataIndex; -1 if empty.</para>
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                if (_itemCount == 0) return -1;
                float snapRef = _displayedOffset - SnapReferenceOffset();
                return ModPositive(Mathf.RoundToInt(snapRef / _stepSize), _itemCount);
            }
        }

        /// <summary>
        /// 是否已完成初始化
        /// <para>Whether the list has been initialized.</para>
        /// </summary>
        public bool IsInitialized { get; private set; }

        #endregion

        #region 私有字段 / Private Fields

        private RectTransform _contentRect;
        private RectTransform _viewportRect;
        private int _itemCount;
        private float _stepSize;
        private float _totalSize;

        /// <summary>
        /// 核心偏移 — Content 顶边相对 Viewport 顶边的偏移（向下为正）。
        /// <para>Core offset: Content top-edge Y relative to Viewport top-edge (positive = downward).</para>
        /// </summary>
        private float _displayedOffset;

        private float _cachedItemWidth = 100f;
        private float _cachedItemHeight = 100f;

        // 对象池 / Object pool
        private readonly Queue<GameObject> _pool = new Queue<GameObject>();
        private readonly Dictionary<int, GameObject> _activeItems = new Dictionary<int, GameObject>();

        // 范围去重 / Range dedup
        private int _lastFirst = int.MinValue, _lastLast = int.MinValue;
        private readonly List<int> _toRemoveList = new List<int>(16);

        private bool _dragging, _snapping;
        private float _snapStartOffset, _snapTargetOffset, _snapElapsed;
        private int _lastSelectedIndex = -1;

        #endregion

        #region 生命周期 / Lifecycle

        private void Awake()
        {
            if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
            _contentRect = scrollRect.content;
            _viewportRect = scrollRect.viewport;

            if (_contentRect == null)
            {
                D.Error("[ UI ] [ CircularScrollListPro ] ScrollRect.Content 为空。");
                enabled = false;
                return;
            }

            scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            scrollRect.inertia = true;
            scrollRect.onValueChanged.AddListener(OnScrollChanged);
        }

        private void OnEnable()
        {
            if (scrollRect != null)
            {
                scrollRect.horizontal = direction == Direction.Horizontal;
                scrollRect.vertical = direction == Direction.Vertical;
            }
        }

        private void Update()
        {
            if (_itemCount == 0) return;

            if (_snapping)
            {
                UpdateSnapAnimation();
            }
            else if (!_dragging)
            {
                float speed = ScrollSpeed();
                if (speed < snapVelocityThreshold)
                {
                    if (snapToItem) BeginSnap();
                    else SilentNormalize();
                }
            }

            CheckSelectionChange();
        }

        private void OnDestroy()
        {
            if (scrollRect != null)
                scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
            foreach (var go in _pool) { if (go != null) Destroy(go); }
            _pool.Clear();
        }

        #endregion

        #region 拖拽事件 / Drag Events

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            _dragging = true;
            _snapping = false;
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
        }

        #endregion

        #region 公开方法 / Public Methods

        /// <summary>
        /// 初始化列表 / Initialize with item count.
        /// </summary>
        public void Initialize(int itemCount)
        {
            _itemCount = itemCount;
            Rebuild();
        }

        /// <summary>
        /// 刷新可见项 / Refresh visible items.
        /// </summary>
        public void Refresh()
        {
            if (_itemCount == 0) return;
            RefreshCachedSizes();
            _stepSize = GetItemStepSize();
            _totalSize = _itemCount * _stepSize;
            NormalizeOffset();
            ApplyContentLayout();
            ApplyContentPosition();
            ResetRangeTracking();
            RefreshVisibleItems();
            IsInitialized = true;
        }

        /// <summary>
        /// 跳转到指定 dataIndex / Scroll to dataIndex.
        /// </summary>
        public void ScrollTo(int dataIndex)
        {
            if (_itemCount == 0) return;
            _snapping = false;
            dataIndex = ModPositive(dataIndex, _itemCount);
            _displayedOffset = dataIndex * _stepSize + SnapReferenceOffset();
            NormalizeOffset();
            ApplyContentPosition();
            ResetRangeTracking();
            RefreshVisibleItems();
            NotifySelection(dataIndex);
        }

        #endregion

        #region 内部 — 尺寸 / Size Helpers

        private void RefreshCachedSizes()
        {
            if (itemPrefab != null)
            {
                _cachedItemWidth = itemPrefab.sizeDelta.x > 0 ? itemPrefab.sizeDelta.x : 100f;
                _cachedItemHeight = itemPrefab.sizeDelta.y > 0 ? itemPrefab.sizeDelta.y : 100f;
            }
        }

        private float GetItemStepSize()
        {
            float dim = direction == Direction.Vertical ? _cachedItemHeight : _cachedItemWidth;
            return dim + itemSpacing;
        }

        private float ReadViewportSize()
        {
            if (_viewportRect == null) return 400f;
            float size = direction == Direction.Vertical ? _viewportRect.rect.height : _viewportRect.rect.width;
            return size > 1f ? size : 400f;
        }

        private float ItemDim() => direction == Direction.Vertical ? _cachedItemHeight : _cachedItemWidth;

        #endregion

        #region 内部 — 吸附偏移 / Snap Reference

        /// <summary>
        /// SnapAlign 对应的参考偏移。吸附目标 = si * step + snapRef。
        /// <para>Snap-alignment reference offset. Snap target = si * step + snapRef.</para>
        /// </summary>
        private float SnapReferenceOffset()
        {
            float itemDim = ItemDim();
            float vpSize = ReadViewportSize();
            return snapAlignment switch
            {
                SnapAlign.Center => (itemDim - vpSize) * 0.5f,
                SnapAlign.Bottom => itemDim - vpSize,
                _ => 0f // Top
            };
        }

        #endregion

        #region 内部 — 坐标 / Coordinates

        private float GetScrollOffset()
            => direction == Direction.Vertical
                ? _contentRect.anchoredPosition.y
                : -_contentRect.anchoredPosition.x;

        private void ApplyContentPosition()
        {
            if (direction == Direction.Vertical)
                _contentRect.anchoredPosition = new Vector2(_contentRect.anchoredPosition.x, _displayedOffset);
            else
                _contentRect.anchoredPosition = new Vector2(-_displayedOffset, _contentRect.anchoredPosition.y);
        }

        private static int ModPositive(int x, int m) => ((x % m) + m) % m;

        private void ResetRangeTracking()
        {
            _lastFirst = int.MinValue;
            _lastLast = int.MinValue;
        }

        private float ScrollSpeed()
        {
            var v = scrollRect.velocity;
            return direction == Direction.Vertical ? Mathf.Abs(v.y) : Mathf.Abs(v.x);
        }

        #endregion

        #region 内部 — 对象池 / Object Pool

        private GameObject RentNew()
        {
            var go = Instantiate(itemPrefab.gameObject, _contentRect);
            go.SetActive(false);
            _pool.Enqueue(go);
            return go;
        }

        private GameObject Rent() => _pool.Count > 0 ? _pool.Dequeue() : RentNew();

        private void Recycle(GameObject go)
        {
            go.SetActive(false);
            _pool.Enqueue(go);
        }

        private void ClearActive()
        {
            foreach (var kv in _activeItems) Recycle(kv.Value);
            _activeItems.Clear();
        }

        private void ClearPool()
        {
            foreach (var go in _pool) { if (go != null) Destroy(go); }
            _pool.Clear();
        }

        #endregion

        #region 内部 — 归一化 / Normalization

        private void NormalizeOffset()
        {
            if (_totalSize <= 0) return;
            while (_displayedOffset < _totalSize) _displayedOffset += _totalSize;
            while (_displayedOffset >= _totalSize * 2f) _displayedOffset -= _totalSize;
        }

        /// <summary>
        /// 静止时无感归一化（同时移 Content，视觉无跳变）。
        /// <para>Silent normalization: adjusts offset + content position together, visually seamless.</para>
        /// </summary>
        private void SilentNormalize()
        {
            if (_totalSize <= 0) return;
            float old = _displayedOffset;
            NormalizeOffset();
            if (Mathf.Abs(_displayedOffset - old) > 0.01f)
            {
                ResetRangeTracking();
                ApplyContentPosition();
                RefreshVisibleItems();
            }
        }

        #endregion

        #region 内部 — 重建 / Rebuild

        private void Rebuild()
        {
            ClearActive();
            ClearPool();
            IsInitialized = false;
            if (_itemCount <= 0) return;

            RefreshCachedSizes();
            _stepSize = GetItemStepSize();
            _totalSize = _itemCount * _stepSize;

            // 池子 = 视口可见数 + 双缓冲 + 安全余量（不归一化时可能多出一周期）
            float vpSize = ReadViewportSize();
            int visibleItems = Mathf.CeilToInt(vpSize / _stepSize);
            int needed = visibleItems + bufferCount * 2 + 4;
            if (needed < 3) needed = 3;

            for (int i = 0; i < needed; i++) RentNew();

            ApplyContentLayout();
            _displayedOffset = _totalSize + SnapReferenceOffset(); // 初始在安全区中并带上对齐
            _snapping = false;
            _lastSelectedIndex = -1;

            ApplyContentPosition();
            ResetRangeTracking();
            RefreshVisibleItems();
            IsInitialized = true;
        }

        private void ApplyContentLayout()
        {
            float threeCycle = _totalSize * 3f;
            _contentRect.anchorMin = _contentRect.anchorMax = Vector2.up;
            _contentRect.pivot = Vector2.up;
            if (direction == Direction.Vertical)
                _contentRect.sizeDelta = new Vector2(_cachedItemWidth, threeCycle);
            else
                _contentRect.sizeDelta = new Vector2(threeCycle, _cachedItemWidth);
        }

        #endregion

        #region 内部 — 滚动处理 / Scroll Handling

        /// <summary>
        /// 滚动回调 — 只读偏移 + 刷新可见，不做归一化。
        /// <para>Scroll callback: reads offset and refreshes visible items. No normalization.</para>
        /// </summary>
        private void OnScrollChanged(Vector2 _)
        {
            if (_itemCount == 0 || _snapping) return;
            _displayedOffset = GetScrollOffset();
            RefreshVisibleItems();
        }

        #endregion

        #region 内部 — 可见范围 / Visible Range

        /// <summary>
        /// 渲染可见项。坐标系: item si 的 viewport Y = _displayedOffset - si * stepSize。
        /// <para>Renders visible items. Coordinate: item si viewport Y = _displayedOffset - si * stepSize.</para>
        /// </summary>
        private void RefreshVisibleItems()
        {
            if (_itemCount == 0) return;

            float itemDim = ItemDim();
            float vpSize = ReadViewportSize();

            // item 可见条件: _displayedOffset - si*step + itemDim >= 0  AND  _displayedOffset - si*step <= vpSize
            // → si <= (offset + itemDim) / step  AND  si >= (offset - vpSize) / step
            int first = Mathf.FloorToInt((_displayedOffset - vpSize) / _stepSize) - bufferCount;
            int last  = Mathf.CeilToInt((_displayedOffset + itemDim) / _stepSize) + bufferCount;

            if (first == _lastFirst && last == _lastLast) return;
            _lastFirst = first;
            _lastLast = last;

            // 回收离开范围的
            _toRemoveList.Clear();
            foreach (var kv in _activeItems)
            {
                if (kv.Key < first || kv.Key > last)
                    _toRemoveList.Add(kv.Key);
            }
            foreach (int si in _toRemoveList)
            {
                Recycle(_activeItems[si]);
                _activeItems.Remove(si);
            }

            // 填充进入范围的
            for (int si = first; si <= last; si++)
            {
                if (_activeItems.ContainsKey(si)) continue;

                int dataIndex = ModPositive(si, _itemCount);
                var go = Rent();
                go.SetActive(true);

                var rt = go.transform as RectTransform;
                rt.SetParent(_contentRect, false);
                rt.anchorMin = rt.anchorMax = Vector2.up;
                rt.pivot = Vector2.up;

                float pos = si * _stepSize;
                if (direction == Direction.Vertical)
                {
                    rt.sizeDelta = new Vector2(_cachedItemWidth, itemDim);
                    rt.anchoredPosition = new Vector2(0, -pos);
                }
                else
                {
                    rt.sizeDelta = new Vector2(itemDim, _cachedItemWidth);
                    rt.anchoredPosition = new Vector2(pos, 0);
                }

                OnFillItem?.Invoke(dataIndex, go);
                _activeItems[si] = go;
            }
        }

        #endregion

        #region 内部 — 吸附 / Snap

        private void BeginSnap()
        {
            float snapRef = SnapReferenceOffset();
            float nearest = Mathf.Round((_displayedOffset - snapRef) / _stepSize) * _stepSize + snapRef;

            if (Mathf.Abs(nearest - _displayedOffset) < 0.5f)
            {
                _displayedOffset = nearest;
                ApplyContentPosition();
                ResetRangeTracking();
                RefreshVisibleItems();
                return;
            }

            _snapStartOffset = _displayedOffset;
            _snapTargetOffset = nearest;
            _snapElapsed = 0;
            _snapping = true;
            if (scrollRect != null) scrollRect.velocity = Vector2.zero;
        }

        private void UpdateSnapAnimation()
        {
            _snapElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_snapElapsed / Mathf.Max(snapDuration, 0.01f));
            t = 1f - Mathf.Pow(1f - t, 3f);

            _displayedOffset = Mathf.Lerp(_snapStartOffset, _snapTargetOffset, t);
            ApplyContentPosition();
            ResetRangeTracking();
            RefreshVisibleItems();

            if (t >= 1f)
            {
                _displayedOffset = _snapTargetOffset;
                _snapping = false;
            }
        }

        #endregion

        #region 内部 — 选中通知 / Selection

        private void CheckSelectionChange()
        {
            int idx = SelectedIndex;
            if (idx != _lastSelectedIndex)
            {
                _lastSelectedIndex = idx;
                OnSelectedIndexChanged?.Invoke(idx);
            }
        }

        private void NotifySelection(int dataIndex)
            => OnSelectedIndexChanged?.Invoke(dataIndex);

        #endregion
    }
}

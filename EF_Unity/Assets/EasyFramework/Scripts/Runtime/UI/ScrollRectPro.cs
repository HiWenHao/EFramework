/* 
 * ================================================
 * Describe:      This script is used to enhance the scroll view, copy the UGUI source code also refer to Wenruo code. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-01-28 11:23:34
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-07-12 17:49:32
 * ScriptVersion: 0.2
 * ===============================================
*/
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EasyFramework.UI
{
    /// <summary>
    /// A super component for making a child RectTransform scroll.
    /// </summary>
    /// <remarks>
    /// ScrollRectPro will not do any clipping on its own. Combined with a Mask component, it can be turned into a scroll view.
    /// </remarks>
    [AddComponentMenu("UI/Scroll Rect Pro", 102)]
    [SelectionBase]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class ScrollRectPro : UIBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        private ScrollRectPro() { }

        /// <summary>
        /// A setting for which behavior to use when content moves beyond the confines of its container.
        /// <para>当内容超出其容器的范围时使用的行为设置。</para>
        /// </summary>
        public enum MovementType
        {
            /// <summary>
            /// Unrestricted movement. The content can move forever.
            /// </summary>
            Unrestricted,

            /// <summary>
            /// Elastic movement. The content is allowed to temporarily move beyond the container, but is pulled back elastically.
            /// </summary>
            Elastic,

            /// <summary>
            /// Clamped movement. The content can not be moved beyond its container.
            /// </summary>
            Clamped,
        }
        
        /// <summary>
        /// Current scrolling direction
        /// <para>当前滚动方向</para>
        /// </summary>
        public AxisType Direction {
            get
            {
                return _direction;
            }
            set
            {
                _direction = value;
                if (!_inited && _hasScrollbar && _scrollbar)
                {
                    RectTransform _rect = _scrollbar.GetComponent<RectTransform>();
                    if (_direction == AxisType.Vertical)
                    {
                        _scrollbar.SetDirection(ScrollbarPro.Direction.BottomToTop, true);

                        _rect.anchorMin = Vector2.right;
                        _rect.anchorMax = Vector2.one;
                        _rect.pivot = new Vector2(1f, 0.5f);
                        _rect.sizeDelta = new Vector2(20f, 0);
                    }
                    else
                    {
                        _scrollbar.SetDirection(ScrollbarPro.Direction.LeftToRight, true);

                        _rect.anchorMin = Vector2.zero;
                        _rect.anchorMax = new Vector2(1f, 0f);
                        _rect.pivot = Vector2.zero;
                        _rect.sizeDelta = new Vector2(0, 20f);
                    }
                }
                if (!_inited && Content)
                {
                    if (_direction == AxisType.Vertical)
                    {
                        Content.sizeDelta = new Vector2(-20f, 0f);
                    }
                    else
                    {
                        Content.sizeDelta = new Vector2(0f, -20f);
                    }
                }
            }
        }

        public MovementType movementType = MovementType.Elastic;

        /// <summary>
        /// The content that can be scrolled. It should be a child of the GameObject.
        /// <para>可滚动的内容。它应该是滚动视图的子对象，上面有ScrollRectPro。</para>
        /// </summary>
        public RectTransform Content;

        /// <summary>
        /// The elemental of content that can be scrolled.
        /// <para>滚动内容的元素</para>
        /// </summary>
        public GameObject Elemental;

        /// <summary>
        /// Current scroll view pro element max count.
        /// <para>当前滚动视图元素的最大计数。</para>
        /// </summary>
        public int ElementMaxCount => _maxElementalCount;

        /// <summary>
        /// The current velocity of the content.
        /// <para>当前的滚动速度</para>
        /// </summary>
        /// <remarks>
        /// The velocity is defined in units per second.
        /// <para>速度的单位是每秒。</para>
        /// </remarks>
        public Vector2 Velocity { get { return _velocity; } set { _velocity = value; } }

        /// <summary>
        /// The scroll position as a Vector2 between (0,0) and (1,1) with (0,0) being the lower left corner.
        /// <para>滚动位置，介于(0,0)和(1,1)之间，其中(0,0)是左下角。</para>
        /// </summary>
        public Vector2 NormalizedPosition
        {
            get
            {
                return new Vector2(HorizontalNormalizedPosition, VerticalNormalizedPosition);
            }
            set
            {
                SetNormalizedPosition(value.x, 0);
                SetNormalizedPosition(value.y, 1);
            }
        }

        /// <summary>
        /// The horizontal scroll position as a value between 0 and 1, with 0 being at the left.
        /// <para>水平滚动位置为0到1之间的值，0表示左侧。</para>
        /// </summary>
        public float HorizontalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if ((_contentBounds.size.x <= _viewBounds.size.x) || Mathf.Approximately(_contentBounds.size.x, _viewBounds.size.x))
                    return (_viewBounds.min.x > _contentBounds.min.x) ? 1 : 0;
                return (_viewBounds.min.x - _contentBounds.min.x) / (_contentBounds.size.x - _viewBounds.size.x);
            }
            set
            {
                SetNormalizedPosition(value, 0);
            }
        }

        /// <summary>
        /// The vertical scroll position as a value between 0 and 1, with 0 being at the bottom.
        /// <para>垂直滚动位置为0到1之间的值，0表示底部。</para>
        /// </summary>
        public float VerticalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if ((_contentBounds.size.y <= _viewBounds.size.y) || Mathf.Approximately(_contentBounds.size.y, _viewBounds.size.y))
                    return (_viewBounds.min.y > _contentBounds.min.y) ? 1 : 0;

                return (_viewBounds.min.y - _contentBounds.min.y) / (_contentBounds.size.y - _viewBounds.size.y);
            }
            set
            {
                SetNormalizedPosition(value, 1);
            }
        }


        #region SerializeField
        [SerializeField]
        int _lines = 1;

        [SerializeField]
        bool _inertia = true;

        [SerializeField]
        float _dockSpeed = 20f;

        [SerializeField]
        bool _autoDocking = false;

        [SerializeField]
        int _maxCount = 10;

        [SerializeField]
        bool _hasScrollbar = false;

        /// <summary>
        /// The amount of elasticity to use when the content moves beyond the scroll rect.
        /// </summary>
        [SerializeField]
        float _elasticity = 0.1f;

        /// <summary>
        /// The rate at which movement slows down.
        /// *****Only used when inertia is enabled*****
        /// </summary>
        /// <remarks>
        /// The deceleration rate is the speed reduction per second. A value of 0.5 halves the speed each second. The default is 0.135. The deceleration rate is only used when inertia is enabled.
        /// </remarks>
        [SerializeField]
        float _decelerationRate = 0.135f;

        /// <summary>
        /// Horizontal and vertical spacing
        /// </summary>
        [SerializeField]
        Vector2Int _spacing = new Vector2Int(10, 10);
        
        [SerializeField]
        ScrollbarPro _scrollbar;
        #endregion

        #region Local Field
        private int _minIndex = -1;
        private int _maxIndex = -1;
        private int _maxElementalCount = -1;

        private float _elementWidth;
        private float _elementHeight;
        private float _contentWidth;
        private float _contentHeight;
        private float _contentOffset;

        private bool _canDock;
        private bool _inScrolling;
        private bool _dragging;
        private bool _inited = false;
        private bool _onScrollBarDarg;

        private Bounds _viewBounds;
        private Bounds _contentBounds;
        private Bounds _prevViewBounds;
        private Bounds _prevContentBounds;

        private Vector2 _velocity;
        private Vector2 _onEndOffset;
        private Vector2 _contentEndPostation;
        private Vector2 _prevPosition = Vector2.zero;
        private Vector2 _contentStartPosition = Vector2.zero;
        private Vector2 _pointerStartLocalCursor = Vector2.zero;

        private Vector3[] _corners = new Vector3[4];

        [SerializeField]
        private AxisType _direction;

        private RectTransform _rect;

        private struct ElementInfo
        {
            public Vector3 Postation;
            public GameObject Element;
        };
        private ElementInfo[] _elementInfosArray;
        private Stack<GameObject> _elementsPool;

        private Action<GameObject, int> _callbackFunc;
        #endregion

        public override bool IsActive()
        {
            return base.IsActive() && Content != null;
        }

        protected override void Awake()
        {
            if (!Application.isPlaying) return;
            _rect = GetComponent<RectTransform>();

            if (_hasScrollbar && _scrollbar)
            {
                _scrollbar.onValueChanged.AddListener(SetScrollbarProNormalizedPosition);
                _scrollbar.onScrollDrag.AddListener(OnScrollbarProDragChanged);
            }
            InIt(null, _maxCount);
            UpdateScrollbarProPostation(Vector2.zero);
        }

        protected virtual void LateUpdate()
        {
            if (!Application.isPlaying || !Content || (!_inScrolling && _onScrollBarDarg))
                return;

            float deltaTime = Time.unscaledDeltaTime;
            Vector2 offset = CalculateOffset(Vector2.zero);
            if (deltaTime > 0.0f)
            {
                if (!_dragging && (offset != Vector2.zero || _velocity != Vector2.zero))
                {
                    int _axis = (int)_direction;
                    Vector2 position = Content.anchoredPosition;
                    // Apply spring physics if movement is elastic and content has an offset from the view.
                    if (movementType == MovementType.Elastic && offset[_axis] != 0)
                    {
                        float speed = _velocity[_axis];
                        position[_axis] = Mathf.SmoothDamp(Content.anchoredPosition[_axis], Content.anchoredPosition[_axis] + offset[_axis], ref speed, _elasticity, Mathf.Infinity, deltaTime);

                        float _end = position[_axis] >= 0 ? 1.0f : 5.0f;

                        if (speed < _end)
                        {
                            speed = 0;
                            _inScrolling = false;
                        }

                        _velocity[_axis] = speed;
                        _canDock = false;
                    }
                    // Else move content according to velocity with deceleration applied.
                    else if (_inertia)
                    {
                        _velocity[_axis] *= Mathf.Pow(_decelerationRate, deltaTime);
                        if (Mathf.Abs(_velocity[_axis]) < 3.0f)
                            OnScrollEnd();
                        position[_axis] += _velocity[_axis] * deltaTime;
                    }
                    // If we have neither elaticity or friction, there shouldn't be any velocity.
                    else
                        OnScrollEnd();

                    if (movementType == MovementType.Clamped)
                    {
                        offset = CalculateOffset(position - Content.anchoredPosition);
                        position += offset;
                    }

                    SetContentAnchoredPosition(position);
                }

                if (_dragging && _inertia)
                {
                    Vector3 newVelocity = (Content.anchoredPosition - _prevPosition) / deltaTime;
                    _velocity = Vector3.Lerp(_velocity, newVelocity, deltaTime * 10);
                }
            }

            if (_viewBounds != _prevViewBounds || _contentBounds != _prevContentBounds || Content.anchoredPosition != _prevPosition)
            {
                UpdateScrollbarProPostation(offset);
                UpdateCheck();
                UpdatePrevData();
            }

            if (!_dragging && _autoDocking && _velocity == Vector2.zero && _canDock && !_onScrollBarDarg)
            {
                Docking();
            }
        }

        protected override void OnDisable()
        {
            if (!Application.isPlaying) return;

            _dragging = false;
            _callbackFunc = null;
            _velocity = Vector2.zero;
            if (_hasScrollbar && _scrollbar)
            {
                _scrollbar.onValueChanged.RemoveListener(SetScrollbarProNormalizedPosition);
                _scrollbar.onScrollDrag.RemoveListener(OnScrollbarProDragChanged);
            }
        }

        #region Drag Handler
        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            UpdateBounds();

            _pointerStartLocalCursor = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(((RectTransform)transform), eventData.position, eventData.pressEventCamera, out _pointerStartLocalCursor);
            _contentStartPosition = Content.anchoredPosition;
            _dragging = true;
            _inScrolling = true;
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (!_dragging)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(((RectTransform)transform), eventData.position, eventData.pressEventCamera, out Vector2 localCursor))
                return;

            UpdateBounds();

            var _pointerDelta = localCursor - _pointerStartLocalCursor;
            Vector2 _position = _contentStartPosition + _pointerDelta;

            // Offset to get content into place in the view.
            Vector2 _offset = CalculateOffset(_position - Content.anchoredPosition);
            _position += _offset;
            if (movementType == MovementType.Elastic)
            {
                if (_offset.x != 0)
                    _position.x = _position.x - RubberDelta(_offset.x, _viewBounds.size.x);
                if (_offset.y != 0)
                    _position.y = _position.y - RubberDelta(_offset.y, _viewBounds.size.y);
            }

            SetContentAnchoredPosition(_position);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            _dragging = false;
        }

        #endregion

        #region Private Fcuntion
        /// <summary>
        /// Sets the anchored position of the content.
        /// <para>设置内容的锚定位置。</para>
        /// </summary>
        void SetContentAnchoredPosition(Vector2 position, bool refresh = true)
        {
            if (_direction == AxisType.Vertical)
                position.x = Content.anchoredPosition.x;
            if (_direction == AxisType.Horizontal)
                position.y = Content.anchoredPosition.y;

            if (position != Content.anchoredPosition && refresh)
            {
                Content.anchoredPosition = position;
                UpdateBounds();
            }
        }

        /// <summary>
        /// Helper function to update the previous data fields on a ScrollRectPro. Call this before you change data in the ScrollRectPro.
        /// <para>在ScrollRectPro上更新之前的数据字段的辅助函数。在更改ScrollRectPro中的数据之前调用它。</para>
        /// </summary>
        void UpdatePrevData()
        {
            if (Content == null)
                _prevPosition = Vector2.zero;
            else
                _prevPosition = Content.anchoredPosition;
            _prevViewBounds = _viewBounds;
            _prevContentBounds = _contentBounds;
        }

        /// <summary>
        /// >Set the horizontal or vertical scroll position as a value between 0 and 1, with 0 being at the left or at the bottom.
        /// <para>将水平或垂直滚动位置设置为0到1之间的值，0表示左侧或底部。</para>
        /// </summary>
        /// <param name="value">The position to set, between 0 and 1.<para>要设置的位置，在0到1之间。</para></param>
        /// <param name="axis">The axis to set: 0 for horizontal, 1 for vertical.<para>要设置的轴:0表示水平，1表示垂直。</para></param>
        void SetNormalizedPosition(float value, int axis)
        {
            UpdateBounds();
            // How much the content is larger than the view.
            float hiddenLength = _contentBounds.size[axis] - _viewBounds.size[axis];
            // Where the position of the lower left corner of the content bounds should be, in the space of the view.
            float contentBoundsMinPosition = _viewBounds.min[axis] - value * hiddenLength;
            // The new content localPosition, in the space of the view.
            float newLocalPosition = Content.localPosition[axis] + contentBoundsMinPosition - _contentBounds.min[axis];

            Vector3 localPosition = Content.localPosition;
            if (Mathf.Abs(localPosition[axis] - newLocalPosition) > 0.01f)
            {
                localPosition[axis] = newLocalPosition;
                Content.localPosition = localPosition;
                _velocity[axis] = 0;
                UpdateBounds();
            }
        }

        float RubberDelta(float overStretching, float viewSize)
        {
            return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
        }

        /// <summary>
        /// Calculate the bounds the ScrollRectPro should be using.
        /// <para>计算ScrollRectPro应该使用的边界。</para>
        /// </summary>
        void UpdateBounds()
        {
            _viewBounds = new Bounds(((RectTransform)transform).rect.center, ((RectTransform)transform).rect.size);
            _contentBounds = GetBounds();

            if (Content == null)
                return;

            Vector3 contentSize = _contentBounds.size;
            Vector3 contentPos = _contentBounds.center;
            var contentPivot = Content.pivot;
            AdjustBounds(ref _viewBounds, ref contentPivot, ref contentSize, ref contentPos);
            _contentBounds.size = contentSize;
            _contentBounds.center = contentPos;

            if (movementType == MovementType.Clamped)
            {
                Vector2 delta = Vector2.zero;
                if (_viewBounds.max.x > _contentBounds.max.x)
                {
                    delta.x = Math.Min(_viewBounds.min.x - _contentBounds.min.x, _viewBounds.max.x - _contentBounds.max.x);
                }
                else if (_viewBounds.min.x < _contentBounds.min.x)
                {
                    delta.x = Math.Max(_viewBounds.min.x - _contentBounds.min.x, _viewBounds.max.x - _contentBounds.max.x);
                }

                if (_viewBounds.min.y < _contentBounds.min.y)
                {
                    delta.y = Math.Max(_viewBounds.min.y - _contentBounds.min.y, _viewBounds.max.y - _contentBounds.max.y);
                }
                else if (_viewBounds.max.y > _contentBounds.max.y)
                {
                    delta.y = Math.Min(_viewBounds.min.y - _contentBounds.min.y, _viewBounds.max.y - _contentBounds.max.y);
                }
                if (delta.sqrMagnitude > float.Epsilon)
                {
                    contentPos = Content.anchoredPosition + delta;
                    if (_direction == AxisType.Vertical)
                        contentPos.x = Content.anchoredPosition.x;
                    if (_direction == AxisType.Horizontal)
                        contentPos.y = Content.anchoredPosition.y;
                    AdjustBounds(ref _viewBounds, ref contentPivot, ref contentSize, ref contentPos);
                }
            }
        }

        /// <summary>
        /// 调整范围
        /// </summary>
        /// <param name="viewBounds">视图边界</param>
        /// <param name="contentPivot">内容中心点</param>
        /// <param name="contentSize">内容尺寸</param>
        /// <param name="contentPos">内容位置</param>
        void AdjustBounds(ref Bounds viewBounds, ref Vector2 contentPivot, ref Vector3 contentSize, ref Vector3 contentPos)
        {
            // Make sure content bounds are at least as large as view by adding padding if not.
            // One might think at first that if the content is smaller than the view, scrolling should be allowed.
            // However, that's not how scroll views normally work.
            // Scrolling is *only* possible when content is *larger* than view.
            // We use the pivot of the content rect to decide in which directions the content bounds should be expanded.
            // E.g. if pivot is at top, bounds are expanded downwards.
            // This also works nicely when ContentSizeFitter is used on the content.
            Vector3 excess = viewBounds.size - contentSize;
            if (excess.x > 0)
            {
                contentPos.x -= excess.x * (contentPivot.x - 0.5f);
                contentSize.x = viewBounds.size.x;
            }
            if (excess.y > 0)
            {
                contentPos.y -= excess.y * (contentPivot.y - 0.5f);
                contentSize.y = viewBounds.size.y;
            }
        }

        /// <summary>
        /// 获取范围
        /// </summary>
        /// <returns></returns>
        Bounds GetBounds()
        {
            if (Content == null)
                return new Bounds();
            Content.GetWorldCorners(_corners);
            var viewWorldToLocalMatrix = ((RectTransform)transform).worldToLocalMatrix;

            var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int j = 0; j < 4; j++)
            {
                Vector3 v = viewWorldToLocalMatrix.MultiplyPoint3x4(_corners[j]);
                vMin = Vector3.Min(v, vMin);
                vMax = Vector3.Max(v, vMax);
            }

            var bounds = new Bounds(vMin, Vector3.zero);
            bounds.Encapsulate(vMax);
            return bounds;
        }

        /// <summary>
        /// 计算偏移量
        /// </summary>
        Vector2 CalculateOffset(Vector2 delta)
        {
            Vector2 offset = Vector2.zero;
            if (movementType == MovementType.Unrestricted)
                return offset;

            Vector2 min = _contentBounds.min;
            Vector2 max = _contentBounds.max;

            if (_direction == AxisType.Horizontal)
            {
                min.x += delta.x;
                max.x += delta.x;

                float maxOffset = _viewBounds.max.x - max.x;
                float minOffset = _viewBounds.min.x - min.x;

                if (minOffset < -0.001f)
                    offset.x = minOffset;
                else if (maxOffset > 0.001f)
                    offset.x = maxOffset;
            }
            else /*(direction == AxisType.Vertical)*/
            {
                min.y += delta.y;
                max.y += delta.y;

                float maxOffset = _viewBounds.max.y - max.y;
                float minOffset = _viewBounds.min.y - min.y;

                if (maxOffset > 0.001f)
                    offset.y = maxOffset;
                else if (minOffset < -0.001f)
                    offset.y = minOffset;
            }
            return offset;
        }

        /// <summary>
        /// 从对象池中获取一个对象
        /// </summary>
        GameObject GetPoolsObj()
        {
            GameObject _go = null;
            if (_elementsPool.Count > 0)
                _go = _elementsPool.Pop();
            if (_go == null)
                _go = Instantiate(Elemental);

            _go.transform.SetParent(Content.transform);
            _go.transform.localScale = Vector3.one;
            SetActive(_go, true);

            return _go;
        }

        /// <summary>
        /// Set the element push the pool.
        /// <para>回收一个元素对象</para>
        /// </summary>
        void SetPoolsObj(GameObject element)
        {
            if (element != null)
            {
                _elementsPool.Push(element);
                SetActive(element, false);
            }
        }

        /// <summary>
        /// Set active with the element.
        /// <para>设置元素可见状态</para>
        /// </summary>
        void SetActive(GameObject obj, bool isActive)
        {
            if (obj != null)
            {
                obj.SetActive(isActive);
            }
        }

        /// <summary>
        /// Check the anchor is right.
        /// <para>检查锚点是否正确。</para>
        /// </summary>
        void CheckAnchor(RectTransform rt)
        {
            if (_direction == AxisType.Vertical)
            {
                if (!((rt.anchorMin == new Vector2(0, 1) && rt.anchorMax == new Vector2(0, 1)) ||
                      (rt.anchorMin == new Vector2(0, 1) && rt.anchorMax == new Vector2(1, 1))))
                {
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(1, 1);
                }
            }
            else
            {
                if (!((rt.anchorMin == new Vector2(0, 1) && rt.anchorMax == new Vector2(0, 1)) ||
                      (rt.anchorMin == new Vector2(0, 0) && rt.anchorMax == new Vector2(0, 1))))
                {
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(0, 1);
                }
            }
        }

        /// <summary>
        /// When the scroll update the view.
        /// <para>当滚动时更新视图</para>
        /// </summary>
        void UpdateCheck()
        {
            if (_elementInfosArray == null) return;

            int _count = _elementInfosArray.Length;
            for (int i = 0, length = _count; i < length; i++)
            {
                ElementInfo _element = _elementInfosArray[i];
                GameObject obj = _element.Element;
                Vector3 pos = _element.Postation;
                float rangePos = _direction == AxisType.Vertical ? pos.y : pos.x;

                if (IsOutRange(rangePos))
                {
                    if (obj != null)
                    {
                        SetPoolsObj(obj);
                        _elementInfosArray[i].Element = null;
                    }
                }
                else
                {
                    if (obj == null)
                    {
                        GameObject cell = GetPoolsObj();
                        cell.transform.localPosition = pos;
                        //cell.name = i.ToString();
                        _elementInfosArray[i].Element = cell;
                        CallbackFunction(cell, i);
                    }
                }
            }
        }

        /// <summary>
        /// Check whether it is out of the display range
        /// <para>检查是否超出显示范围</para>
        /// </summary>
        /// <param name="pos">The element position. <para>元素位置</para></param>
        bool IsOutRange(float pos)
        {
            Vector3 listP = Content.anchoredPosition;
            if (_direction == AxisType.Vertical)
            {
                if (pos + listP.y > _elementHeight || pos + listP.y < -_rect.rect.height)
                {
                    return true;
                }
            }
            else
            {
                if (pos + listP.x < -_elementWidth || pos + listP.x > _rect.rect.width)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// When scrolling ends
        /// <para>当滚动结束</para>
        /// </summary>
        void OnScrollEnd()
        {
            _canDock = true;
            _velocity = Vector2.zero;

            float _everySize;
            if (_direction == AxisType.Horizontal)
            {
                _everySize = _elementWidth + _spacing.x;
                _contentOffset = Content.anchoredPosition.x % _everySize;
                _contentOffset = _contentOffset > (_everySize / 2.0f) ? _everySize - _contentOffset : -_contentOffset;
                _onEndOffset = new Vector2(_contentOffset, 0);

            }
            else
            {
                _everySize = _elementHeight + _spacing.y;
                _contentOffset = Content.anchoredPosition.y % _everySize;
                _contentOffset = _contentOffset > (_everySize / 2.0f) ? _everySize - _contentOffset : -_contentOffset;
                _onEndOffset = new Vector2(0, _contentOffset);
            }
            _contentEndPostation = Content.anchoredPosition + _onEndOffset;
            _contentOffset = _direction == AxisType.Horizontal ? _onEndOffset.x : _onEndOffset.y;
        }

        /// <summary>
        /// Show the list with new count.
        /// <para>用新数据更新列表</para>
        /// </summary>
        void ShowList(int num)
        {
            _minIndex = -1;
            _maxIndex = -1;

            if (_direction == AxisType.Vertical)
            {
                float contentSize = (_spacing.y + _elementHeight) * Mathf.CeilToInt((float)num / _lines);
                _contentHeight = contentSize;
                _contentWidth = Content.sizeDelta.x;
                contentSize = contentSize < _rect.rect.height ? _rect.rect.height : contentSize;
                Content.sizeDelta = new Vector2(_contentWidth, contentSize);
                if (num != _maxElementalCount)
                {
                    Content.anchoredPosition = new Vector2(Content.anchoredPosition.x, 0);
                }
            }
            else
            {
                float contentSize = (_spacing.x + _elementWidth) * Mathf.CeilToInt((float)num / _lines);
                _contentWidth = contentSize;
                _contentHeight = Content.sizeDelta.x;
                contentSize = contentSize < _rect.rect.width ? _rect.rect.width : contentSize;
                Content.sizeDelta = new Vector2(contentSize, _contentHeight);
                if (num != _maxElementalCount)
                {
                    Content.anchoredPosition = new Vector2(0, Content.anchoredPosition.y);
                }
            }

            int lastEndIndex = 0;

            if (_inited)
            {
                lastEndIndex = num - _maxElementalCount > 0 ? _maxElementalCount : num;
                //lastEndIndex = m_ClearList ? 0 : lastEndIndex;

                int count = _maxElementalCount;
                for (int i = lastEndIndex; i < count; i++)
                {
                    if (_elementInfosArray[i].Element != null)
                    {
                        SetPoolsObj(_elementInfosArray[i].Element);
                        _elementInfosArray[i].Element = null;
                    }
                }
            }

            ElementInfo[] _tempCellInfos = _elementInfosArray;
            _elementInfosArray = new ElementInfo[num];

            for (int i = 0; i < num; i++)
            {
                if (_maxElementalCount != -1 && i < lastEndIndex)
                {
                    ElementInfo _ei = _tempCellInfos.Length > i ? _tempCellInfos[i] : new ElementInfo();

                    float rPos = _direction == AxisType.Vertical ? _ei.Postation.y : _ei.Postation.x;
                    if (!IsOutRange(rPos))
                    {
                        _minIndex = _minIndex == -1 ? i : _minIndex;
                        _maxIndex = i;

                        if (_ei.Element == null)
                        {
                            _ei.Element = GetPoolsObj();
                        }

                        _ei.Element.transform.GetComponent<RectTransform>().localPosition = _ei.Postation;
                        _ei.Element.name = i.ToString();
                        _ei.Element.SetActive(true);

                        CallbackFunction(_ei.Element, i);
                    }
                    else
                    {
                        SetPoolsObj(_ei.Element);
                        _ei.Element = null;
                    }

                    _elementInfosArray[i] = _ei;
                    continue;
                }

                ElementInfo _element = new ElementInfo();

                if (_direction == AxisType.Vertical)
                {
                    _element.Postation = new Vector3(_elementWidth * (i % _lines) + _spacing.x * (i % _lines), -(_elementHeight * Mathf.FloorToInt(i / _lines) + _spacing.y * Mathf.FloorToInt(i / _lines)), 0);
                }
                else
                {
                    _element.Postation = new Vector3(_elementWidth * Mathf.FloorToInt(i / _lines) + _spacing.x * Mathf.FloorToInt(i / _lines), -(_elementHeight * (i % _lines) + _spacing.y * (i % _lines)), 0);
                }

                float cellPos = _direction == AxisType.Vertical ? _element.Postation.y : _element.Postation.x;
                if (IsOutRange(cellPos))
                {
                    _element.Element = null;
                    _elementInfosArray[i] = _element;
                    continue;
                }

                _minIndex = _minIndex == -1 ? i : _minIndex;
                _maxIndex = i;

                GameObject cell = GetPoolsObj();
                cell.GetComponent<RectTransform>().localPosition = _element.Postation;
                //cell.name = i.ToString();

                _element.Element = cell;
                _elementInfosArray[i] = _element;

                CallbackFunction(cell, i);
            }
            _maxCount = num;
            _maxElementalCount = num;
            _inited = true;
        }

        /// <summary>
        /// The callback event with elements list update.
        /// <para>更新元素列表的回调事件。</para>
        /// </summary>
        void CallbackFunction(GameObject obj, int index)
        {
            _callbackFunc?.Invoke(obj, index);
        }

        /// <summary>
        /// 停靠
        /// </summary>
        void Docking()
        {
            Vector2 _v2 = (_direction == AxisType.Horizontal ? Vector2.right : Vector2.up) * _contentOffset;
            Content.anchoredPosition += _dockSpeed * Time.deltaTime * _v2;
            UpdateBounds();

            Vector2 _disV2 = _contentEndPostation - Content.anchoredPosition;
            if (MathF.Abs(_direction == AxisType.Horizontal ? _disV2.x : _disV2.y) <= 2.0f)
            {
                Content.anchoredPosition = _contentEndPostation;
                _inScrolling = false;
                _canDock = false;
            }
        }

        /// <summary>
        /// 设置滑动条位置
        /// </summary>
        void SetScrollbarProNormalizedPosition(float value)
        {
            SetNormalizedPosition(value, (int)_direction);
            UpdateCheck();

        }

        /// <summary>
        /// 当滑动条发生变化时
        /// </summary>
        void OnScrollbarProDragChanged(bool drag)
        {
            _onScrollBarDarg = drag;
            if (_onScrollBarDarg)
            {
                _canDock = false;
                _inScrolling = false;
            }
        }

        /// <summary>
        /// Update scrollbar pro postation.
        /// <para>更新滑动条位置</para>
        /// </summary>
        void UpdateScrollbarProPostation(Vector2 offset)
        {
            if (_onScrollBarDarg || !_hasScrollbar || !_scrollbar)
                return;

            if (_direction == AxisType.Horizontal)
            {
                if (_contentBounds.size.x > 0)
                    _scrollbar.size = Mathf.Clamp01((_viewBounds.size.x - Mathf.Abs(offset.x)) / _contentBounds.size.x);
                else 
                    _scrollbar.size = 1;

                _scrollbar.value = HorizontalNormalizedPosition;
            }
            else
            {
                if (_contentBounds.size.y > 0)
                    _scrollbar.size = Mathf.Clamp01((_viewBounds.size.y - Mathf.Abs(offset.y)) / _contentBounds.size.y);
                else
                    _scrollbar.size = 1;

                _scrollbar.value = VerticalNormalizedPosition;
            }
        }

        #endregion

        /// <summary>
        /// Initialize the scroll view pro.
        /// <para>初始化滚动视图</para>
        /// </summary>
        /// <param name="callback">The update event. <para>更新回调函数</para></param>
        /// <param name="maxCount">The elements list max count. <para>最大元素数量</para></param>
        public void InIt(Action<GameObject, int> callback, int maxCount)
        {
            _callbackFunc = null;
            _callbackFunc = callback;

            if (_inited)
            {
                UpdateLines(maxCount);
                return;
            }

            _elementsPool = new Stack<GameObject>();
            SetPoolsObj(Elemental);

            RectTransform elementRectTrans = Elemental.GetComponent<RectTransform>();
            elementRectTrans.pivot = new Vector2(0f, 1f);
            elementRectTrans.anchorMax = new Vector2(0f, 1f);
            Vector2 _v2 = elementRectTrans.sizeDelta;
            if (_v2.x == 0)
                elementRectTrans.sizeDelta = new Vector2(Screen.width, _v2.y);
            if (_v2.y == 0)
                elementRectTrans.sizeDelta = new Vector2(_v2.x, Screen.height);
            CheckAnchor(elementRectTrans);
            elementRectTrans.anchoredPosition = Vector2.zero;

            _elementHeight = elementRectTrans.rect.height;
            _elementWidth = elementRectTrans.rect.width;
            Content.pivot = new Vector2(0f, 1f);
            _contentHeight = Content.rect.height;
            _contentWidth = Content.rect.width;

            CheckAnchor(Content);
            _inited = true;
            ShowList(maxCount);
        }

        /// <summary>
        /// Back to starting point.
        /// <para>回到起点</para>
        /// </summary>
        public void GoToStart()
        {
            GoToElementPosWithIndex(0);
        }

        /// <summary>
        /// Go to position with element index.
        /// <para>通过索引到达元素的坐标位置</para>
        /// </summary>
        /// <param name="index">The element index. 索引ID</param>
        public void GoToElementPosWithIndex(int index)
        {
            if (null == _elementInfosArray || _elementInfosArray.Length == 0) return;

            int theFirstIndex = index - index % _lines;
            var tmpIndex = theFirstIndex + _maxIndex;

            int theLastIndex = tmpIndex > _maxElementalCount - 1 ? _maxElementalCount - 1 : tmpIndex;

            if (theLastIndex == _maxElementalCount - 1)
            {
                var shortOfNum = _maxElementalCount % _lines == 0 ? 0 : _lines - _maxElementalCount % _lines;
                theFirstIndex = theLastIndex - _maxIndex + shortOfNum;
            }

            Vector2 newPos = _elementInfosArray[theFirstIndex].Postation;
            if (_direction == AxisType.Vertical)
            {
                var posY = index <= _lines ? -newPos.y : -newPos.y - _spacing.y;
                Content.anchoredPosition = new Vector2(Content.anchoredPosition.x, posY);
            }
            else
            {
                var posX = index <= _lines ? -newPos.x : -newPos.x + _spacing.x;
                Content.anchoredPosition = new Vector2(posX, Content.anchoredPosition.y);
            }
        }

        /// <summary>
        /// Changed the lines of Scroll view pro.
        /// <para>更改行/列数</para>
        /// </summary>
        /// <param name="maxCount">Max count. <para>最大行/列数</para></param>
        public void UpdateLines(int maxCount)
        {
            if (!_inited) return;
            ShowList(maxCount);
        }

        /// <summary>
        /// Update the list.
        /// <para>更新展示列表</para>
        /// </summary>
        public void UpdateList()
        {
            for (int i = 0, length = _elementInfosArray.Length; i < length; i++)
            {
                ElementInfo _element = _elementInfosArray[i];
                if (_element.Element != null)
                {
                    float rangePos = _direction == AxisType.Vertical ? _element.Postation.y : _element.Postation.x;
                    if (!IsOutRange(rangePos))
                    {
                        CallbackFunction(_element.Element, i);
                    }
                }
            }
        }

        /// <summary>
        /// Update the element with index.
        /// <para>按索引更新元素</para>
        /// </summary>
        /// <param name="index">The element index.<para>元素索引</para></param>
        public void UpdateElement(int index)
        {
            ElementInfo element = _elementInfosArray[index];
            if (element.Element != null)
            {
                float rangePos = _direction == AxisType.Vertical ? element.Postation.y : element.Postation.x;
                if (!IsOutRange(rangePos))
                {
                    CallbackFunction(element.Element, index);
                }
            }
        }
    }
}

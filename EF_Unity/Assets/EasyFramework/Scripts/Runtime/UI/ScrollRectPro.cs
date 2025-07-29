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
                return m_direction;
            }
            set
            {
                m_direction = value;
                if (!m_Inited && m_HasScrollbar && m_Scrollbar)
                {
                    RectTransform _rect = m_Scrollbar.GetComponent<RectTransform>();
                    if (m_direction == AxisType.Vertical)
                    {
                        m_Scrollbar.SetDirection(ScrollbarPro.Direction.BottomToTop, true);

                        _rect.anchorMin = Vector2.right;
                        _rect.anchorMax = Vector2.one;
                        _rect.pivot = new Vector2(1f, 0.5f);
                        _rect.sizeDelta = new Vector2(20f, 0);
                    }
                    else
                    {
                        m_Scrollbar.SetDirection(ScrollbarPro.Direction.LeftToRight, true);

                        _rect.anchorMin = Vector2.zero;
                        _rect.anchorMax = new Vector2(1f, 0f);
                        _rect.pivot = Vector2.zero;
                        _rect.sizeDelta = new Vector2(0, 20f);
                    }
                }
                if (!m_Inited && content)
                {
                    if (m_direction == AxisType.Vertical)
                    {
                        content.sizeDelta = new Vector2(-20f, 0f);
                    }
                    else
                    {
                        content.sizeDelta = new Vector2(0f, -20f);
                    }
                }
            }
        }

        public MovementType movementType = MovementType.Elastic;

        /// <summary>
        /// The content that can be scrolled. It should be a child of the GameObject.
        /// <para>可滚动的内容。它应该是滚动视图的子对象，上面有ScrollRectPro。</para>
        /// </summary>
        public RectTransform content;

        /// <summary>
        /// The elemental of content that can be scrolled.
        /// <para>滚动内容的元素</para>
        /// </summary>
        public GameObject Elemental;

        /// <summary>
        /// Current scroll view pro element max count.
        /// <para>当前滚动视图元素的最大计数。</para>
        /// </summary>
        public int ElementMaxCount => m_MaxElementalCount;

        /// <summary>
        /// The current velocity of the content.
        /// <para>当前的滚动速度</para>
        /// </summary>
        /// <remarks>
        /// The velocity is defined in units per second.
        /// <para>速度的单位是每秒。</para>
        /// </remarks>
        public Vector2 Velocity { get { return m_Velocity; } set { m_Velocity = value; } }

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
                if ((m_ContentBounds.size.x <= m_ViewBounds.size.x) || Mathf.Approximately(m_ContentBounds.size.x, m_ViewBounds.size.x))
                    return (m_ViewBounds.min.x > m_ContentBounds.min.x) ? 1 : 0;
                return (m_ViewBounds.min.x - m_ContentBounds.min.x) / (m_ContentBounds.size.x - m_ViewBounds.size.x);
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
                if ((m_ContentBounds.size.y <= m_ViewBounds.size.y) || Mathf.Approximately(m_ContentBounds.size.y, m_ViewBounds.size.y))
                    return (m_ViewBounds.min.y > m_ContentBounds.min.y) ? 1 : 0;

                return (m_ViewBounds.min.y - m_ContentBounds.min.y) / (m_ContentBounds.size.y - m_ViewBounds.size.y);
            }
            set
            {
                SetNormalizedPosition(value, 1);
            }
        }


        #region SerializeField
        [SerializeField]
        int m_Lines = 1;

        [SerializeField]
        bool m_Inertia = true;

        [SerializeField]
        float m_DockSpeed = 20f;

        [SerializeField]
        bool m_AutoDocking = false;

        [SerializeField]
        int m_MaxCount = 10;

        [SerializeField]
        bool m_HasScrollbar = false;

        /// <summary>
        /// The amount of elasticity to use when the content moves beyond the scroll rect.
        /// </summary>
        [SerializeField]
        float m_Elasticity = 0.1f;

        /// <summary>
        /// The rate at which movement slows down.
        /// *****Only used when inertia is enabled*****
        /// </summary>
        /// <remarks>
        /// The deceleration rate is the speed reduction per second. A value of 0.5 halves the speed each second. The default is 0.135. The deceleration rate is only used when inertia is enabled.
        /// </remarks>
        [SerializeField]
        float m_DecelerationRate = 0.135f;

        /// <summary>
        /// Horizontal and vertical spacing
        /// </summary>
        [SerializeField]
        Vector2Int _spacing = new Vector2Int(10, 10);
        
        [SerializeField]
        ScrollbarPro m_Scrollbar;
        #endregion

        #region Local Field
        private int m_MinIndex = -1;
        private int m_MaxIndex = -1;
        private int m_MaxElementalCount = -1;

        private float m_ElementWidth;
        private float m_ElementHeight;
        private float m_ContentWidth;
        private float m_ContentHeight;
        private float m_ContentOffset;

        private bool m_CanDock;
        private bool m_InScrolling;
        private bool m_Dragging;
        private bool m_Inited = false;
        private bool m_OnScrollBarDarg;

        private Bounds m_ViewBounds;
        private Bounds m_ContentBounds;
        private Bounds m_PrevViewBounds;
        private Bounds m_PrevContentBounds;

        private Vector2 m_Velocity;
        private Vector2 m_OnEndOffset;
        private Vector2 m_ContentEndPostation;
        private Vector2 m_PrevPosition = Vector2.zero;
        private Vector2 m_ContentStartPosition = Vector2.zero;
        private Vector2 m_PointerStartLocalCursor = Vector2.zero;

        private Vector3[] m_Corners = new Vector3[4];

        [SerializeField]
        private AxisType m_direction;

        private RectTransform m_Rect;

        private struct ElementInfo
        {
            public Vector3 Postation;
            public GameObject Element;
        };
        private ElementInfo[] m_ElementInfosArray;
        private Stack<GameObject> m_ElementsPool;

        private Action<GameObject, int> m_CallbackFunc;
        #endregion

        public override bool IsActive()
        {
            return base.IsActive() && content != null;
        }

        protected override void Awake()
        {
            if (!Application.isPlaying) return;
            m_Rect = GetComponent<RectTransform>();

            if (m_HasScrollbar && m_Scrollbar)
            {
                m_Scrollbar.onValueChanged.AddListener(SetScrollbarProNormalizedPosition);
                m_Scrollbar.onScrollDrag.AddListener(OnScrollbarProDragChanged);
            }
            InIt(null, m_MaxCount);
            UpdateScrollbarProPostation(Vector2.zero);
        }

        protected virtual void LateUpdate()
        {
            if (!Application.isPlaying || !content || (!m_InScrolling && m_OnScrollBarDarg))
                return;

            float deltaTime = Time.unscaledDeltaTime;
            Vector2 offset = CalculateOffset(Vector2.zero);
            if (deltaTime > 0.0f)
            {
                if (!m_Dragging && (offset != Vector2.zero || m_Velocity != Vector2.zero))
                {
                    int _axis = (int)m_direction;
                    Vector2 position = content.anchoredPosition;
                    // Apply spring physics if movement is elastic and content has an offset from the view.
                    if (movementType == MovementType.Elastic && offset[_axis] != 0)
                    {
                        float speed = m_Velocity[_axis];
                        position[_axis] = Mathf.SmoothDamp(content.anchoredPosition[_axis], content.anchoredPosition[_axis] + offset[_axis], ref speed, m_Elasticity, Mathf.Infinity, deltaTime);

                        float _end = position[_axis] >= 0 ? 1.0f : 5.0f;

                        if (speed < _end)
                        {
                            speed = 0;
                            m_InScrolling = false;
                        }

                        m_Velocity[_axis] = speed;
                        m_CanDock = false;
                    }
                    // Else move content according to velocity with deceleration applied.
                    else if (m_Inertia)
                    {
                        m_Velocity[_axis] *= Mathf.Pow(m_DecelerationRate, deltaTime);
                        if (Mathf.Abs(m_Velocity[_axis]) < 3.0f)
                            OnScrollEnd();
                        position[_axis] += m_Velocity[_axis] * deltaTime;
                    }
                    // If we have neither elaticity or friction, there shouldn't be any velocity.
                    else
                        OnScrollEnd();

                    if (movementType == MovementType.Clamped)
                    {
                        offset = CalculateOffset(position - content.anchoredPosition);
                        position += offset;
                    }

                    SetContentAnchoredPosition(position);
                }

                if (m_Dragging && m_Inertia)
                {
                    Vector3 newVelocity = (content.anchoredPosition - m_PrevPosition) / deltaTime;
                    m_Velocity = Vector3.Lerp(m_Velocity, newVelocity, deltaTime * 10);
                }
            }

            if (m_ViewBounds != m_PrevViewBounds || m_ContentBounds != m_PrevContentBounds || content.anchoredPosition != m_PrevPosition)
            {
                UpdateScrollbarProPostation(offset);
                UpdateCheck();
                UpdatePrevData();
            }

            if (!m_Dragging && m_AutoDocking && m_Velocity == Vector2.zero && m_CanDock && !m_OnScrollBarDarg)
            {
                Docking();
            }
        }

        protected override void OnDisable()
        {
            if (!Application.isPlaying) return;

            m_Dragging = false;
            m_CallbackFunc = null;
            m_Velocity = Vector2.zero;
            if (m_HasScrollbar && m_Scrollbar)
            {
                m_Scrollbar.onValueChanged.RemoveListener(SetScrollbarProNormalizedPosition);
                m_Scrollbar.onScrollDrag.RemoveListener(OnScrollbarProDragChanged);
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

            m_PointerStartLocalCursor = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(((RectTransform)transform), eventData.position, eventData.pressEventCamera, out m_PointerStartLocalCursor);
            m_ContentStartPosition = content.anchoredPosition;
            m_Dragging = true;
            m_InScrolling = true;
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (!m_Dragging)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(((RectTransform)transform), eventData.position, eventData.pressEventCamera, out Vector2 localCursor))
                return;

            UpdateBounds();

            var _pointerDelta = localCursor - m_PointerStartLocalCursor;
            Vector2 _position = m_ContentStartPosition + _pointerDelta;

            // Offset to get content into place in the view.
            Vector2 _offset = CalculateOffset(_position - content.anchoredPosition);
            _position += _offset;
            if (movementType == MovementType.Elastic)
            {
                if (_offset.x != 0)
                    _position.x = _position.x - RubberDelta(_offset.x, m_ViewBounds.size.x);
                if (_offset.y != 0)
                    _position.y = _position.y - RubberDelta(_offset.y, m_ViewBounds.size.y);
            }

            SetContentAnchoredPosition(_position);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_Dragging = false;
        }

        #endregion

        #region Private Fcuntion
        /// <summary>
        /// Sets the anchored position of the content.
        /// <para>设置内容的锚定位置。</para>
        /// </summary>
        void SetContentAnchoredPosition(Vector2 position, bool refresh = true)
        {
            if (m_direction == AxisType.Vertical)
                position.x = content.anchoredPosition.x;
            if (m_direction == AxisType.Horizontal)
                position.y = content.anchoredPosition.y;

            if (position != content.anchoredPosition && refresh)
            {
                content.anchoredPosition = position;
                UpdateBounds();
            }
        }

        /// <summary>
        /// Helper function to update the previous data fields on a ScrollRectPro. Call this before you change data in the ScrollRectPro.
        /// <para>在ScrollRectPro上更新之前的数据字段的辅助函数。在更改ScrollRectPro中的数据之前调用它。</para>
        /// </summary>
        void UpdatePrevData()
        {
            if (content == null)
                m_PrevPosition = Vector2.zero;
            else
                m_PrevPosition = content.anchoredPosition;
            m_PrevViewBounds = m_ViewBounds;
            m_PrevContentBounds = m_ContentBounds;
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
            float hiddenLength = m_ContentBounds.size[axis] - m_ViewBounds.size[axis];
            // Where the position of the lower left corner of the content bounds should be, in the space of the view.
            float contentBoundsMinPosition = m_ViewBounds.min[axis] - value * hiddenLength;
            // The new content localPosition, in the space of the view.
            float newLocalPosition = content.localPosition[axis] + contentBoundsMinPosition - m_ContentBounds.min[axis];

            Vector3 localPosition = content.localPosition;
            if (Mathf.Abs(localPosition[axis] - newLocalPosition) > 0.01f)
            {
                localPosition[axis] = newLocalPosition;
                content.localPosition = localPosition;
                m_Velocity[axis] = 0;
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
            m_ViewBounds = new Bounds(((RectTransform)transform).rect.center, ((RectTransform)transform).rect.size);
            m_ContentBounds = GetBounds();

            if (content == null)
                return;

            Vector3 contentSize = m_ContentBounds.size;
            Vector3 contentPos = m_ContentBounds.center;
            var contentPivot = content.pivot;
            AdjustBounds(ref m_ViewBounds, ref contentPivot, ref contentSize, ref contentPos);
            m_ContentBounds.size = contentSize;
            m_ContentBounds.center = contentPos;

            if (movementType == MovementType.Clamped)
            {
                Vector2 delta = Vector2.zero;
                if (m_ViewBounds.max.x > m_ContentBounds.max.x)
                {
                    delta.x = Math.Min(m_ViewBounds.min.x - m_ContentBounds.min.x, m_ViewBounds.max.x - m_ContentBounds.max.x);
                }
                else if (m_ViewBounds.min.x < m_ContentBounds.min.x)
                {
                    delta.x = Math.Max(m_ViewBounds.min.x - m_ContentBounds.min.x, m_ViewBounds.max.x - m_ContentBounds.max.x);
                }

                if (m_ViewBounds.min.y < m_ContentBounds.min.y)
                {
                    delta.y = Math.Max(m_ViewBounds.min.y - m_ContentBounds.min.y, m_ViewBounds.max.y - m_ContentBounds.max.y);
                }
                else if (m_ViewBounds.max.y > m_ContentBounds.max.y)
                {
                    delta.y = Math.Min(m_ViewBounds.min.y - m_ContentBounds.min.y, m_ViewBounds.max.y - m_ContentBounds.max.y);
                }
                if (delta.sqrMagnitude > float.Epsilon)
                {
                    contentPos = content.anchoredPosition + delta;
                    if (m_direction == AxisType.Vertical)
                        contentPos.x = content.anchoredPosition.x;
                    if (m_direction == AxisType.Horizontal)
                        contentPos.y = content.anchoredPosition.y;
                    AdjustBounds(ref m_ViewBounds, ref contentPivot, ref contentSize, ref contentPos);
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
            if (content == null)
                return new Bounds();
            content.GetWorldCorners(m_Corners);
            var viewWorldToLocalMatrix = ((RectTransform)transform).worldToLocalMatrix;

            var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int j = 0; j < 4; j++)
            {
                Vector3 v = viewWorldToLocalMatrix.MultiplyPoint3x4(m_Corners[j]);
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

            Vector2 min = m_ContentBounds.min;
            Vector2 max = m_ContentBounds.max;

            if (m_direction == AxisType.Horizontal)
            {
                min.x += delta.x;
                max.x += delta.x;

                float maxOffset = m_ViewBounds.max.x - max.x;
                float minOffset = m_ViewBounds.min.x - min.x;

                if (minOffset < -0.001f)
                    offset.x = minOffset;
                else if (maxOffset > 0.001f)
                    offset.x = maxOffset;
            }
            else /*(direction == AxisType.Vertical)*/
            {
                min.y += delta.y;
                max.y += delta.y;

                float maxOffset = m_ViewBounds.max.y - max.y;
                float minOffset = m_ViewBounds.min.y - min.y;

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
            if (m_ElementsPool.Count > 0)
                _go = m_ElementsPool.Pop();
            if (_go == null)
                _go = Instantiate(Elemental);

            _go.transform.SetParent(content.transform);
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
                m_ElementsPool.Push(element);
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
            if (m_direction == AxisType.Vertical)
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
            if (m_ElementInfosArray == null) return;

            int _count = m_ElementInfosArray.Length;
            for (int i = 0, length = _count; i < length; i++)
            {
                ElementInfo _element = m_ElementInfosArray[i];
                GameObject obj = _element.Element;
                Vector3 pos = _element.Postation;
                float rangePos = m_direction == AxisType.Vertical ? pos.y : pos.x;

                if (IsOutRange(rangePos))
                {
                    if (obj != null)
                    {
                        SetPoolsObj(obj);
                        m_ElementInfosArray[i].Element = null;
                    }
                }
                else
                {
                    if (obj == null)
                    {
                        GameObject cell = GetPoolsObj();
                        cell.transform.localPosition = pos;
                        //cell.name = i.ToString();
                        m_ElementInfosArray[i].Element = cell;
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
            Vector3 listP = content.anchoredPosition;
            if (m_direction == AxisType.Vertical)
            {
                if (pos + listP.y > m_ElementHeight || pos + listP.y < -m_Rect.rect.height)
                {
                    return true;
                }
            }
            else
            {
                if (pos + listP.x < -m_ElementWidth || pos + listP.x > m_Rect.rect.width)
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
            m_CanDock = true;
            m_Velocity = Vector2.zero;

            float _everySize;
            if (m_direction == AxisType.Horizontal)
            {
                _everySize = m_ElementWidth + _spacing.x;
                m_ContentOffset = content.anchoredPosition.x % _everySize;
                m_ContentOffset = m_ContentOffset > (_everySize / 2.0f) ? _everySize - m_ContentOffset : -m_ContentOffset;
                m_OnEndOffset = new Vector2(m_ContentOffset, 0);

            }
            else
            {
                _everySize = m_ElementHeight + _spacing.y;
                m_ContentOffset = content.anchoredPosition.y % _everySize;
                m_ContentOffset = m_ContentOffset > (_everySize / 2.0f) ? _everySize - m_ContentOffset : -m_ContentOffset;
                m_OnEndOffset = new Vector2(0, m_ContentOffset);
            }
            m_ContentEndPostation = content.anchoredPosition + m_OnEndOffset;
            m_ContentOffset = m_direction == AxisType.Horizontal ? m_OnEndOffset.x : m_OnEndOffset.y;
        }

        /// <summary>
        /// Show the list with new count.
        /// <para>用新数据更新列表</para>
        /// </summary>
        void ShowList(int num)
        {
            m_MinIndex = -1;
            m_MaxIndex = -1;

            if (m_direction == AxisType.Vertical)
            {
                float contentSize = (_spacing.y + m_ElementHeight) * Mathf.CeilToInt((float)num / m_Lines);
                m_ContentHeight = contentSize;
                m_ContentWidth = content.sizeDelta.x;
                contentSize = contentSize < m_Rect.rect.height ? m_Rect.rect.height : contentSize;
                content.sizeDelta = new Vector2(m_ContentWidth, contentSize);
                if (num != m_MaxElementalCount)
                {
                    content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0);
                }
            }
            else
            {
                float contentSize = (_spacing.x + m_ElementWidth) * Mathf.CeilToInt((float)num / m_Lines);
                m_ContentWidth = contentSize;
                m_ContentHeight = content.sizeDelta.x;
                contentSize = contentSize < m_Rect.rect.width ? m_Rect.rect.width : contentSize;
                content.sizeDelta = new Vector2(contentSize, m_ContentHeight);
                if (num != m_MaxElementalCount)
                {
                    content.anchoredPosition = new Vector2(0, content.anchoredPosition.y);
                }
            }

            int lastEndIndex = 0;

            if (m_Inited)
            {
                lastEndIndex = num - m_MaxElementalCount > 0 ? m_MaxElementalCount : num;
                //lastEndIndex = m_ClearList ? 0 : lastEndIndex;

                int count = m_MaxElementalCount;
                for (int i = lastEndIndex; i < count; i++)
                {
                    if (m_ElementInfosArray[i].Element != null)
                    {
                        SetPoolsObj(m_ElementInfosArray[i].Element);
                        m_ElementInfosArray[i].Element = null;
                    }
                }
            }

            ElementInfo[] _tempCellInfos = m_ElementInfosArray;
            m_ElementInfosArray = new ElementInfo[num];

            for (int i = 0; i < num; i++)
            {
                if (m_MaxElementalCount != -1 && i < lastEndIndex)
                {
                    ElementInfo _ei = _tempCellInfos.Length > i ? _tempCellInfos[i] : new ElementInfo();

                    float rPos = m_direction == AxisType.Vertical ? _ei.Postation.y : _ei.Postation.x;
                    if (!IsOutRange(rPos))
                    {
                        m_MinIndex = m_MinIndex == -1 ? i : m_MinIndex;
                        m_MaxIndex = i;

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

                    m_ElementInfosArray[i] = _ei;
                    continue;
                }

                ElementInfo _element = new ElementInfo();

                if (m_direction == AxisType.Vertical)
                {
                    _element.Postation = new Vector3(m_ElementWidth * (i % m_Lines) + _spacing.x * (i % m_Lines), -(m_ElementHeight * Mathf.FloorToInt(i / m_Lines) + _spacing.y * Mathf.FloorToInt(i / m_Lines)), 0);
                }
                else
                {
                    _element.Postation = new Vector3(m_ElementWidth * Mathf.FloorToInt(i / m_Lines) + _spacing.x * Mathf.FloorToInt(i / m_Lines), -(m_ElementHeight * (i % m_Lines) + _spacing.y * (i % m_Lines)), 0);
                }

                float cellPos = m_direction == AxisType.Vertical ? _element.Postation.y : _element.Postation.x;
                if (IsOutRange(cellPos))
                {
                    _element.Element = null;
                    m_ElementInfosArray[i] = _element;
                    continue;
                }

                m_MinIndex = m_MinIndex == -1 ? i : m_MinIndex;
                m_MaxIndex = i;

                GameObject cell = GetPoolsObj();
                cell.GetComponent<RectTransform>().localPosition = _element.Postation;
                //cell.name = i.ToString();

                _element.Element = cell;
                m_ElementInfosArray[i] = _element;

                CallbackFunction(cell, i);
            }
            m_MaxCount = num;
            m_MaxElementalCount = num;
            m_Inited = true;
        }

        /// <summary>
        /// The callback event with elements list update.
        /// <para>更新元素列表的回调事件。</para>
        /// </summary>
        void CallbackFunction(GameObject obj, int index)
        {
            m_CallbackFunc?.Invoke(obj, index);
        }

        /// <summary>
        /// 停靠
        /// </summary>
        void Docking()
        {
            Vector2 _v2 = (m_direction == AxisType.Horizontal ? Vector2.right : Vector2.up) * m_ContentOffset;
            content.anchoredPosition += m_DockSpeed * Time.deltaTime * _v2;
            UpdateBounds();

            Vector2 _disV2 = m_ContentEndPostation - content.anchoredPosition;
            if (MathF.Abs(m_direction == AxisType.Horizontal ? _disV2.x : _disV2.y) <= 2.0f)
            {
                content.anchoredPosition = m_ContentEndPostation;
                m_InScrolling = false;
                m_CanDock = false;
            }
        }

        /// <summary>
        /// 设置滑动条位置
        /// </summary>
        void SetScrollbarProNormalizedPosition(float value)
        {
            SetNormalizedPosition(value, (int)m_direction);
            UpdateCheck();

        }

        /// <summary>
        /// 当滑动条发生变化时
        /// </summary>
        void OnScrollbarProDragChanged(bool drag)
        {
            m_OnScrollBarDarg = drag;
            if (m_OnScrollBarDarg)
            {
                m_CanDock = false;
                m_InScrolling = false;
            }
        }

        /// <summary>
        /// Update scrollbar pro postation.
        /// <para>更新滑动条位置</para>
        /// </summary>
        void UpdateScrollbarProPostation(Vector2 offset)
        {
            if (m_OnScrollBarDarg || !m_HasScrollbar || !m_Scrollbar)
                return;

            if (m_direction == AxisType.Horizontal)
            {
                if (m_ContentBounds.size.x > 0)
                    m_Scrollbar.size = Mathf.Clamp01((m_ViewBounds.size.x - Mathf.Abs(offset.x)) / m_ContentBounds.size.x);
                else 
                    m_Scrollbar.size = 1;

                m_Scrollbar.value = HorizontalNormalizedPosition;
            }
            else
            {
                if (m_ContentBounds.size.y > 0)
                    m_Scrollbar.size = Mathf.Clamp01((m_ViewBounds.size.y - Mathf.Abs(offset.y)) / m_ContentBounds.size.y);
                else
                    m_Scrollbar.size = 1;

                m_Scrollbar.value = VerticalNormalizedPosition;
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
            m_CallbackFunc = null;
            m_CallbackFunc = callback;

            if (m_Inited)
            {
                UpdateLines(maxCount);
                return;
            }

            m_ElementsPool = new Stack<GameObject>();
            SetPoolsObj(Elemental);

            RectTransform _elementRectTrans = Elemental.GetComponent<RectTransform>();
            _elementRectTrans.pivot = new Vector2(0f, 1f);
            _elementRectTrans.anchorMax = new Vector2(0f, 1f);
            Vector2 _v2 = _elementRectTrans.sizeDelta;
            if (_v2.x == 0)
                _elementRectTrans.sizeDelta = new Vector2(Screen.width, _v2.y);
            if (_v2.y == 0)
                _elementRectTrans.sizeDelta = new Vector2(_v2.x, Screen.height);
            CheckAnchor(_elementRectTrans);
            _elementRectTrans.anchoredPosition = Vector2.zero;

            m_ElementHeight = _elementRectTrans.rect.height;
            m_ElementWidth = _elementRectTrans.rect.width;
            content.pivot = new Vector2(0f, 1f);
            m_ContentHeight = content.rect.height;
            m_ContentWidth = content.rect.width;

            CheckAnchor(content);
            m_Inited = true;
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
            if (null == m_ElementInfosArray || m_ElementInfosArray.Length == 0) return;

            int theFirstIndex = index - index % m_Lines;
            var tmpIndex = theFirstIndex + m_MaxIndex;

            int theLastIndex = tmpIndex > m_MaxElementalCount - 1 ? m_MaxElementalCount - 1 : tmpIndex;

            if (theLastIndex == m_MaxElementalCount - 1)
            {
                var shortOfNum = m_MaxElementalCount % m_Lines == 0 ? 0 : m_Lines - m_MaxElementalCount % m_Lines;
                theFirstIndex = theLastIndex - m_MaxIndex + shortOfNum;
            }

            Vector2 newPos = m_ElementInfosArray[theFirstIndex].Postation;
            if (m_direction == AxisType.Vertical)
            {
                var posY = index <= m_Lines ? -newPos.y : -newPos.y - _spacing.y;
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, posY);
            }
            else
            {
                var posX = index <= m_Lines ? -newPos.x : -newPos.x + _spacing.x;
                content.anchoredPosition = new Vector2(posX, content.anchoredPosition.y);
            }
        }

        /// <summary>
        /// Changed the lines of Scroll view pro.
        /// <para>更改行/列数</para>
        /// </summary>
        /// <param name="maxCount">Max count. <para>最大行/列数</para></param>
        public void UpdateLines(int maxCount)
        {
            if (!m_Inited) return;
            ShowList(maxCount);
        }

        /// <summary>
        /// Update the list.
        /// <para>更新展示列表</para>
        /// </summary>
        public void UpdateList()
        {
            for (int i = 0, length = m_ElementInfosArray.Length; i < length; i++)
            {
                ElementInfo _element = m_ElementInfosArray[i];
                if (_element.Element != null)
                {
                    float rangePos = m_direction == AxisType.Vertical ? _element.Postation.y : _element.Postation.x;
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
            ElementInfo _element = m_ElementInfosArray[index];
            if (_element.Element != null)
            {
                float rangePos = m_direction == AxisType.Vertical ? _element.Postation.y : _element.Postation.x;
                if (!IsOutRange(rangePos))
                {
                    CallbackFunction(_element.Element, index);
                }
            }
        }
    }
}

/* 
 * ================================================
 * Describe:      This script is used to enhance the scroll view, copy the UGUI source code also refer to Wenruo code. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-01-28 11:23:34
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-01-28 11:23:34
 * ScriptVersion: 0.1
 * ===============================================
*/
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    public class ScrollRectPro : UIBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler, ICanvasElement, ILayoutGroup
    {
        /// <summary>
        /// A setting for which behavior to use when content moves beyond the confines of its container.
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
        /// Set the scrolling be enabled with direction
        /// </summary>
        public enum Direction
        {
            Horizontal,
            Vertical
        }

        /// <summary>
        /// Scrolling be enabled with direction
        /// </summary>
        public Direction direction = Direction.Vertical;

        /// <summary>
        /// The behavior to use when the content moves beyond the scroll rect.
        /// </summary>
        public MovementType movementType = MovementType.Elastic;

        /// <summary>
        /// The content that can be scrolled. It should be a child of the GameObject with ScrollRectPro on it.
        /// </summary>
        public RectTransform content;

        /// <summary>
        /// The elemental of content that can be scrolled.
        /// </summary>
        public GameObject Elemental;

        /// <summary>
        /// The scroll view can auto integer docking
        /// </summary>
        public bool AutoDocking = true;

        /// <summary>
        /// When the scroll view moves slow than this value able to dock.
        /// </summary>
        public float DockVelocity = 20f;

        /// <summary>
        /// Should movement inertia be enabled?
        /// </summary>
        /// <remarks>
        /// Inertia means that the scrollrect content will keep scrolling for a while after being dragged. It gradually slows down according to the decelerationRate.
        /// </remarks>
        public bool Inertia = true;

        /// <summary>
        /// Current scroll view pro element max count.
        /// </summary>
        public int ElementMaxCount => m_MaxCount;

        /// <summary>
        /// When the direction is vertical, the value is several columns.Conversely the direction is horizontal, the value is several rows.
        /// </summary>
        public int Lines = 1;

        /// <summary>
        /// The amount of elasticity to use when the content moves beyond the scroll rect.
        /// </summary>
        public float Elasticity = 0.1f;

        /// <summary>
        /// The rate at which movement slows down.
        /// *****Only used when inertia is enabled*****
        /// </summary>
        /// <remarks>
        /// The deceleration rate is the speed reduction per second. A value of 0.5 halves the speed each second. The default is 0.135. The deceleration rate is only used when inertia is enabled.
        /// </remarks>
        public float DecelerationRate = 0.135f;

        /// <summary>
        /// The sensitivity to scroll wheel and track pad scroll events.
        /// </summary>
        /// <remarks>
        /// Higher values indicate higher sensitivity.
        /// </remarks>
        public float ScrollSensitivity = 1.0f;

        /// <summary>
        /// Horizontal and vertical spacing
        /// </summary>
        public Vector2Int Spacing = Vector2Int.zero;

        /// <summary>
        /// The current velocity of the content.
        /// </summary>
        /// <remarks>
        /// The velocity is defined in units per second.
        /// </remarks>
        public Vector2 velocity { get { return m_Velocity; } set { m_Velocity = value; } }


        private int m_MaxCount = -1;
        private int m_MinIndex = -1;
        private int m_MaxIndex = -1;
        private int m_CurrentIndex = -1;

        private float m_ElementWidth;
        private float m_ElementHeight;
        private float m_ContentWidth;
        private float m_ContentHeight;

        private bool m_Dragging;
        private bool m_Scrolling;
        private bool m_Inited = false;
        private bool m_ClearList = false;
        [NonSerialized] private bool m_HasRebuiltLayout = false;

        private Vector2 m_Velocity;
        private Vector2 m_PrevPosition = Vector2.zero;
        private Vector2 m_ContentStartPosition = Vector2.zero;
        private Vector2 m_PointerStartLocalCursor = Vector2.zero;// The offset from handle position to mouse down position

        private readonly Vector3[] m_Corners = new Vector3[4];

        private Bounds m_ViewBounds;
        private Bounds m_ContentBounds;
        private Bounds m_PrevViewBounds;
        private Bounds m_PrevContentBounds;

        private Action<GameObject, int> CallbackFunc;

        private RectTransform m_Rect;
        private RectTransform rectTransform
        {
            get
            {
                if (m_Rect == null)
                    m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }
        private RectTransform ViewRect => (RectTransform)transform;

        private DrivenRectTransformTracker m_Tracker;

        /// <summary>
        /// Record the coordinates and game object of the object 
        /// </summary>
        private struct ElementInfo
        {
            public Vector3 pos;
            public GameObject obj;
        };

        /// <summary>
        /// The scroll view pro elements information.
        /// </summary>
        private ElementInfo[] m_ElementInfos;

        /// <summary>
        /// The scroll view pro elements pool.
        /// </summary>
        private Stack<GameObject> m_ElementsPool;

        protected ScrollRectPro()
        { }

        public virtual void Rebuild(CanvasUpdate executing)
        {
            if (executing == CanvasUpdate.PostLayout)
            {
                UpdateBounds();
                UpdatePrevData();

                m_HasRebuiltLayout = true;
            }
        }

        public virtual void LayoutComplete()
        { }

        public virtual void GraphicUpdateComplete()
        { }

        protected override void OnEnable()
        {
            base.OnEnable();

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            SetDirty();
        }

        protected override void OnDisable()
        {
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);
            m_Dragging = false;
            m_Scrolling = false;
            m_HasRebuiltLayout = false;
            m_Tracker.Clear();
            m_Velocity = Vector2.zero;
            CallbackFunc = null;
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            base.OnDisable();
        }
        
        protected override void OnDestroy()
        {
            CallbackFunc = null;
            m_Velocity = Vector2.zero;
            base.OnDestroy();
        }

        /// <summary>
        /// See member in base class.
        /// </summary>
        public override bool IsActive()
        {
            return base.IsActive() && content != null;
        }

        private void EnsureLayoutHasRebuilt()
        {
            if (!m_HasRebuiltLayout && !CanvasUpdateRegistry.IsRebuildingLayout())
                Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// Sets the velocity to zero on both axes so the content stops moving.
        /// </summary>
        public virtual void StopMovement()
        {
            m_Velocity = Vector2.zero;
        }

        public virtual void OnScroll(PointerEventData data)
        {
            if (!IsActive())
                return;

            EnsureLayoutHasRebuilt();
            UpdateBounds();

            Vector2 delta = data.scrollDelta;
            // Down is positive for scroll events, while in UI system up is positive.
            delta.y *= -1;
            if (direction == Direction.Vertical)
            {
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    delta.y = delta.x;
                delta.x = 0;
            }
            if (direction == Direction.Horizontal)
            {
                if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                    delta.x = delta.y;
                delta.y = 0;
            }

            if (data.IsScrolling())
                m_Scrolling = true;

            Vector2 position = content.anchoredPosition;
            position += delta * ScrollSensitivity;
            if (movementType == MovementType.Clamped)
                position += CalculateOffset(position - content.anchoredPosition);

            SetContentAnchoredPosition(position);
            UpdateBounds();
        }

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_Velocity = Vector2.zero;
        }

        /// <summary>
        /// Handling for when the content is beging being dragged.
        /// </summary>
        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            UpdateBounds();

            m_PointerStartLocalCursor = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(ViewRect, eventData.position, eventData.pressEventCamera, out m_PointerStartLocalCursor);
            m_ContentStartPosition = content.anchoredPosition;
            m_Dragging = true;
        }

        /// <summary>
        /// Handling for when the content has finished being dragged.
        /// </summary>
        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_Dragging = false;
        }

        /// <summary>
        /// Handling for when the content is dragged.
        /// </summary>
        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!m_Dragging)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(ViewRect, eventData.position, eventData.pressEventCamera, out localCursor))
                return;

            UpdateBounds();

            var pointerDelta = localCursor - m_PointerStartLocalCursor;
            Vector2 position = m_ContentStartPosition + pointerDelta;

            // Offset to get content into place in the view.
            Vector2 offset = CalculateOffset(position - content.anchoredPosition);
            position += offset;
            if (movementType == MovementType.Elastic)
            {
                if (offset.x != 0)
                    position.x = position.x - RubberDelta(offset.x, m_ViewBounds.size.x);
                if (offset.y != 0)
                    position.y = position.y - RubberDelta(offset.y, m_ViewBounds.size.y);
            }

            SetContentAnchoredPosition(position);
        }

        /// <summary>
        /// Sets the anchored position of the content.
        /// </summary>
        protected virtual void SetContentAnchoredPosition(Vector2 position)
        {
            if (direction == Direction.Vertical)
                position.x = content.anchoredPosition.x;
            if (direction == Direction.Horizontal)
                position.y = content.anchoredPosition.y;

            if (position != content.anchoredPosition)
            {
                content.anchoredPosition = position;
                UpdateBounds();
            }
        }

        protected virtual void LateUpdate()
        {
            if (!content)
                return;

            EnsureLayoutHasRebuilt();
            UpdateBounds();
            float deltaTime = Time.unscaledDeltaTime;
            Vector2 offset = CalculateOffset(Vector2.zero);
            if (!m_Dragging && (offset != Vector2.zero || m_Velocity != Vector2.zero))
            {
                int _axis = (int)direction;
                Vector2 position = content.anchoredPosition;
                // Apply spring physics if movement is elastic and content has an offset from the view.
                if (movementType == MovementType.Elastic && offset[_axis] != 0)
                {
                    float speed = m_Velocity[_axis];
                    float smoothTime = Elasticity;
                    if (m_Scrolling)
                        smoothTime *= 3.0f;
                    position[_axis] = Mathf.SmoothDamp(content.anchoredPosition[_axis], content.anchoredPosition[_axis] + offset[_axis], ref speed, smoothTime, Mathf.Infinity, deltaTime);
                    if (Mathf.Abs(speed) < 1)
                        speed = 0;
                    m_Velocity[_axis] = speed;
                }
                // Else move content according to velocity with deceleration applied.
                else if (Inertia)
                {
                    m_Velocity[_axis] *= Mathf.Pow(DecelerationRate, deltaTime);
                    if (Mathf.Abs(m_Velocity[_axis]) < 1)
                        m_Velocity[_axis] = 0;
                    position[_axis] += m_Velocity[_axis] * deltaTime;
                }
                // If we have neither elaticity or friction, there shouldn't be any velocity.
                else
                {
                    m_Velocity[_axis] = 0;
                }

                if (movementType == MovementType.Clamped)
                {
                    offset = CalculateOffset(position - content.anchoredPosition);
                    position += offset;
                }

                SetContentAnchoredPosition(position);
            }

            if (m_Dragging && Inertia)
            {
                Vector3 newVelocity = (content.anchoredPosition - m_PrevPosition) / deltaTime;
                m_Velocity = Vector3.Lerp(m_Velocity, newVelocity, deltaTime * 10);
            }

            if (m_ViewBounds != m_PrevViewBounds || m_ContentBounds != m_PrevContentBounds || content.anchoredPosition != m_PrevPosition)
            {
                UISystemProfilerApi.AddMarker("ScrollRectPro.value", this);
                UpdateCheck(normalizedPosition);
                UpdatePrevData();
            }

            if (!m_Dragging && AutoDocking)
            {
                float _va = m_Velocity[(int)direction];
                if (Mathf.Abs(_va) <= DockVelocity && Mathf.Abs(_va) != 0)
                {
                    StopMovement();
                    if (_va < 0)
                    {
                        if (direction == Direction.Horizontal)
                            GoToElementPosWithIndex(m_CurrentIndex - m_MaxIndex);
                        else
                            GoToElementPosWithIndex(m_CurrentIndex + 1);
                    }
                    else
                    {
                        if (direction == Direction.Horizontal)
                            GoToElementPosWithIndex(m_CurrentIndex + 1);
                        else 
                            GoToElementPosWithIndex(m_CurrentIndex - m_MaxIndex);
                    }
                }
            }
            m_Scrolling = false;
        }

        /// <summary>
        /// Helper function to update the previous data fields on a ScrollRectPro. Call this before you change data in the ScrollRectPro.
        /// </summary>
        protected void UpdatePrevData()
        {
            if (content == null)
                m_PrevPosition = Vector2.zero;
            else
                m_PrevPosition = content.anchoredPosition;
            m_PrevViewBounds = m_ViewBounds;
            m_PrevContentBounds = m_ContentBounds;
        }

        /// <summary>
        /// The scroll position as a Vector2 between (0,0) and (1,1) with (0,0) being the lower left corner.
        /// </summary>
        public Vector2 normalizedPosition
        {
            get
            {
                return new Vector2(horizontalNormalizedPosition, verticalNormalizedPosition);
            }
            set
            {
                SetNormalizedPosition(value.x, 0);
                SetNormalizedPosition(value.y, 1);
            }
        }

        /// <summary>
        /// The horizontal scroll position as a value between 0 and 1, with 0 being at the left.
        /// </summary>
        public float horizontalNormalizedPosition
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
        /// </summary>
        public float verticalNormalizedPosition
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

        /// <summary>
        /// >Set the horizontal or vertical scroll position as a value between 0 and 1, with 0 being at the left or at the bottom.
        /// </summary>
        /// <param name="value">The position to set, between 0 and 1.</param>
        /// <param name="axis">The axis to set: 0 for horizontal, 1 for vertical.</param>
        protected virtual void SetNormalizedPosition(float value, int axis)
        {
            EnsureLayoutHasRebuilt();
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

        private static float RubberDelta(float overStretching, float viewSize)
        {
            return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual void CalculateLayoutInputHorizontal() { }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual void CalculateLayoutInputVertical() { }

        #region Called by the layout system
        public virtual void SetLayoutHorizontal()
        {
            m_Tracker.Clear();

        }

        public virtual void SetLayoutVertical()
        {
            m_ViewBounds = new Bounds(ViewRect.rect.center, ViewRect.rect.size);
            m_ContentBounds = GetBounds();
        }
        #endregion

        /// <summary>
        /// Calculate the bounds the ScrollRectPro should be using.
        /// </summary>
        protected void UpdateBounds()
        {
            m_ViewBounds = new Bounds(ViewRect.rect.center, ViewRect.rect.size);
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
                    if (direction == Direction.Vertical)
                        contentPos.x = content.anchoredPosition.x;
                    if (direction == Direction.Horizontal)
                        contentPos.y = content.anchoredPosition.y;
                    AdjustBounds(ref m_ViewBounds, ref contentPivot, ref contentSize, ref contentPos);
                }
            }
        }

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

        private Bounds GetBounds()
        {
            if (content == null)
                return new Bounds();
            content.GetWorldCorners(m_Corners);
            var viewWorldToLocalMatrix = ViewRect.worldToLocalMatrix;
            return InternalGetBounds(m_Corners, ref viewWorldToLocalMatrix);
        }

        Bounds InternalGetBounds(Vector3[] corners, ref Matrix4x4 viewWorldToLocalMatrix)
        {
            var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int j = 0; j < 4; j++)
            {
                Vector3 v = viewWorldToLocalMatrix.MultiplyPoint3x4(corners[j]);
                vMin = Vector3.Min(v, vMin);
                vMax = Vector3.Max(v, vMax);
            }

            var bounds = new Bounds(vMin, Vector3.zero);
            bounds.Encapsulate(vMax);
            return bounds;
        }

        private Vector2 CalculateOffset(Vector2 delta)
        {
            Vector2 offset = Vector2.zero;
            if (movementType == MovementType.Unrestricted)
                return offset;

            Vector2 min = m_ContentBounds.min;
            Vector2 max = m_ContentBounds.max;

            if (direction == Direction.Horizontal)
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
            if (direction == Direction.Vertical)
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
        /// Override to alter or add to the code that keeps the appearance of the scroll rect synced with its data.
        /// </summary>
        protected void SetDirty()
        {
            if (!IsActive())
                return;

            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        /// <summary>
        /// Override to alter or add to the code that caches data to avoid repeated heavy operations.
        /// </summary>
        protected void SetDirtyCaching()
        {
            if (!IsActive())
                return;

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        protected GameObject GetPoolsObj()
        {
            GameObject cell = null;
            if (m_ElementsPool.Count > 0) cell = m_ElementsPool.Pop();
            if (cell == null) cell = Instantiate(this.Elemental) as GameObject;

            cell.transform.SetParent(content.transform);
            cell.transform.localScale = Vector3.one;
            SetActive(cell, true);

            return cell;
        }

        /// <summary>
        /// Set the element push the pool.
        /// </summary>
        protected void SetPoolsObj(GameObject element)
        {
            if (element != null)
            {
                m_ElementsPool.Push(element);
                SetActive(element, false);
            }
        }

        /// <summary>
        /// Set active with the element.
        /// </summary>
        protected void SetActive(GameObject obj, bool isActive)
        {
            if (obj != null)
            {
                obj.SetActive(isActive);
            }
        }

        /// <summary>
        /// Check the anchor is right.
        /// </summary>
        private void CheckAnchor(RectTransform rt)
        {
            if (direction == Direction.Vertical)
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
        /// </summary>
        private void UpdateCheck(Vector2 v2)
        {
            if (m_ElementInfos == null) return;

            int _count = m_ElementInfos.Length;
            for (int i = 0, length = _count; i < length; i++)
            {
                ElementInfo _element = m_ElementInfos[i];
                GameObject obj = _element.obj;
                Vector3 pos = _element.pos;
                float rangePos = direction == Direction.Vertical ? pos.y : pos.x;

                if (IsOutRange(rangePos))
                {
                    if (obj != null)
                    {
                        SetPoolsObj(obj);
                        m_ElementInfos[i].obj = null;
                    }
                }
                else
                {
                    if (obj == null)
                    {
                        GameObject cell = GetPoolsObj();
                        cell.transform.localPosition = pos;
                        //cell.name = i.ToString();
                        m_ElementInfos[i].obj = cell;
                        CallbackFunction(cell, i);
                    }
                }
            }
        }

        /// <summary>
        /// Check whether it is out of the display range
        /// </summary>
        /// <param name="pos">The element position</param>
        protected bool IsOutRange(float pos)
        {
            Vector3 listP = content.anchoredPosition;
            if (direction == Direction.Vertical)
            {
                if (pos + listP.y > m_ElementHeight || pos + listP.y < -rectTransform.rect.height)
                {
                    return true;
                }
            }
            else
            {
                if (pos + listP.x < -m_ElementWidth || pos + listP.x > rectTransform.rect.width)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Show the list with new count.
        /// </summary>
        void ShowList(int num)
        {
            m_MinIndex = -1;
            m_MaxIndex = -1;

            if (direction == Direction.Vertical)
            {
                float contentSize = (Spacing.y + m_ElementHeight) * Mathf.CeilToInt((float)num / Lines);
                m_ContentHeight = contentSize;
                m_ContentWidth = content.sizeDelta.x;
                contentSize = contentSize < rectTransform.rect.height ? rectTransform.rect.height : contentSize;
                content.sizeDelta = new Vector2(m_ContentWidth, contentSize);
                if (num != m_MaxCount)
                {
                    content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0);
                }
            }
            else
            {
                float contentSize = (Spacing.x + m_ElementWidth) * Mathf.CeilToInt((float)num / Lines);
                m_ContentWidth = contentSize;
                m_ContentHeight = content.sizeDelta.x;
                contentSize = contentSize < rectTransform.rect.width ? rectTransform.rect.width : contentSize;
                content.sizeDelta = new Vector2(contentSize, m_ContentHeight);
                if (num != m_MaxCount)
                {
                    content.anchoredPosition = new Vector2(0, content.anchoredPosition.y);
                }
            }

            int lastEndIndex = 0;

            if (m_Inited)
            {
                lastEndIndex = num - m_MaxCount > 0 ? m_MaxCount : num;
                lastEndIndex = m_ClearList ? 0 : lastEndIndex;

                int count = m_ClearList ? m_ElementInfos.Length : m_MaxCount;
                for (int i = lastEndIndex; i < count; i++)
                {
                    if (m_ElementInfos[i].obj != null)
                    {
                        SetPoolsObj(m_ElementInfos[i].obj);
                        m_ElementInfos[i].obj = null;
                    }
                }
            }

            ElementInfo[] _tempCellInfos = m_ElementInfos;
            m_ElementInfos = new ElementInfo[num];

            for (int i = 0; i < num; i++)
            {
                if (m_MaxCount != -1 && i < lastEndIndex)
                {
                    ElementInfo _ei = _tempCellInfos[i];

                    float rPos = direction == Direction.Vertical ? _ei.pos.y : _ei.pos.x;
                    if (!IsOutRange(rPos))
                    {
                        m_MinIndex = m_MinIndex == -1 ? i : m_MinIndex;
                        m_MaxIndex = i;

                        if (_ei.obj == null)
                        {
                            _ei.obj = GetPoolsObj();
                        }

                        _ei.obj.transform.GetComponent<RectTransform>().localPosition = _ei.pos;
                        //_ei.obj.name = i.ToString();
                        _ei.obj.SetActive(true);

                        CallbackFunction(_ei.obj, i);
                    }
                    else
                    {
                        SetPoolsObj(_ei.obj);
                        _ei.obj = null;
                    }

                    m_ElementInfos[i] = _ei;
                    continue;
                }

                ElementInfo _element = new ElementInfo();

                if (direction == Direction.Vertical)
                {
                    _element.pos = new Vector3(m_ElementWidth * (i % Lines) + Spacing.x * (i % Lines), -(m_ElementHeight * Mathf.FloorToInt(i / Lines) + Spacing.y * Mathf.FloorToInt(i / Lines)), 0);
                }
                else
                {
                    _element.pos = new Vector3(m_ElementWidth * Mathf.FloorToInt(i / Lines) + Spacing.x * Mathf.FloorToInt(i / Lines), -(m_ElementHeight * (i % Lines) + Spacing.y * (i % Lines)), 0);
                }

                float cellPos = direction == Direction.Vertical ? _element.pos.y : _element.pos.x;
                if (IsOutRange(cellPos))
                {
                    _element.obj = null;
                    m_ElementInfos[i] = _element;
                    continue;
                }

                m_MinIndex = m_MinIndex == -1 ? i : m_MinIndex;
                m_MaxIndex = i;

                GameObject cell = GetPoolsObj();
                cell.GetComponent<RectTransform>().localPosition = _element.pos;
                //cell.name = i.ToString();

                _element.obj = cell;
                m_ElementInfos[i] = _element;

                CallbackFunction(cell, i);
            }
            m_MaxCount = num;
            m_Inited = true;
        }

        /// <summary>
        /// The callback event with elements list update.
        /// </summary>
        protected void CallbackFunction(GameObject obj, int index)
        {
            CallbackFunc?.Invoke(obj, index);
            m_CurrentIndex = index;
        }

        /// <summary>
        /// Initialize the scroll view pro.
        /// </summary>
        /// <param name="callback">The update event</param>
        /// <param name="maxCount">The elements list max count.</param>
        public void InIt(Action<GameObject, int> callback, int maxCount)
        {
            CallbackFunc = null;
            CallbackFunc = callback;

            if (m_Inited) return;

            m_ElementsPool = new Stack<GameObject>();
            SetPoolsObj(Elemental);

            RectTransform cellRectTrans = Elemental.GetComponent<RectTransform>();
            cellRectTrans.pivot = new Vector2(0f, 1f);
            CheckAnchor(cellRectTrans);
            cellRectTrans.anchoredPosition = Vector2.zero;

            m_ElementHeight = cellRectTrans.rect.height;
            m_ElementWidth = cellRectTrans.rect.width;
            content.pivot = new Vector2(0f, 1f);
            m_ContentHeight = content.rect.height;
            m_ContentWidth = content.rect.width;

            CheckAnchor(content);
            m_Inited = true;
            ShowList(maxCount);
        }

        /// <summary>
        /// Changed the lines of Scroll view pro.
        /// </summary>
        /// <param name="maxCount">Max count.</param>
        public void ChangedLinesNumber(int maxCount)
        {
            ShowList(maxCount);
        }

        #region Arrive at designated position
        /// <summary>
        /// restore to the original position with index 0
        /// </summary>
        public void GoToStartLine()
        {
            GoToElementPosWithIndex(0);
        }

        /// <summary>
        /// Go to position with element index.通过index定位到某一单元格的坐标位置
        /// </summary>
        /// <param name="index">The element index. 索引ID</param>
        public void GoToElementPosWithIndex(int index)
        {
            if (null == m_ElementInfos || m_ElementInfos.Length == 0) return;

            int theFirstIndex = index - index % Lines;
            var tmpIndex = theFirstIndex + m_MaxIndex;

            int theLastIndex = tmpIndex > m_MaxCount - 1 ? m_MaxCount - 1 : tmpIndex;

            if (theLastIndex == m_MaxCount - 1)
            {
                var shortOfNum = m_MaxCount % Lines == 0 ? 0 : Lines - m_MaxCount % Lines;
                theFirstIndex = theLastIndex - m_MaxIndex + shortOfNum;
            }

            Vector2 newPos = m_ElementInfos[theFirstIndex].pos;
            if (direction == Direction.Vertical)
            {
                var posY = index <= Lines ? -newPos.y : -newPos.y - Spacing.y;
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, posY);
            }
            else
            {
                var posX = index <= Lines ? -newPos.x : -newPos.x + Spacing.x;
                content.anchoredPosition = new Vector2(posX, content.anchoredPosition.y);
            }
        }
        #endregion

        #region Update the elements list.
        /// <summary>
        /// Update the list.
        /// </summary>
        public void UpdateList()
        {
            for (int i = 0, length = m_ElementInfos.Length; i < length; i++)
            {
                ElementInfo _element = m_ElementInfos[i];
                if (_element.obj != null)
                {
                    float rangePos = direction == Direction.Vertical ? _element.pos.y : _element.pos.x;
                    if (!IsOutRange(rangePos))
                    {
                        CallbackFunction(_element.obj, i);
                    }
                }
            }
        }

        /// <summary>
        /// Update the element with index.
        /// </summary>
        /// <param name="index">The element index.</param>
        public void UpdateElement(int index)
        {
            ElementInfo _element = m_ElementInfos[index - 1];
            if (_element.obj != null)
            {
                float rangePos = direction == Direction.Vertical ? _element.pos.y : _element.pos.x;
                if (!IsOutRange(rangePos))
                {
                    CallbackFunction(_element.obj, index - 1);
                }
            }
        }
        #endregion

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            SetDirtyCaching();
        }
#endif
    }
}

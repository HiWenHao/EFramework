/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-07-11 16:05:35
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-07-11 16:05:35
 * ScriptVersion: 0.1
 * ===============================================
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasyFramework.UI
{
    [AddComponentMenu("UI/ScrollbarPro", 36)]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// A standard scrollbar with a variable sized handle that can be dragged between 0 and 1.
    /// </summary>
    /// <remarks>
    /// The slider component is a Selectable that controls a handle which follow the current value and is sized according to the size property.
    /// The anchors of the handle RectTransforms are driven by the Scrollbar. The handle can be a direct child of the GameObject with the Scrollbar, or intermediary RectTransforms can be placed in between for additional control.
    /// When a change to the scrollbar value occurs, a callback is sent to any registered listeners of onValueChanged.
    /// </remarks>
    public class ScrollbarPro : Selectable, IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler, ICanvasElement
    {
        /// <summary>
        /// Setting that indicates one of four directions the scrollbar will travel.
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// Starting position is the Left.
            /// </summary>
            LeftToRight,

            /// <summary>
            /// Starting position is the Right
            /// </summary>
            RightToLeft,

            /// <summary>
            /// Starting position is the Bottom.
            /// </summary>
            BottomToTop,

            /// <summary>
            /// Starting position is the Top.
            /// </summary>
            TopToBottom,
        }

        [Serializable]
        /// <summary>
        /// UnityEvent callback for when a scrollbar is scrolled.
        /// </summary>
        public class ScrollEvent : UnityEvent<float> { }
        public class ScrollDragEvent : UnityEvent<bool> { }

        [SerializeField]
        private RectTransform _handleRect;

        /// <summary>
        /// The RectTransform to use for the handle.
        /// </summary>
        public RectTransform handleRect { get { return _handleRect; } set { if (SetClass(ref _handleRect, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        // Direction of movement.
        [SerializeField]
        private Direction _direction = Direction.LeftToRight;

        /// <summary>
        /// The direction of the scrollbar from minimum to maximum value.
        /// </summary>
        public Direction direction { get { return _direction; } set { if (SetStruct(ref _direction, value)) UpdateVisuals(); } }

        protected ScrollbarPro()
        { }

        [Range(0f, 1f)]
        [SerializeField]
        private float _value;

        /// <summary>
        /// The current value of the scrollbar, between 0 and 1.
        /// </summary>
        public float value
        {
            get
            {
                float val = _value;
                if (_numberOfSteps > 1)
                    val = Mathf.Round(val * (_numberOfSteps - 1)) / (_numberOfSteps - 1);
                return val;
            }
            set
            {
                Set(value);
            }
        }

        /// <summary>
        /// Set the value of the scrollbar without invoking onValueChanged callback.
        /// </summary>
        /// <param name="input">The new value for the scrollbar.</param>
        public virtual void SetValueWithoutNotify(float input)
        {
            Set(input, false);
        }

        [Range(0f, 1f)]
        [SerializeField]
        private float _size = 0.2f;

        /// <summary>
        /// The size of the scrollbar handle where 1 means it fills the entire scrollbar.
        /// </summary>
        public float size { get { return _size; } set { if (SetStruct(ref _size, Mathf.Clamp01(value))) UpdateVisuals(); } }

        [Range(0, 11)]
        [SerializeField]
        private int _numberOfSteps = 0;

        /// <summary>
        /// The number of steps to use for the value. A value of 0 disables use of steps.
        /// </summary>
        public int numberOfSteps { get { return _numberOfSteps; } set { if (SetStruct(ref _numberOfSteps, value)) { Set(_value); UpdateVisuals(); } } }

        [Space(6)]

        [SerializeField]
        private ScrollEvent _onValueChanged = new ScrollEvent();

        [SerializeField]
        private ScrollDragEvent _onScrollDrag = new ScrollDragEvent();

        /// <summary>
        /// Handling for when the scrollbar value is changed.
        /// </summary>
        /// <remarks>
        /// Allow for delegate-based subscriptions for faster events than 'eventReceiver', and allowing for multiple receivers.
        /// </remarks>
        public ScrollEvent onValueChanged { get { return _onValueChanged; } set { _onValueChanged = value; } }
        public ScrollDragEvent onScrollDrag { get { return _onScrollDrag; } set { _onScrollDrag = value; } }

        // Private fields

        private RectTransform _containerRect;

        // The offset from handle position to mouse down position
        private Vector2 _offset = Vector2.zero;

        // Size of each step.
        float stepSize { get { return (_numberOfSteps > 1) ? 1f / (_numberOfSteps - 1) : 0.1f; } }

        // field is never assigned warning
#pragma warning disable 649
        private DrivenRectTransformTracker _tracker;
#pragma warning restore 649
        private Coroutine _pointerDownRepeat;
        private bool _isPointerDownAndNotDragging = false;

        // This "delayed" mechanism is required for case 1037681.
        private bool _delayedUpdateVisuals = false;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            _size = Mathf.Clamp01(_size);

            //This can be invoked before OnEnabled is called. So we shouldn't be accessing other objects, before OnEnable is called.
            if (IsActive())
            {
                UpdateCachedReferences();
                Set(_value, false);
                // Update rects (in next update) since other things might affect them even if value didn't change.
                _delayedUpdateVisuals = true;
            }

            if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this) && !Application.isPlaying)
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }

#endif // if UNITY_EDITOR

        public virtual void Rebuild(CanvasUpdate executing)
        {
#if UNITY_EDITOR
            if (executing == CanvasUpdate.Prelayout)
                onValueChanged.Invoke(value);
#endif
        }

        /// <summary>
        /// See ICanvasElement.LayoutComplete.
        /// </summary>
        public virtual void LayoutComplete()
        { }

        /// <summary>
        /// See ICanvasElement.GraphicUpdateComplete.
        /// </summary>
        public virtual void GraphicUpdateComplete()
        { }

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateCachedReferences();
            Set(_value, false);
            // Update rects since they need to be initialized correctly.
            UpdateVisuals();
        }

        protected override void OnDisable()
        {
            _tracker.Clear();
            base.OnDisable();
        }

        /// <summary>
        /// Update the rect based on the delayed update visuals.
        /// Got around issue of calling sendMessage from onValidate.
        /// </summary>
        protected virtual void Update()
        {
            if (_delayedUpdateVisuals)
            {
                _delayedUpdateVisuals = false;
                UpdateVisuals();
            }
        }

        void UpdateCachedReferences()
        {
            if (_handleRect && _handleRect.parent != null)
                _containerRect = _handleRect.parent.GetComponent<RectTransform>();
            else
                _containerRect = null;
        }

        void Set(float input, bool sendCallback = true)
        {
            float currentValue = _value;

            // bugfix (case 802330) clamp01 input in callee before calling this function, this allows inertia from dragging content to go past extremities without being clamped
            _value = input;

            // If the stepped value doesn't match the last one, it's time to update
            if (currentValue == value)
                return;

            UpdateVisuals();
            if (sendCallback)
            {
                UISystemProfilerApi.AddMarker("Scrollbar.value", this);
                _onValueChanged.Invoke(value);
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            //This can be invoked before OnEnabled is called. So we shouldn't be accessing other objects, before OnEnable is called.
            if (!IsActive())
                return;

            UpdateVisuals();
        }

        enum Axis
        {
            Horizontal = 0,
            Vertical = 1
        }

        Axis axis { get { return (_direction == Direction.LeftToRight || _direction == Direction.RightToLeft) ? Axis.Horizontal : Axis.Vertical; } }
        bool reverseValue { get { return _direction == Direction.RightToLeft || _direction == Direction.TopToBottom; } }

        // Force-update the scroll bar. Useful if you've changed the properties and want it to update visually.
        private void UpdateVisuals()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateCachedReferences();
#endif
            _tracker.Clear();

            if (_containerRect != null)
            {
                _tracker.Add(this, _handleRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;

                float movement = Mathf.Clamp01(value) * (1 - size);
                if (reverseValue)
                {
                    anchorMin[(int)axis] = 1 - movement - size;
                    anchorMax[(int)axis] = 1 - movement;
                }
                else
                {
                    anchorMin[(int)axis] = movement;
                    anchorMax[(int)axis] = movement + size;
                }

                _handleRect.anchorMin = anchorMin;
                _handleRect.anchorMax = anchorMax;
            }
        }

        // Update the scroll bar's position based on the mouse.
        void UpdateDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (_containerRect == null)
                return;

            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_containerRect, eventData.position, eventData.pressEventCamera, out localCursor))
                return;

            Vector2 handleCenterRelativeToContainerCorner = localCursor - _offset - _containerRect.rect.position;
            Vector2 handleCorner = handleCenterRelativeToContainerCorner - (_handleRect.rect.size - _handleRect.sizeDelta) * 0.5f;

            float parentSize = axis == 0 ? _containerRect.rect.width : _containerRect.rect.height;
            float remainingSize = parentSize * (1 - size);
            if (remainingSize <= 0)
                return;

            DoUpdateDrag(handleCorner, remainingSize);
        }

        //this function is testable, it is found using reflection in ScrollbarClamp test
        private void DoUpdateDrag(Vector2 handleCorner, float remainingSize)
        {
            switch (_direction)
            {
                case Direction.LeftToRight:
                    Set(Mathf.Clamp01(handleCorner.x / remainingSize));
                    break;
                case Direction.RightToLeft:
                    Set(Mathf.Clamp01(1f - (handleCorner.x / remainingSize)));
                    break;
                case Direction.BottomToTop:
                    Set(Mathf.Clamp01(handleCorner.y / remainingSize));
                    break;
                case Direction.TopToBottom:
                    Set(Mathf.Clamp01(1f - (handleCorner.y / remainingSize)));
                    break;
            }
        }

        private bool MayDrag(PointerEventData eventData)
        {
            return IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left;
        }

        /// <summary>
        /// Handling for when the scrollbar value is begin being dragged.
        /// </summary>
        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            _isPointerDownAndNotDragging = false;

            if (!MayDrag(eventData))
                return;

            if (_containerRect == null)
                return;

            _offset = Vector2.zero;
            if (RectTransformUtility.RectangleContainsScreenPoint(_handleRect, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera))
            {
                Vector2 localMousePos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_handleRect, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out localMousePos))
                    _offset = localMousePos - _handleRect.rect.center;
            }
        }

        /// <summary>
        /// Handling for when the scrollbar value is dragged.
        /// </summary>
        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            if (_containerRect != null)
                UpdateDrag(eventData);
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;
        }

        /// <summary>
        /// Event triggered when pointer is pressed down on the scrollbar.
        /// </summary>
        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            base.OnPointerDown(eventData);
            _isPointerDownAndNotDragging = true;
            _pointerDownRepeat = StartCoroutine(ClickRepeat(eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera));

            _onScrollDrag?.Invoke(true);
        }

        protected IEnumerator ClickRepeat(PointerEventData eventData)
        {
            return ClickRepeat(eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera);
        }

        /// <summary>
        /// Coroutine function for handling continual press during Scrollbar.OnPointerDown.
        /// </summary>
        protected IEnumerator ClickRepeat(Vector2 screenPosition, Camera camera)
        {
            while (_isPointerDownAndNotDragging)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(_handleRect, screenPosition, camera))
                {
                    Vector2 localMousePos;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_handleRect, screenPosition, camera, out localMousePos))
                    {
                        var axisCoordinate = axis == 0 ? localMousePos.x : localMousePos.y;

                        // modifying value depending on direction, fixes (case 925824)

                        float change = axisCoordinate < 0 ? size : -size;
                        value += reverseValue ? change : -change;
                        value = Mathf.Clamp01(value);
                        // Only keep 4 decimals of precision
                        value = Mathf.Round(value * 10000f) / 10000f;
                    }
                }
                yield return new WaitForEndOfFrame();

            }            

            StopCoroutine(_pointerDownRepeat);
        }

        /// <summary>
        /// Event triggered when pointer is released after pressing on the scrollbar.
        /// </summary>
        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            _isPointerDownAndNotDragging = false;
            onScrollDrag?.Invoke(false);
        }

        /// <summary>
        /// Handling for movement events.
        /// </summary>
        public override void OnMove(AxisEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
            {
                base.OnMove(eventData);
                return;
            }

            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    if (axis == Axis.Horizontal && FindSelectableOnLeft() == null)
                        Set(Mathf.Clamp01(reverseValue ? value + stepSize : value - stepSize));
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Right:
                    if (axis == Axis.Horizontal && FindSelectableOnRight() == null)
                        Set(Mathf.Clamp01(reverseValue ? value - stepSize : value + stepSize));
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Up:
                    if (axis == Axis.Vertical && FindSelectableOnUp() == null)
                        Set(Mathf.Clamp01(reverseValue ? value - stepSize : value + stepSize));
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Down:
                    if (axis == Axis.Vertical && FindSelectableOnDown() == null)
                        Set(Mathf.Clamp01(reverseValue ? value + stepSize : value - stepSize));
                    else
                        base.OnMove(eventData);
                    break;
            }
        }

        /// <summary>
        /// Prevents selection if we we move on the Horizontal axis. See Selectable.FindSelectableOnLeft.
        /// </summary>
        public override Selectable FindSelectableOnLeft()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Horizontal)
                return null;
            return base.FindSelectableOnLeft();
        }

        /// <summary>
        /// Prevents selection if we we move on the Horizontal axis.  See Selectable.FindSelectableOnRight.
        /// </summary>
        public override Selectable FindSelectableOnRight()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Horizontal)
                return null;
            return base.FindSelectableOnRight();
        }

        /// <summary>
        /// Prevents selection if we we move on the Vertical axis. See Selectable.FindSelectableOnUp.
        /// </summary>
        public override Selectable FindSelectableOnUp()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Vertical)
                return null;
            return base.FindSelectableOnUp();
        }

        /// <summary>
        /// Prevents selection if we we move on the Vertical axis. See Selectable.FindSelectableOnDown.
        /// </summary>
        public override Selectable FindSelectableOnDown()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Vertical)
                return null;
            return base.FindSelectableOnDown();
        }

        /// <summary>
        /// See: IInitializePotentialDragHandler.OnInitializePotentialDrag
        /// </summary>
        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        /// <summary>
        /// Set the direction of the scrollbar, optionally setting the layout as well.
        /// </summary>
        /// <param name="direction">The direction of the scrollbar.</param>
        /// <param name="includeRectLayouts">Should the layout be flipped together with the direction?</param>
        public void SetDirection(Direction direction, bool includeRectLayouts)
        {
            Axis oldAxis = axis;
            bool oldReverse = reverseValue;
            this.direction = direction;

            if (!includeRectLayouts)
                return;

            if (axis != oldAxis)
                RectTransformUtility.FlipLayoutAxes(transform as RectTransform, true, true);

            if (reverseValue != oldReverse)
                RectTransformUtility.FlipLayoutOnAxis(transform as RectTransform, (int)axis, true, true);
        }
        bool SetStruct<T>(ref T currentValue, T newValue) where T : struct
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
                return false;

            currentValue = newValue;
            return true;
        }
        bool SetClass<T>(ref T currentValue, T newValue) where T : class
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return false;

            currentValue = newValue;
            return true;
        }
    }
}

/*
 * ================================================
 * Describe:        This script is used to custom UGUI`s button.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-10-26 16:43:48
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-23 15:15:00
 * ScriptVersion:   0.5
 * ===============================================
 */

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasyFramework.Ui
{
    /// <summary>
    /// 增强按钮组件，提供指针、点击、按压、拖拽与导航等完整交互事件。
    /// <para>Enhanced button component providing pointer, click, press, drag and navigation interaction events.</para>
    /// </summary>
    [AddComponentMenu("UI/Button Pro", 101)]
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(CanvasRenderer))]
    public class ButtonPro : Selectable, IPointerClickHandler, IPointerMoveHandler, ISubmitHandler, ICancelHandler
    {
        //  双击所需的连续点击次数
        private const int DoubleClickCount = 2;

        /// <summary>
        /// 构造函数（禁止直接 new，仅由 Unity 序列化/AddComponent 创建）。
        /// <para>Constructor (direct instantiation is forbidden; created only by Unity serialization or AddComponent).</para>
        /// </summary>
        protected ButtonPro()
        {
        }

        #region Fields - 字段

        /// <summary>
        /// 双击最大间隔（秒）
        /// <para>Maximum interval between clicks for double-click detection (in seconds)</para>
        /// </summary>
        [HeaderPro("设置", "Settings", HeaderProType.Title)]
        [HeaderPro("双击间隔（秒）", "Double Click Interval (s)")]
        [Tooltip("双击最大间隔（秒）")]
        public float doubleClickInterval = 0.17f;

        /// <summary>
        /// 长按触发时间（秒）
        /// <para>Time required to trigger a long press (in seconds)</para>
        /// </summary>
        [HeaderPro("长按时长（秒）", "Long Press Duration (s)")] [Tooltip("长按触发时间（秒）")]
        public float longPressDuration = 0.5f;

        /// <summary>
        /// 持续按压触发间隔（秒），0 表示每帧触发
        /// <para>Interval at which continuous-press events are triggered (in seconds); 0 means every frame</para>
        /// </summary>
        [HeaderPro("连按间隔（秒）", "Keep Press Interval (s)")] [Tooltip("持续按压触发间隔（秒），0 表示每帧触发")]
        public float keepPressInterval = 0.1f;

        /// <summary>
        /// 拖拽最小移动距离（像素），小于此值不触发拖拽
        /// <para>Minimum movement distance to trigger a drag (in pixels); smaller movements are ignored</para>
        /// </summary>
        [HeaderPro("拖拽阈值（像素）", "Drag Threshold (px)")] [Tooltip("拖拽最小移动距离（像素），小于此值不触发拖拽")]
        public float dragThresholdPixels = 10f;

        /// <summary>
        /// 指针按下事件
        /// <para>Pointer down event</para>
        /// </summary>
        [HeaderPro("指针事件", "Pointer Events", HeaderProType.Title)] [HeaderPro("指针按下", "Pointer Down")]
        public UnityEvent<PointerEventData> onPointerDown = new UnityEvent<PointerEventData>();

        /// <summary>
        /// 指针抬起事件
        /// <para>Pointer up event</para>
        /// </summary>
        [HeaderPro("指针抬起", "Pointer Up")]
        public UnityEvent<PointerEventData> onPointerUp = new UnityEvent<PointerEventData>();

        /// <summary>
        /// 指针进入事件
        /// <para>Pointer enter event</para>
        /// </summary>
        [HeaderPro("指针进入", "Pointer Enter")]
        public UnityEvent<PointerEventData> onPointerEnter = new UnityEvent<PointerEventData>();

        /// <summary>
        /// 指针离开事件
        /// <para>Pointer exit event</para>
        /// </summary>
        [HeaderPro("指针离开", "Pointer Exit")]
        public UnityEvent<PointerEventData> onPointerExit = new UnityEvent<PointerEventData>();

        /// <summary>
        /// 单击事件
        /// <para>Click event</para>
        /// </summary>
        [HeaderPro("点击事件", "Click Events", HeaderProType.Title)] [HeaderPro("单击", "Click")]
        public UnityEvent onClick = new UnityEvent();

        /// <summary>
        /// 双击事件，需通过本字段直接增删监听者
        /// <para>Double-click event; add/remove listeners via this field directly</para>
        /// </summary>
        [HeaderPro("双击", "Double Click")] public DoubleClickUnityEvent onDoubleClick = new DoubleClickUnityEvent();

        /// <summary>
        /// 长按事件
        /// <para>Long press event</para>
        /// </summary>
        [HeaderPro("长按", "Long Press")] public UnityEvent onLongPress = new UnityEvent();

        /// <summary>
        /// 持续按压事件
        /// <para>Continuous press event</para>
        /// </summary>
        [HeaderPro("持续按压", "Keep Press")] public UnityEvent onKeepPress = new UnityEvent();

        /// <summary>
        /// 拖拽开始事件
        /// <para>Drag start event</para>
        /// </summary>
        [HeaderPro("拖拽事件", "Drag Events", HeaderProType.Title)] [HeaderPro("拖拽开始", "Drag Start")]
        public UnityEvent onDragStart = new UnityEvent();

        /// <summary>
        /// 拖拽方向事件，携带当前主方向
        /// <para>Drag direction event carrying the current dominant direction</para>
        /// </summary>
        [HeaderPro("拖拽方向", "Dragging")] public UnityEvent<MoveDirection> onDragging = new UnityEvent<MoveDirection>();

        /// <summary>
        /// 拖拽结束事件
        /// <para>Drag end event</para>
        /// </summary>
        [HeaderPro("拖拽结束", "Drag End")] public UnityEvent onDragEnd = new UnityEvent();

        /// <summary>
        /// 选中事件
        /// <para>Select event</para>
        /// </summary>
        [HeaderPro("导航事件", "Navigation Events", HeaderProType.Title)] [HeaderPro("选中", "Select")]
        public UnityEvent onSelect = new UnityEvent();

        /// <summary>
        /// 取消选中事件
        /// <para>Deselect event</para>
        /// </summary>
        [HeaderPro("取消选中", "Deselect")] public UnityEvent onDeselect = new UnityEvent();

        /// <summary>
        /// 提交事件
        /// <para>Submit event</para>
        /// </summary>
        [HeaderPro("提交", "Submit")] public UnityEvent onSubmit = new UnityEvent();

        /// <summary>
        /// 导航移动事件，携带移动方向
        /// <para>Navigation move event carrying the move direction</para>
        /// </summary>
        [HeaderPro("导航移动", "Move")] public UnityEvent<MoveDirection> onMove = new UnityEvent<MoveDirection>();

        /// <summary>
        /// 取消事件（ESC / 手柄返回）
        /// <para>Cancel event (ESC / gamepad back)</para>
        /// </summary>
        [HeaderPro("取消", "Cancel")] public UnityEvent onCancel = new UnityEvent();

        /// <summary>
        /// 有效按压开始事件，不含不可交互或被拦截的按压
        /// <para>Effective press start event, excluding non-interactable or intercepted presses</para>
        /// </summary>
        [HeaderPro("按压事件", "Press Events", HeaderProType.Title)] [HeaderPro("按压开始", "Press Start")]
        public UnityEvent onPressStart = new UnityEvent();

        /// <summary>
        /// 有效按压结束事件，与按压开始成对
        /// <para>Effective press end event, paired with the press start</para>
        /// </summary>
        [HeaderPro("按压结束", "Press End")]
        public UnityEvent onPressEnd = new UnityEvent();

        /// <summary>
        /// 长按进度事件，取值 0~1，抵达 1 时紧接着触发长按
        /// <para>Long-press progress event in 0~1; reaching 1 is followed by the long-press event</para>
        /// </summary>
        [HeaderPro("长按进度", "Hold Progress")]
        public UnityEvent<float> onHoldProgress = new UnityEvent<float>();

        /// <summary>
        /// 状态变化事件，携带对外可见的按钮状态
        /// <para>State-changed event carrying the publicly visible button state</para>
        /// </summary>
        [HeaderPro("状态变化", "State Changed")]
        public UnityEvent<ButtonProState> onStateChanged = new UnityEvent<ButtonProState>();

        private Coroutine _longPressCoroutine;      //  用于检测长按
        private Coroutine _keepPressCoroutine;      //  用于循环持续按压
        private Coroutine _clickDelayCoroutine;     //  用于延迟单击等待双击
        private Coroutine _submitCoroutine;         //  用于提交后恢复按下态视觉

        private int _clickCount;                    //  连续点击计数（最多计到 2）
        private bool _isLongPressTriggered;         //  本次按压是否已经触发了长按事件
        private bool _isDragging;                   //  本次按压是否已经进入拖拽状态
        private float _lastClickTime;               //  上次单击发生的时间
        private Vector2 _pointerDownPosition;       //  按下时的屏幕坐标
        private int _activePointerId;               //  当前正在处理的指针 ID（鼠标左键为 -1，不能用作哨兵）
        private bool _hasActivePointer;             //  是否存在正在处理的按压指针
        private MoveDirection _lastDragDirection;   //  上一次已上报的拖拽方向
        private bool _hasPressStarted;              //  本次有效按压是否已上报开始
        private bool _holdProgressDirty;            //  长按进度是否已偏离 0，中断时才需要复位
        private ButtonProState _lastNotifiedState;  //  上一次已上报的按钮状态
        private bool _hasNotifiedState;             //  是否已经上报过状态

        #endregion

        #region Lifecycle - 生命周期

        /// <summary>
        /// 启用时重置按压与点击状态
        /// <para>Resets press and click state on enable</para>
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            ResetState();
            //  base.OnEnable 已触发状态转换上报，此处仅兜底尚未上报的情况
            NotifyStateChanged(currentSelectionState);
        }

        /// <summary>
        /// 禁用时收尾手势并停止本组件管理的协程
        /// <para>Ends active gestures and stops coroutines owned by this component on disable</para>
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            EndDrag();
            EndPress();
            if (_submitCoroutine != null)
            {
                StopCoroutine(_submitCoroutine);
                _submitCoroutine = null;
            }

            //  仅停止本组件管理的协程，避免误停外部在同物体上启动的协程
            StopLongPressCoroutine();
            StopKeepPressCoroutine();
            StopClickDelayCoroutine();
            ResetState();
        }

        #endregion

        #region Private tool function - 私有工具函数

        #region IEnumerator contents - 迭代器相关

        //  按压是否已结束：抬手、禁用、不可交互都会令按压协程退出
        private bool IsPressValid()
        {
            return _hasActivePointer && IsActive() && IsInteractable();
        }

        //  长按检测：拖拽开始或按压失效时中止，期间持续上报进度
        private IEnumerator LongPressDetect()
        {
            float duration = Mathf.Max(0f, longPressDuration);
            NotifyHoldProgress(0f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (!IsPressValid() || _isDragging)
                    yield break;
                elapsed += Time.unscaledDeltaTime;
                NotifyHoldProgress(duration > 0f ? elapsed / duration : 1f);
                yield return null;
            }

            if (!IsPressValid() || _isDragging || _isLongPressTriggered)
                yield break;

            NotifyHoldProgress(1f);
            //  长按正常完成，进度保持 1，抬手时不再复位
            _holdProgressDirty = false;
            _isLongPressTriggered = true;
            onLongPress.Invoke();
        }

        //  持续按压循环：先等待一个间隔再触发，避免快速单击也触发持续按压
        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        private IEnumerator KeepPressLoop()
        {
            if (!IsPressValid() || _isDragging)
                yield break;

            if (keepPressInterval <= 0f)
            {
                while (true)
                {
                    if (!IsPressValid() || _isDragging)
                        yield break;
                    yield return null;
                    onKeepPress.Invoke();
                }
            }

            var wait = new WaitForSecondsRealtime(Mathf.Max(0f, keepPressInterval));
            while (true)
            {
                if (!IsPressValid() || _isDragging)
                    yield break;
                yield return wait;
                onKeepPress.Invoke();
            }
        }

        //  单击等待：等待期间若出现新的点击、拖拽或长按，则取消单击
        private IEnumerator DelayedSingleClick()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, doubleClickInterval));

            //  回调前先解除引用，回调内重新开始点击序列时不会被误停
            _clickDelayCoroutine = null;

            //  后续点击会由 OnPointerDown 停止本协程，此处只需处理手势状态
            if (_isDragging || _isLongPressTriggered)
                yield break;

            ResetClickSequence();
            onClick.Invoke();
        }

        //  提交后按 Button 的行为恢复选中态视觉
        private IEnumerator FinishSubmit()
        {
            float elapsed = 0f;
            float fadeTime = Mathf.Max(0f, colors.fadeDuration);
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _submitCoroutine = null;
            DoStateTransition(currentSelectionState, false);
        }

        //  停止单击检测
        private void StopClickDelayCoroutine(bool needStop = true)
        {
            if (_clickDelayCoroutine == null)
                return;

            if (needStop)
                StopCoroutine(_clickDelayCoroutine);
            _clickDelayCoroutine = null;
        }

        //  停止长按检测
        private void StopLongPressCoroutine()
        {
            if (_longPressCoroutine == null)
                return;

            StopCoroutine(_longPressCoroutine);
            _longPressCoroutine = null;
        }

        //  停止持续按压循环
        private void StopKeepPressCoroutine()
        {
            if (_keepPressCoroutine == null)
                return;

            StopCoroutine(_keepPressCoroutine);
            _keepPressCoroutine = null;
        }

        #endregion

        //  重置一次按压相关的全部状态
        private void ResetState()
        {
            StopLongPressCoroutine();
            StopKeepPressCoroutine();
            StopClickDelayCoroutine();

            _isLongPressTriggered = false;
            _isDragging = false;
            _activePointerId = 0;
            _hasActivePointer = false;
            _lastDragDirection = MoveDirection.None;
            _holdProgressDirty = false;
            ResetClickSequence();
        }

        //  重置点击序列（双击判定用）
        private void ResetClickSequence()
        {
            StopClickDelayCoroutine();
            _clickCount = 0;
            _lastClickTime = 0f;
        }

        //  按压开始时上报，保证与按压结束成对
        private void BeginPress()
        {
            if (_hasPressStarted)
                return;

            _hasPressStarted = true;
            onPressStart.Invoke();
        }

        //  按压结束时上报，不可交互或禁用导致的中断也会收尾
        private void EndPress()
        {
            //  仅在进度上报过但长按未完成时复位，避免长按成功后抬手多触发一次 0
            ResetHoldProgress();
            if (!_hasPressStarted)
                return;

            _hasPressStarted = false;
            onPressEnd.Invoke();
        }

        //  上报长按进度，取值限制在 0~1；进度已上报时标记为需要复位
        private void NotifyHoldProgress(float progress)
        {
            float value = Mathf.Clamp01(progress);
            if (value > 0f)
                _holdProgressDirty = true;
            onHoldProgress.Invoke(value);
        }

        //  长按未完成就中断时复位进度到 0；已完成则保持 1，不重复触发
        private void ResetHoldProgress()
        {
            if (!_holdProgressDirty)
                return;

            _holdProgressDirty = false;
            onHoldProgress.Invoke(0f);
        }

        //  把 Selectable 的受保护状态映射为对外可见状态
        private static ButtonProState ConvertState(SelectionState state)
        {
            return state switch
            {
                SelectionState.Highlighted => ButtonProState.Highlighted,
                SelectionState.Pressed => ButtonProState.Pressed,
                SelectionState.Selected => ButtonProState.Selected,
                SelectionState.Disabled => ButtonProState.Disabled,
                _ => ButtonProState.Normal
            };
        }

        //  状态真正变化时才上报，避免 Selectable 重复转换造成刷屏
        private void NotifyStateChanged(SelectionState state)
        {
            ButtonProState mapped = ConvertState(state);
            if (_hasNotifiedState && _lastNotifiedState == mapped)
                return;

            _hasNotifiedState = true;
            _lastNotifiedState = mapped;
            onStateChanged.Invoke(mapped);
        }

        //  结束拖拽手势，保证 onDragEnd 每次手势只触发一次
        private void EndDrag()
        {
            if (!_isDragging)
                return;

            _isDragging = false;
            _lastDragDirection = MoveDirection.None;
            onDragEnd.Invoke();
        }

        //  计算拖拽方向
        private static MoveDirection GetDragDirection(Vector2 delta)
        {
            return Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                ? (delta.x > 0 ? MoveDirection.Right : MoveDirection.Left)
                : (delta.y > 0 ? MoveDirection.Up : MoveDirection.Down);
        }

        //  处理拖拽移动；首次越过阈值或方向改变时上报事件
        private void OnDragTriggered(Vector2 current)
        {
            //  长按已发生时不再上报拖拽，保持两种手势互斥
            if (_isLongPressTriggered)
                return;

            Vector2 delta = current - _pointerDownPosition;
            float threshold = Mathf.Max(0f, dragThresholdPixels);
            if (Mathf.Abs(delta.x) < threshold && Mathf.Abs(delta.y) < threshold)
                return;

            //  拖拽一旦成立即取消长按与持续按压，二者互斥
            if (!_isDragging)
            {
                _isDragging = true;
                StopLongPressCoroutine();
                StopKeepPressCoroutine();
                ResetClickSequence();
                ResetHoldProgress();
                onDragStart.Invoke();
            }

            MoveDirection direction = GetDragDirection(delta);
            if (direction == _lastDragDirection)
                return;

            _lastDragDirection = direction;
            onDragging.Invoke(direction);
        }

        //  处理单击或双击操作
        private void HandleClickOrDoubleClick()
        {
            float currentTime = Time.realtimeSinceStartup;
            float interval = Mathf.Max(0f, doubleClickInterval);
            _clickCount = currentTime - _lastClickTime <= interval && _clickCount < DoubleClickCount
                ? _clickCount + 1
                : 1;
            _lastClickTime = currentTime;

            if (!onDoubleClick.HasListener)
            {
                ResetClickSequence();
                onClick.Invoke();
                return;
            }

            if (_clickCount == DoubleClickCount)
            {
                StopClickDelayCoroutine();
                ResetClickSequence();
                onDoubleClick.Invoke();
            }
            else
            {
                StopClickDelayCoroutine();
                _clickDelayCoroutine = StartCoroutine(DelayedSingleClick());
            }
        }

        #endregion

        #region Interface Implement - 接口实现

        /// <summary>
        /// 状态转换时同步对外状态事件
        /// <para>Notifies the public state event on state transitions</para>
        /// </summary>
        /// <param name="state">目标状态 <para>Target state</para></param>
        /// <param name="instant">是否立即转换 <para>Whether to transition instantly</para></param>
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            //  不可交互会中断按压，此处收尾保证按压起止成对（EndPress 幂等）
            if (state == SelectionState.Disabled)
                EndPress();
            if (isActiveAndEnabled)
                NotifyStateChanged(state);
        }

        /// <summary>
        /// 指针按下时调用
        /// <para>Called when a pointer is pressed down</para>
        /// </summary>
        /// <param name="eventData">指针事件数据 <para>Pointer event data</para></param>
        public override void OnPointerDown(PointerEventData eventData)
        {
            //  先过滤再调用基类：避免第二根手指污染选中态，也保证不可交互时基类不会置位 isPointerDown
            if (eventData.button != PointerEventData.InputButton.Left || _hasActivePointer)
                return;
            if (!IsActive() || !IsInteractable())
                return;

            base.OnPointerDown(eventData);
            onPointerDown.Invoke(eventData);
            //  回调内可能已把按钮置为不可交互，此时不应上报按压开始
            if (!IsActive() || !IsInteractable())
            {
                EndDrag();
                EndPress();
                return;
            }

            StopLongPressCoroutine();
            StopKeepPressCoroutine();
            StopClickDelayCoroutine();

            _activePointerId = eventData.pointerId;
            _hasActivePointer = true;
            _pointerDownPosition = eventData.position;
            _isLongPressTriggered = false;
            _isDragging = false;
            _lastDragDirection = MoveDirection.None;

            BeginPress();
            _longPressCoroutine = StartCoroutine(LongPressDetect());
            _keepPressCoroutine = StartCoroutine(KeepPressLoop());
        }

        /// <summary>
        /// 指针抬起时调用
        /// <para>Called when a pointer is released</para>
        /// </summary>
        /// <param name="eventData">指针事件数据 <para>Pointer event data</para></param>
        public override void OnPointerUp(PointerEventData eventData)
        {
            //  先过滤再调用基类，避免非当前指针清掉 Selectable 的按下态
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            if (!_hasActivePointer || eventData.pointerId != _activePointerId)
                return;

            base.OnPointerUp(eventData);
            onPointerUp.Invoke(eventData);
            EndPress();

            //  必须先停止协程，否则按钮在按压中变为不可交互时会遗留无限循环
            StopLongPressCoroutine();
            StopKeepPressCoroutine();
            _hasActivePointer = false;

            //  抬手位置兜底裁决：覆盖指针移出按钮后收不到移动事件的情况
            OnDragTriggered(eventData.position);
            //  EndDrag 会清空 _isDragging，先留存本次手势是否发生过拖拽
            bool wasDragging = _isDragging;
            EndDrag();

            if (wasDragging || !IsActive() || !IsInteractable() || _isLongPressTriggered)
            {
                ResetClickSequence();
                return;
            }

            HandleClickOrDoubleClick();
        }

        /// <summary>
        /// 指针移动时调用，用于拖拽方向检测（<see cref="IPointerMoveHandler"/> 接口实现）。
        /// 仅监听悬停对象，不抢占父级 ScrollRect 的拖拽事件。
        /// <para>Called when a pointer moves for drag detection (implements <see cref="IPointerMoveHandler"/>).
        /// Observes hovered objects only and never steals drag events from a parent ScrollRect.</para>
        /// </summary>
        /// <param name="eventData">指针事件数据 <para>Pointer event data</para></param>
        public void OnPointerMove(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            if (!_hasActivePointer || eventData.pointerId != _activePointerId)
                return;
            if (!IsActive() || !IsInteractable())
                return;

            OnDragTriggered(eventData.position);
        }

        /// <summary>
        /// 指针进入时调用
        /// <para>Called when a pointer enters</para>
        /// </summary>
        /// <param name="eventData">指针事件数据 <para>Pointer event data</para></param>
        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            if (IsActive() && IsInteractable())
                onPointerEnter.Invoke(eventData);
        }

        /// <summary>
        /// 指针离开时调用，不可交互时也会触发以便外部清理悬停态
        /// <para>Called when a pointer exits; also raised while non-interactable so external hover state can be cleared</para>
        /// </summary>
        /// <param name="eventData">指针事件数据 <para>Pointer event data</para></param>
        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            onPointerExit.Invoke(eventData);
        }

        /// <summary>
        /// 选中时调用
        /// <para>Called when selected</para>
        /// </summary>
        /// <param name="eventData">事件数据 <para>Event data</para></param>
        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            onSelect.Invoke();
        }

        /// <summary>
        /// 取消选中时调用
        /// <para>Called when deselected</para>
        /// </summary>
        /// <param name="eventData">事件数据 <para>Event data</para></param>
        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            onDeselect.Invoke();
        }

        /// <summary>
        /// 导航移动时调用（<see cref="Selectable"/> 回调）
        /// <para>Called on navigation move (a <see cref="Selectable"/> callback)</para>
        /// </summary>
        /// <param name="eventData">轴事件数据 <para>Axis event data</para></param>
        public override void OnMove(AxisEventData eventData)
        {
            base.OnMove(eventData);
            onMove.Invoke(eventData.moveDir);
        }

        /// <summary>
        /// 提交时调用（<see cref="ISubmitHandler"/> 接口实现），同时触发单击事件
        /// <para>Called on submit (implements <see cref="ISubmitHandler"/>); also raises the click event</para>
        /// </summary>
        /// <param name="eventData">事件数据 <para>Event data</para></param>
        public void OnSubmit(BaseEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
                return;

            DoStateTransition(SelectionState.Pressed, false);
            onSubmit.Invoke();
            onClick.Invoke();

            if (_submitCoroutine != null)
                StopCoroutine(_submitCoroutine);
            _submitCoroutine = StartCoroutine(FinishSubmit());
        }

        /// <summary>
        /// 指针点击时调用（<see cref="IPointerClickHandler"/> 接口实现）。
        /// 保持空实现以阻断点击事件向父级 <see cref="Selectable"/> 冒泡，实际点击逻辑在 OnPointerUp 中处理。
        /// <para>Called when a pointer clicks (implements <see cref="IPointerClickHandler"/>). Intentionally empty to stop the
        /// click event from bubbling to parent <see cref="Selectable"/>; the actual click logic is handled in OnPointerUp.</para>
        /// </summary>
        /// <param name="eventData">指针事件数据 <para>Pointer event data</para></param>
        public void OnPointerClick(PointerEventData eventData)
        {
        }

        /// <summary>
        /// 取消时调用（<see cref="ICancelHandler"/> 接口实现）
        /// <para>Called on cancel (implements <see cref="ICancelHandler"/>)</para>
        /// </summary>
        /// <param name="eventData">事件数据 <para>Event data</para></param>
        public void OnCancel(BaseEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
                return;

            onCancel.Invoke();
        }

        #endregion
    }
}
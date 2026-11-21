/*
 * ================================================
 * Describe:      This script is used to custom UGUI`s button.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-10-26 16:43:48
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-24 21:30:20
 * ScriptVersion: 0.1
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
    /// 增强按钮组件，提供丰富的交互事件。
    /// <para>Enhanced button component providing a variety of interaction events.</para>
    /// </summary>
    [AddComponentMenu("UI/Button Pro", 101)]
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(CanvasRenderer))]
    public class ButtonPro : Selectable, IPointerClickHandler
    {
        protected ButtonPro() { }
        
        [HeaderPro("事件", "Events", HeaderProType.Title)]
        [HeaderPro("单击", "Click", HeaderProType.Note)]
        [SerializeField] private UnityEvent onClick;

        [HeaderPro("双击", "Double Click", HeaderProType.Note)]
        [SerializeField] private UnityEvent onDoubleClick;

        [HeaderPro("长按", "Long Press", HeaderProType.Note)]
        [SerializeField] private UnityEvent onLongPress;

        [HeaderPro("持续按压", "Keep Press", HeaderProType.Note)]
        [SerializeField] private UnityEvent onKeepPress;

        [HeaderPro("拖拽", "Dragging", HeaderProType.Note)]
        [SerializeField] private UnityEvent<MoveDirection> onDragging;

        /// <summary>
        /// 双击最大间隔（秒）
        /// <para>Maximum interval between clicks for double-click detection (in seconds)</para>
        /// </summary>
        [HeaderPro("设置", "Settings", HeaderProType.Title)]
        [HeaderPro("双击间隔（秒）", "Double Click Interval (s)", HeaderProType.Note)]
        [Tooltip("双击最大间隔（秒）")]
        public float doubleClickInterval = 0.17f;

        /// <summary>
        /// 长按触发时间（秒）
        /// <para>Time required to trigger a long press (in seconds)</para>
        /// </summary>
        [HeaderPro("长按时长（秒）", "Long Press Duration (s)", HeaderProType.Note)]
        [Tooltip("长按触发时间（秒）")]
        public float longPressDuration = 0.5f;

        /// <summary>
        /// 持续按压触发间隔（秒），0 表示每帧触发
        /// <para>Interval at which continuous-press events are triggered (in seconds); 0 means every frame</para>
        /// </summary>
        [HeaderPro("连按间隔（秒）", "Keep Press Interval (s)", HeaderProType.Note)]
        [Tooltip("持续按压触发间隔（秒），0 表示每帧触发")]
        public float keepPressInterval = 0.1f;

        /// <summary>
        /// 拖拽最小移动距离（像素），小于此值不触发拖拽
        /// <para>Minimum movement distance to trigger a drag (in pixels); smaller movements are ignored</para>
        /// </summary>
        [HeaderPro("拖拽阈值（像素）", "Drag Threshold (px)", HeaderProType.Note)]
        [Tooltip("拖拽最小移动距离（像素），小于此值不触发拖拽")]
        public float dragThresholdPixels = 10f;


        private Coroutine _longPressCoroutine;      //  用于检测长按
        private Coroutine _keepPressCoroutine;      //  用于循环持续按压
        private Coroutine _clickDelayCoroutine;     //  用于延迟单击等待双击

        private int _clickCount;                //  点击计数
        private bool _isLongPressTriggered;     //  本次按压是否已经触发了长按事件
        private float _lastClickTime;           //  上次单击发生的时间
        private Vector2 _pointerDownPosition;   //  按下时的屏幕坐标
        
        private int _onDoubleClickListenerCount;    //  双击监听者数量

        #region Event add/remove listener - 事件增加/删除监听
        
        /// <summary>
        /// 添加点击事件监听器
        /// <para>Adds a click event listener</para>
        /// </summary>
        /// <param name="action">点击回调 <para>Click callback</para></param>
        public void AddClickListener(UnityAction action) => onClick.AddListener(action);

        /// <summary>
        /// 移除点击事件监听器
        /// <para>Removes a click event listener</para>
        /// </summary>
        /// <param name="action">点击回调 <para>Click callback</para></param>
        public void RemoveClickListener(UnityAction action) => onClick.RemoveListener(action);

        /// <summary>
        /// 添加双击事件监听器
        /// <para>Adds a double-click event listener</para>
        /// </summary>
        /// <param name="action">双击回调 <para>Double-click callback</para></param>
        public void AddDoubleClickListener(UnityAction action)
        {
            _onDoubleClickListenerCount++;
            onDoubleClick.AddListener(action);
        }

        /// <summary>
        /// 移除双击事件监听器
        /// <para>Removes a double-click event listener</para>
        /// </summary>
        /// <param name="action">双击回调 <para>Double-click callback</para></param>
        public void RemoveDoubleClickListener(UnityAction action)
        {
            _onDoubleClickListenerCount--;
            onDoubleClick.RemoveListener(action);
        }

        /// <summary>
        /// 添加长按事件监听器
        /// <para>Adds a long-press event listener</para>
        /// </summary>
        /// <param name="action">长按回调 <para>Long-press callback</para></param>
        public void AddLongPressListener(UnityAction action) => onLongPress.AddListener(action);

        /// <summary>
        /// 移除长按事件监听器
        /// <para>Removes a long-press event listener</para>
        /// </summary>
        /// <param name="action">长按回调 <para>Long-press callback</para></param>
        public void RemoveLongPressListener(UnityAction action) => onLongPress.RemoveListener(action);

        /// <summary>
        /// 添加持续按压事件监听器
        /// <para>Adds a continuous-press event listener</para>
        /// </summary>
        /// <param name="action">持续按压回调 <para>Continuous-press callback</para></param>
        public void AddKeepPressListener(UnityAction action) => onKeepPress.AddListener(action);

        /// <summary>
        /// 移除持续按压事件监听器
        /// <para>Removes a continuous-press event listener</para>
        /// </summary>
        /// <param name="action">持续按压回调 <para>Continuous-press callback</para></param>
        public void RemoveKeepPressListener(UnityAction action) => onKeepPress.RemoveListener(action);

        /// <summary>
        /// 添加拖拽事件监听器
        /// <para>Adds a drag event listener</para>
        /// </summary>
        /// <param name="action">拖拽回调，携带拖拽方向 <para>Drag callback that receives the drag direction</para></param>
        public void AddDraggingListener(UnityAction<MoveDirection> action) => onDragging.AddListener(action);

        /// <summary>
        /// 移除拖拽事件监听器
        /// <para>Removes a drag event listener</para>
        /// </summary>
        /// <param name="action">拖拽回调，携带拖拽方向 <para>Drag callback that receives the drag direction</para></param>
        public void RemoveDraggingListener(UnityAction<MoveDirection> action) => onDragging.RemoveListener(action);

        /// <summary>
        /// 移除所有点击事件监听器
        /// <para>Removes all click event listeners</para>
        /// </summary>
        public void RemoveAllClickListeners() => onClick.RemoveAllListeners();

        /// <summary>
        /// 移除所有双击事件监听器
        /// <para>Removes all double-click event listeners</para>
        /// </summary>
        public void RemoveAllDoubleClickListeners()
        {
            onDoubleClick.RemoveAllListeners();
            _onDoubleClickListenerCount = 0;
        }

        /// <summary>
        /// 移除所有长按事件监听器
        /// <para>Removes all long-press event listeners</para>
        /// </summary>
        public void RemoveAllLongPressListeners() => onLongPress.RemoveAllListeners();

        /// <summary>
        /// 移除所有持续按压事件监听器
        /// <para>Removes all continuous-press event listeners</para>
        /// </summary>
        public void RemoveAllKeepPressListeners() => onKeepPress.RemoveAllListeners();

        /// <summary>
        /// 移除所有拖拽事件监听器
        /// <para>Removes all drag event listeners</para>
        /// </summary>
        public void RemoveAllDraggingListeners() => onDragging.RemoveAllListeners();
        
        #endregion

        #region Lifecycle - 生命周期

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetState();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            StopAllCoroutines();
            ResetState();
        }
        
        #endregion

        #region Private tool function - 私有工具函数

        #region IEnumerator contents - 迭代器相关
        
        //  长按检测
        private IEnumerator LongPressDetect()
        {
            float elapsed = 0f;
            while (elapsed < longPressDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_isLongPressTriggered) 
                yield break;
            
            _isLongPressTriggered = true;
            onLongPress.Invoke();
        }

        //  持续按压循环
        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        private IEnumerator KeepPressLoop()
        {
            if (keepPressInterval <= 0f)
            {
                while (true)
                {
                    onKeepPress.Invoke();
                    yield return null;
                }
            }
            var wait = new WaitForSecondsRealtime(keepPressInterval);
            while (true)
            {
                onKeepPress.Invoke();
                yield return wait;
            }
        }

        //  单击等待
        private IEnumerator DelayedSingleClick()
        {
            yield return new WaitForSecondsRealtime(doubleClickInterval);
            OnAnyEventTriggered();
            onClick.Invoke();
            StopClickDelayCoroutine(false);
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
        
        //  状态重置
        private void ResetState()
        {
            StopLongPressCoroutine();
            StopKeepPressCoroutine();
            StopClickDelayCoroutine();
            
            _isLongPressTriggered = false;
            _clickCount = 0;
            _lastClickTime = 0f;
        }
        
        //  处理拖拽
        private bool OnDragTriggered(Vector2 current)
        {
            if (_isLongPressTriggered)
                return false;

            Vector2 delta = current - _pointerDownPosition;
            
            float absX = Mathf.Abs(delta.x);
            float absY = Mathf.Abs(delta.y);

            if (absX < dragThresholdPixels && absY < dragThresholdPixels)
                return false;
            
            MoveDirection direction = Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? 
                delta.x > 0 ? MoveDirection.Right : MoveDirection.Left :
                delta.y > 0 ? MoveDirection.Up : MoveDirection.Down;
            
            OnAnyEventTriggered();
            onDragging.Invoke(direction);
            return true;
        }
        
        //  检查是否有双击监听者（运行时 + Inspector 持久化）
        private bool HasDoubleClickListener()
        {
            return _onDoubleClickListenerCount > 0 || onDoubleClick.GetPersistentEventCount() > 0;
        }

        //  处理单击或双击操作
        private void HandleClickOrDoubleClick()
        {
            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - _lastClickTime <= doubleClickInterval)
                _clickCount++;
            else
                _clickCount = 1;

            _lastClickTime = currentTime;

            if (!HasDoubleClickListener())
            {
                OnAnyEventTriggered();
                onClick.Invoke();
                return;
            }

            if (_clickCount == 2)
            {
                StopClickDelayCoroutine();
                OnAnyEventTriggered();
                onDoubleClick.Invoke();
            }
            else
                _clickDelayCoroutine = StartCoroutine(DelayedSingleClick());
        }

        private void OnAnyEventTriggered()
        {
            _clickCount = 0;
            _lastClickTime = 0f;
            StopLongPressCoroutine();
            StopClickDelayCoroutine();
            
            _isLongPressTriggered = false;
        }
        
        #endregion

        #region Interface Implement - 接口实现

        /// <summary>
        /// 指针按下时调用
        /// <para>Called when a pointer is pressed down</para>
        /// </summary>
        /// <param name="eventData">指针事件数据 <para>Pointer event data</para></param>
        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (!IsActive() || !IsInteractable())
                return;
            
            StopLongPressCoroutine();
            StopKeepPressCoroutine();
            StopClickDelayCoroutine();
            
            _pointerDownPosition = eventData.position;
            _isLongPressTriggered = false;

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
            base.OnPointerUp(eventData);
            if (!IsActive() || !IsInteractable())
                return;
            
            StopLongPressCoroutine();
            StopKeepPressCoroutine();

            if (_isLongPressTriggered || OnDragTriggered(eventData.position))
                return;

            HandleClickOrDoubleClick();
        }
        
        /// <summary>
        /// 指针点击时调用（IPointerClickHandler 接口实现）
        /// <para>Called when a pointer clicks (implements IPointerClickHandler)</para>
        /// </summary>
        /// <param name="eventData">指针事件数据 <para>Pointer event data</para></param>
        public void OnPointerClick(PointerEventData eventData)
        {
        }

        #endregion
    }
}

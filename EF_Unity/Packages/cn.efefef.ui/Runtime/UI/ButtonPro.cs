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

namespace EasyFramework
{
    /// <summary>
    /// 增强按钮组件，提供丰富的交互事件。
    /// </summary>
    [AddComponentMenu("UI/Button Pro", 101)]
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(CanvasRenderer))]
    public class ButtonPro : Selectable, IPointerClickHandler
    {
        protected ButtonPro() { }
        
        [Header("Events"), Space] 
        [SerializeField] private UnityEvent onClick;                        //  "单击"
        [SerializeField] private UnityEvent onDoubleClick;                  //  "双击"
        [SerializeField] private UnityEvent onLongPress;                    //  "长按（按住达到时间后触发一次）"
        [SerializeField] private UnityEvent onKeepPress;                    //  "持续按压（按住期间按间隔触发）"
        [SerializeField] private UnityEvent<MoveDirection> onDragging;      //  "朝某一方向拖拽"
        
        [Header("Settings"), Space] 
        [Tooltip("双击最大间隔（秒）")] public float doubleClickInterval = 0.17f;
        [Tooltip("长按触发时间（秒）")] public float longPressDuration = 0.5f;
        [Tooltip("持续按压触发间隔（秒），0 表示每帧触发")] public float keepPressInterval = 0.1f;
        [Tooltip("拖拽最小移动距离（像素），小于此值不触发拖拽")] public float dragThresholdPixels = 10f;


        private Coroutine _longPressCoroutine;      //  用于检测长按
        private Coroutine _keepPressCoroutine;      //  用于循环持续按压
        private Coroutine _clickDelayCoroutine;     //  用于延迟单击等待双击

        private int _clickCount;                //  点击计数
        private bool _isLongPressTriggered;     //  本次按压是否已经触发了长按事件
        private float _lastClickTime;           //  上次单击发生的时间
        private Vector2 _pointerDownPosition;   //  按下时的屏幕坐标
        
        private int _onDoubleClickListenerCount;    //  双击监听者数量

        #region Event add/remove listener - 事件增加/删除监听
        
        public void AddClickListener(UnityAction action) => onClick.AddListener(action);
        public void RemoveClickListener(UnityAction action) => onClick.RemoveListener(action);

        public void AddDoubleClickListener(UnityAction action)
        {
            _onDoubleClickListenerCount++;
            onDoubleClick.AddListener(action);
        }
        public void RemoveDoubleClickListener(UnityAction action)
        {
            _onDoubleClickListenerCount--;
            onDoubleClick.RemoveListener(action);
        }

        public void AddLongPressListener(UnityAction action) => onLongPress.AddListener(action);
        public void RemoveLongPressListener(UnityAction action) => onLongPress.RemoveListener(action);

        public void AddKeepPressListener(UnityAction action) => onKeepPress.AddListener(action);
        public void RemoveKeepPressListener(UnityAction action) => onKeepPress.RemoveListener(action);

        public void AddDraggingListener(UnityAction<MoveDirection> action) => onDragging.AddListener(action);
        public void RemoveDraggingListener(UnityAction<MoveDirection> action) => onDragging.RemoveListener(action);

        public void RemoveAllClickListeners() => onClick.RemoveAllListeners();
        public void RemoveAllDoubleClickListeners()
        {
            onDoubleClick.RemoveAllListeners();
            _onDoubleClickListenerCount = 0;
            _onDoubleClickListenerCount = onDoubleClick.GetPersistentEventCount();
        }
        public void RemoveAllLongPressListeners() => onLongPress.RemoveAllListeners();
        public void RemoveAllKeepPressListeners() => onKeepPress.RemoveAllListeners();
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
        
        //  处理单击或双击操作
        private void HandleClickOrDoubleClick()
        {
            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - _lastClickTime <= doubleClickInterval)
                _clickCount++;
            else
                _clickCount = 1;

            _lastClickTime = currentTime;

            if (_onDoubleClickListenerCount == 0)
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
        
        public void OnPointerClick(PointerEventData eventData)
        {
        }

        #endregion
    }
}

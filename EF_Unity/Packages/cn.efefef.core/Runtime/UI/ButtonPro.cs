/*
 * ================================================
 * Describe:      This script is used to custom UGUI`s button.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-10-26 16:43:48
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-13 15:59:18
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static UnityEngine.EventSystems.PointerEventData;

namespace EasyFramework.UI
{
    [AddComponentMenu("UI/Button Pro", 101)]
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(CanvasRenderer))]
    public class ButtonPro : Selectable
    {
        private ButtonPro() { }

        [FormerlySerializedAs("onClickLeft")]
        [SerializeField]
        private UnityEvent _onClickLeft = new UnityEvent();

        [FormerlySerializedAs("onClickRight")]
        [SerializeField]
        private UnityEvent _oClickRight = new UnityEvent();
        
        [FormerlySerializedAs("onDrag"), Header("Left => False, Right => True")]
        [SerializeField]
        private UnityEvent<bool> _onDrag = new UnityEvent<bool>();

        [FormerlySerializedAs("onLongPressLeft")]
        [SerializeField]
        private UnityEvent _onLongPressLeft = new UnityEvent();

        [FormerlySerializedAs("onDoubleClickLeft")]
        [SerializeField]
        private UnityEvent _onDoubleClickLeft = new UnityEvent();

        [FormerlySerializedAs("onKeepPressLeft")]
        [SerializeField]
        private UnityEvent _onKeepPressLeft = new UnityEvent();

        /// <summary>
        /// 被拖拽, Left => False, Right => True
        /// </summary>
        public UnityEvent<bool> OnDrag => _onDrag;
        public UnityEvent OnClickLeft => _onClickLeft;
        public UnityEvent OnClickRight => _oClickRight;
        public UnityEvent OnDoubleClickLeft => _onDoubleClickLeft;
        public UnityEvent OnLongPressLeft => _onLongPressLeft;
        public UnityEvent OnKeepPressLeft => _onKeepPressLeft;

        /// <summary>
        /// 拖拽时移动的最小间隔(像素)
        /// </summary>
        public float IntervalPixelOfDrag { get; set; } = 20;

        /// <summary>
        /// 长按判定的最少时间(毫秒)
        /// </summary>
        public float IntervalTimeOfLongPress { get; set; } = 600.0f;
        
        /// <summary>
        /// 双击判定间隔的最长时间(毫秒)
        /// </summary>
        public float IntervalTimeOfDoubleClick { get; set; } = 170.0f;

        private float _clickCount;
        private bool _onHoldDown;
        private bool _isKeepPress;
        private bool _onEventTrigger;
        private double _clickIntervalTime;
        private DateTime _clickStartTime;
        private Vector3 _clickStartPos;

        private void OnAnyEventTrigger()
        {
            _clickCount = 0;
            _onEventTrigger = true;
            _clickStartTime = default;
        }

        private void Update()
        {
            if (!interactable) return;
            _clickIntervalTime = (DateTime.Now - _clickStartTime).TotalMilliseconds;

            if (!_onHoldDown && 0 != _clickCount)
            {
                if (_clickIntervalTime >= IntervalTimeOfDoubleClick && _clickIntervalTime < IntervalTimeOfLongPress)
                {
                    if (_clickCount == 2)
                        _onDoubleClickLeft?.Invoke();
                    else
                        OnClickLeft?.Invoke();
                    OnAnyEventTrigger();
                }
            }

            if (_onHoldDown && !_onEventTrigger)
            {
                if (_clickIntervalTime >= IntervalTimeOfLongPress)
                {
                    _onHoldDown = false;
                    _onLongPressLeft?.Invoke();
                    OnAnyEventTrigger();
                }
            }

            if (_isKeepPress) OnKeepPressLeft?.Invoke();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            _clickStartPos = Input.mousePosition;
            if (eventData.button == InputButton.Left)
            {
                _onHoldDown = true;
                _onEventTrigger = false;
                _clickStartTime = DateTime.Now;
            }
            _isKeepPress = true;
            base.OnPointerDown(eventData);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            Vector3 magnitude = Input.mousePosition - _clickStartPos;
            if (magnitude.magnitude >= IntervalPixelOfDrag)
            {
                OnDrag?.Invoke(magnitude.x > 0);
                OnAnyEventTrigger();
                return;
            }
            
            if (eventData.button == InputButton.Right)
            {
                OnClickRight?.Invoke();
                OnAnyEventTrigger();
            }
            else if (eventData.button == InputButton.Left && !_onEventTrigger)
            {
                _clickCount++;
                if (_clickCount % 3 == 0)
                {
                    OnClickLeft?.Invoke();
                    OnAnyEventTrigger();
                    return;
                }

                _onHoldDown = false;
                _isKeepPress = false;
            }
            _isKeepPress = false;

            base.OnPointerUp(eventData);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            if (eventData.button == InputButton.Left)
            {
                _onHoldDown = false;
            }
            _isKeepPress = false;

            base.OnPointerExit(eventData);
        }
    }
}

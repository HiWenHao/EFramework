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

        [FormerlySerializedAs("onLongPressLeft")]
        [SerializeField]
        private UnityEvent _onLongPressLeft = new UnityEvent();

        [FormerlySerializedAs("onDoubleClickLeft")]
        [SerializeField]
        private UnityEvent _onDoubleClickLeft = new UnityEvent();

        [FormerlySerializedAs("onKeepPressLeft")]
        [SerializeField]
        private UnityEvent _onKeepPressLeft = new UnityEvent();

        public UnityEvent OnClickLeft
        {
            get { return _onClickLeft; }
        }
        public UnityEvent OnClickRight
        {
            get { return _oClickRight; }
        }
        public UnityEvent OnDoubleClickLeft
        {
            get { return _onDoubleClickLeft; }
        }
        public UnityEvent OnLongPressLeft
        {
            get { return _onLongPressLeft; }
        }
        public UnityEvent OnKeepPressLeft
        {
            get { return _onKeepPressLeft; }
        }

        private float _longPressIntervalTime = 600.0f;
        private float _doubleClcikIntervalTime = 170.0f;

        private float _clickCount = 0;
        private bool _onHoldDown = false;
        private bool _isKeepPress = false;
        private bool _onEventTrigger = false;
        private double _clickIntervalTime = 0;
        private DateTime _clickStartTime;

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
                if (_clickIntervalTime >= _doubleClcikIntervalTime && _clickIntervalTime < _longPressIntervalTime)
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
                if (_clickIntervalTime >= _longPressIntervalTime)
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
                else
                {
                    _onHoldDown = false;
                    _isKeepPress = false;
                }
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

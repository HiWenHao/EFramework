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
        private UnityEvent m_OnClickLeft = new UnityEvent();

        [FormerlySerializedAs("onClickRight")]
        [SerializeField]
        private UnityEvent m_OnClickRight = new UnityEvent();

        [FormerlySerializedAs("onLongPressLeft")]
        [SerializeField]
        private UnityEvent m_onLongPressLeft = new UnityEvent();

        [FormerlySerializedAs("onDoubleClickLeft")]
        [SerializeField]
        private UnityEvent m_onDoubleClickLeft = new UnityEvent();

        [FormerlySerializedAs("onKeepPressLeft")]
        [SerializeField]
        private UnityEvent m_onKeepPressLeft = new UnityEvent();

        public UnityEvent onClickLeft
        {
            get { return m_OnClickLeft; }
        }
        public UnityEvent onClickRight
        {
            get { return m_OnClickRight; }
        }
        public UnityEvent onDoubleClickLeft
        {
            get { return m_onDoubleClickLeft; }
        }
        public UnityEvent onLongPressLeft
        {
            get { return m_onLongPressLeft; }
        }
        public UnityEvent onKeepPressLeft
        {
            get { return m_onKeepPressLeft; }
        }

        private float m_longPressIntervalTime = 600.0f;
        private float m_doubleClcikIntervalTime = 170.0f;

        private float m_clickCount = 0;
        private bool m_onHoldDown = false;
        private bool m_isKeepPress = false;
        private bool m_onEventTrigger = false;
        private double m_clickIntervalTime = 0;
        private DateTime m_clickStartTime;

        private void OnAnyEventTrigger()
        {
            m_clickCount = 0;
            m_onEventTrigger = true;
            m_clickStartTime = default;
        }

        private void Update()
        {
            if (!interactable) return;
            m_clickIntervalTime = (DateTime.Now - m_clickStartTime).TotalMilliseconds;

            if (!m_onHoldDown && 0 != m_clickCount)
            {
                if (m_clickIntervalTime >= m_doubleClcikIntervalTime && m_clickIntervalTime < m_longPressIntervalTime)
                {
                    if (m_clickCount == 2)
                        m_onDoubleClickLeft?.Invoke();
                    else
                        onClickLeft?.Invoke();
                    OnAnyEventTrigger();
                }
            }

            if (m_onHoldDown && !m_onEventTrigger)
            {
                if (m_clickIntervalTime >= m_longPressIntervalTime)
                {
                    m_onHoldDown = false;
                    m_onLongPressLeft?.Invoke();
                    OnAnyEventTrigger();
                }
            }

            if (m_isKeepPress) onKeepPressLeft?.Invoke();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == InputButton.Left)
            {
                m_onHoldDown = true;
                m_onEventTrigger = false;
                m_clickStartTime = DateTime.Now;
            }
            m_isKeepPress = true;
            base.OnPointerDown(eventData);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == InputButton.Right)
            {
                onClickRight?.Invoke();
                OnAnyEventTrigger();
            }
            else if (eventData.button == InputButton.Left && !m_onEventTrigger)
            {
                m_clickCount++;
                if (m_clickCount % 3 == 0)
                {
                    onClickLeft?.Invoke();
                    OnAnyEventTrigger();
                    return;
                }
                else
                {
                    m_onHoldDown = false;
                    m_isKeepPress = false;
                }
            }
            m_isKeepPress = false;

            base.OnPointerUp(eventData);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            if (eventData.button == InputButton.Left)
            {
                m_onHoldDown = false;
            }
            m_isKeepPress = false;

            base.OnPointerExit(eventData);
        }
    }
}

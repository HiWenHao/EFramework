/* 
 * ================================================
 * Describe:      This script is used to custom UGUI`s button. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-10-26 16:43:48
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-10-26 16:43:48
 * ScriptVersion: 0.1
 * ===============================================
*/

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace EasyFramework.UI
{
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(CanvasRenderer))]
    public class ButtonPro : Selectable, ISubmitHandler
    {
        protected ButtonPro() { }

        [Serializable]
        public class ButtonClickedEvent : UnityEvent { }

        [FormerlySerializedAs("onClick")]
        [SerializeField]
        private ButtonClickedEvent m_OnClick = new ButtonClickedEvent();

        [FormerlySerializedAs("onLongPress")]
        [SerializeField]
        private ButtonClickedEvent m_onLongPress = new ButtonClickedEvent();

        [FormerlySerializedAs("onDoubleClick")]
        [SerializeField]
        private ButtonClickedEvent m_onDoubleClick = new ButtonClickedEvent();

        [FormerlySerializedAs("onKeepPress")]
        [SerializeField]
        private ButtonClickedEvent m_onKeepPress = new ButtonClickedEvent();

        public ButtonClickedEvent onClick
        {
            get { return m_OnClick; }
        }
        public ButtonClickedEvent onDoubleClick
        {
            get { return m_onDoubleClick; }
        }
        public ButtonClickedEvent onLongPress
        {
            get { return m_onLongPress; }
        }
        public ButtonClickedEvent onKeepPress
        {
            get { return m_onKeepPress; }
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

        private void Press()
        {
            if (!IsActive() || !IsInteractable())
                return;

            UISystemProfilerApi.AddMarker("Button.onClick", this);
            m_OnClick.Invoke();
        }

        private void Update()
        {
            if (!this.interactable) return;
            m_clickIntervalTime = (DateTime.Now - m_clickStartTime).TotalMilliseconds;

            if (!m_onHoldDown && 0 != m_clickCount)
            {
                if (m_clickIntervalTime >= m_doubleClcikIntervalTime && m_clickIntervalTime < m_longPressIntervalTime)
                {
                    if (m_clickCount == 2)
                        m_onDoubleClick?.Invoke();
                    else
                        onClick?.Invoke();
                    OnAnyEventTrigger();
                }
            }

            if (m_onHoldDown && !m_onEventTrigger)
            {
                if (m_clickIntervalTime >= m_longPressIntervalTime)
                {
                    m_onHoldDown = false;
                    OnAnyEventTrigger();
                    m_onLongPress?.Invoke();
                }
            }

            if (m_isKeepPress) onKeepPress?.Invoke();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            m_onHoldDown = true;
            m_isKeepPress = true;
            m_onEventTrigger = false;
            m_clickStartTime = DateTime.Now;

            base.OnPointerDown(eventData);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (m_onEventTrigger)
                return;

            m_clickCount++;
            if (m_clickCount % 3 == 0)
            {
                onClick?.Invoke();
                OnAnyEventTrigger();
                return;
            }
            else
            {
                m_onHoldDown = false;
                m_isKeepPress = false;
            }

            base.OnPointerUp(eventData);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            m_onHoldDown = false;
            m_isKeepPress = false;

            base.OnPointerExit(eventData);
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            Press();

            if (!IsActive() || !IsInteractable())
                return;

            DoStateTransition(SelectionState.Pressed, false);
            StartCoroutine(OnFinishSubmit());
        }

        private IEnumerator OnFinishSubmit()
        {
            var fadeTime = colors.fadeDuration;
            var elapsedTime = 0f;

            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            DoStateTransition(currentSelectionState, false);
        }
    }
}

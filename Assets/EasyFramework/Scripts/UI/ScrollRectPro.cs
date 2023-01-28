/* 
 * ================================================
 * Describe:      This script is used to  . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-01-28 11:23:34
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-01-28 11:23:34
 * ScriptVersion: 0.1
 * ===============================================
*/
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XHTools;

namespace EasyFramework.UI
{
    [AddComponentMenu("UI/Scroll Rect Pro", 102)]
    [SelectionBase]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// A super component for making a child RectTransform scroll.
    /// </summary>
    /// <remarks>
    /// ScrollRectPro will not do any clipping on its own. Combined with a Mask component, it can be turned into a scroll view.
    /// </remarks>
    public class ScrollRectPro : UIBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler, ICanvasElement, ILayoutElement, ILayoutGroup
    {
        [SerializeField]
        private RectTransform m_Content;


        private bool m_Dragging;
        private Vector2 m_Velocity;

        #region LayoutElement
        //public float minWidth => throw new NotImplementedException();

        //public float preferredWidth => throw new NotImplementedException();

        //public float flexibleWidth => throw new NotImplementedException();

        //public float minHeight => throw new NotImplementedException();

        //public float preferredHeight => throw new NotImplementedException();

        //public float flexibleHeight => throw new NotImplementedException();

        //public int layoutPriority => throw new NotImplementedException();
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float minWidth { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float preferredWidth { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float flexibleWidth { get { return -1; } }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float minHeight { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float preferredHeight { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float flexibleHeight { get { return -1; } }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual int layoutPriority { get { return -1; } }
        #endregion

        #region Drag
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            D.Log("OnInitializePotentialDrag");
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            m_Velocity = Vector2.zero;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            D.Log("OnBeginDrag");
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;



            m_Dragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!m_Dragging)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            D.Log("OnEndDrag");

            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            m_Dragging = false;
        }
        #endregion

        #region ScrollHandler
        public void OnScroll(PointerEventData eventData)
        {

        }
        #endregion

        #region CanvasElement
        public void Rebuild(CanvasUpdate executing)
        {

        }

        public void LayoutComplete()
        {

        }

        public void GraphicUpdateComplete()
        {

        }

        public void CalculateLayoutInputHorizontal()
        {

        }

        public void CalculateLayoutInputVertical()
        {

        }
        #endregion

        #region LayoutGroup
        public void SetLayoutHorizontal()
        {

        }

        public void SetLayoutVertical()
        {

        }
        #endregion

        #region Active
        public override bool IsActive()
        {
            return base.IsActive() && m_Content != null;
        }
        #endregion
    }
}

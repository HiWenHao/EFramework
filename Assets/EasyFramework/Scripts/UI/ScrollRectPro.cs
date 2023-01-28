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
        #region LayoutElement
        public float minWidth => throw new NotImplementedException();

        public float preferredWidth => throw new NotImplementedException();

        public float flexibleWidth => throw new NotImplementedException();

        public float minHeight => throw new NotImplementedException();

        public float preferredHeight => throw new NotImplementedException();

        public float flexibleHeight => throw new NotImplementedException();

        public int layoutPriority => throw new NotImplementedException();
        #endregion

        #region Drag
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {

        }
        public void OnBeginDrag(PointerEventData eventData)
        {

        }

        public void OnDrag(PointerEventData eventData)
        {

        }

        public void OnEndDrag(PointerEventData eventData)
        {

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
            throw new NotImplementedException();
        }

        public void CalculateLayoutInputVertical()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region LayoutGroup
        public void SetLayoutHorizontal()
        {
            throw new NotImplementedException();
        }

        public void SetLayoutVertical()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}

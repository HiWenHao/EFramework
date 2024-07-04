/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-10 15:11:01
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-07-04 09:28:55
 * ScriptVersion: 0.2
 * ===============================================
*/

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace EasyFramework.UI
{
    /// <summary>
    /// 轮播图、幻灯片
    /// </summary>
    [AddComponentMenu("UI/Slideshow", 103)]
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(UnityEngine.UI.Mask))]
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public class Slideshow : UIBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField]
        private AxisType m_MoveAxis;

        [SerializeField]
        private LoopDirectionType m_LoopDirection = LoopDirectionType.LeftOrDown;

        [SerializeField]
        private bool m_CanDrag = false;

        [SerializeField]
        private bool m_AutoLoop = true;

        [SerializeField]
        private int m_SpacingTime = 150;

        [SerializeField]
        private float m_LoopSpaceTime = 1;

        [SerializeField]
        private Vector2 m_Spacing = new Vector2(10, 10);

        [SerializeField]
        private Vector2 m_ElementSize = new Vector2(100, 100);
        public UnityEvent<int> OnIndexChanged { get; set; }

        /// <summary>
        /// 当前处于正中的元素
        /// </summary>
        public int CurrentIndex => m_index;

        /// <summary>
        /// 元素总数
        /// </summary>
        public int ElementCount => m_elementCount;

        private int m_index = 0;
        private int m_preIndex = 0;
        private int m_currentStep = 0;
        private int m_elementCount = 0;
        private float currTimeDelta = 0;
        private bool m_Dragging = false;
        private bool m_IsNormalizing = false;
        private bool contentCheckCache = true;

        private Vector2 m_PrePos;
        private Vector2 m_CurrentPos;
        private RectTransform header;
        private RectTransform viewRectTran;

        protected override void Awake()
        {
            m_elementCount = transform.childCount;
            viewRectTran = GetComponent<RectTransform>();
            header = GetChild(0);
        }

        protected override void OnEnable()
        {
            ResizeChildren();
            if (!ContentIsLongerThanSelf())
                return;
            
            int s;
            do
            {
                s = GetBoundaryState();
                LoopElement(s);
            } while (s != 0);
            
        }

        protected virtual void Update()
        {
            if (!ContentIsLongerThanSelf())
                return;

            int s = GetBoundaryState();
            LoopElement(s);
            //缓动回指定位置
            if (m_IsNormalizing && !m_Dragging)
            {
                if (m_currentStep == m_SpacingTime)
                {
                    m_IsNormalizing = false;
                    m_currentStep = 0;
                    m_CurrentPos = Vector2.zero;
                    return;
                }
                Vector2 delta = m_CurrentPos / m_SpacingTime;
                m_currentStep++;

                foreach (RectTransform i in viewRectTran)
                {
                    i.localPosition -= (Vector3)delta;
                }
            }
            //自动loop
            if (m_AutoLoop && !m_IsNormalizing && !m_Dragging)
            {
                currTimeDelta += Time.deltaTime;
                if (currTimeDelta > m_LoopSpaceTime)
                {
                    currTimeDelta = 0;
                    MoveToIndex(m_index + (int)m_LoopDirection);
                }
            }
            //检测index是否变化
            if (m_MoveAxis == AxisType.Horizontal)
            {
                m_index = (int)(header.localPosition.x / (m_ElementSize.x + m_Spacing.x - 1));
            }
            else
            {
                m_index = (int)(header.localPosition.y / (m_ElementSize.y + m_Spacing.y - 1));
            }
            if (m_index <= 0)
            {
                m_index = Mathf.Abs(m_index);
            }
            else
            {
                m_index = ElementCount - m_index;
            }
            if (m_index != m_preIndex)
            {
                OnIndexChanged?.Invoke(m_index);
            }
            m_preIndex = m_index;

        }

        #region Drag Handler
        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (!m_CanDrag || !contentCheckCache)
            {
                return;
            }
            if (((eventData.button == PointerEventData.InputButton.Left) && IsActive()) && RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRectTran, eventData.position, eventData.pressEventCamera, out Vector2 vector))
            {
                m_Dragging = true;
                m_PrePos = vector;
            }
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (!m_CanDrag || !contentCheckCache)
            {
                return;
            }
            if (((eventData.button == PointerEventData.InputButton.Left) && IsActive()) && RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRectTran, eventData.position, eventData.pressEventCamera, out Vector2 vector))
            {
                m_IsNormalizing = false;
                m_CurrentPos = Vector2.zero;
                m_currentStep = 0;

                Vector2 vector2 = vector - m_PrePos;
                if (m_MoveAxis == AxisType.Horizontal)
                    vector2.y = 0;
                else
                    vector2.x = 0;

                foreach (RectTransform i in viewRectTran)
                {
                    i.localPosition += (Vector3)vector2;
                }

                m_PrePos = vector;
            }
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (!m_CanDrag || !contentCheckCache)
            {
                return;
            }
            m_Dragging = false;
            m_IsNormalizing = true;
            m_CurrentPos = CalcCorrectDeltaPos();
            m_currentStep = 0;
        }
        #endregion

        #region Private Function
        /// <summary>
        /// Resize the elements
        /// <para>调整元素尺寸</para>
        /// </summary>
        private void ResizeChildren()
        {
            Vector2 delta = m_MoveAxis == AxisType.Horizontal ? new Vector2(m_ElementSize.x + m_Spacing.x, 0) : new Vector2(0, m_ElementSize.y + m_Spacing.y);

            for (int i = 0; i < ElementCount; i++)
            {
                var t = GetChild(i);
                if (t)
                {
                    t.localPosition = delta * i;
                    t.sizeDelta = m_ElementSize;
                }
            }
            m_IsNormalizing = false;
            m_CurrentPos = Vector2.zero;
            m_currentStep = 0;
        }

        /// <summary>
        /// Whether the content is larger than itself
        /// <para>内容是否比自身范围大</para>
        /// </summary>
        private bool ContentIsLongerThanSelf()
        {
            float _contentLen;
            float _rectLen;
            if (m_MoveAxis == AxisType.Horizontal)
            {
                _contentLen = ElementCount * (m_ElementSize.x + m_Spacing.x) - m_Spacing.x;
                _rectLen = viewRectTran.rect.xMax - viewRectTran.rect.xMin;
            }
            else
            {
                _contentLen = ElementCount * (m_ElementSize.y + m_Spacing.y) - m_Spacing.y;
                _rectLen = viewRectTran.rect.yMax - viewRectTran.rect.yMin;
            }
            contentCheckCache = _contentLen > _rectLen;
            return contentCheckCache;
        }

        /// <summary>
        /// Boundary cases are detected and divided into 0 not touching the boundary, -1 left (bottom) touching the boundary, and 1 right (top) touching the boundary.
        /// <para>检测边界情况，分为0未触界，-1左(下)触界，1右(上)触界</para>
        /// </summary>
        private int GetBoundaryState()
        {
            RectTransform _left;
            RectTransform _right;
            _left = GetChild(0);
            _right = GetChild(ElementCount - 1);

            Vector3[] _leftV3 = new Vector3[4];
            Vector3[] _rightV3 = new Vector3[4];
            _left.GetWorldCorners(_leftV3);
            _right.GetWorldCorners(_rightV3);

            if (m_MoveAxis == AxisType.Horizontal)
            {
                if (_leftV3[0].x >= GetWorldCorners(0, AxisType.Horizontal))
                {
                    return -1;
                }
                else if (_rightV3[3].x < GetWorldCorners(3, AxisType.Horizontal))
                {
                    return 1;
                }
            }
            else
            {
                if (_leftV3[0].y >= GetWorldCorners(0, AxisType.Vertical))
                {
                    return -1;
                }
                else if (_rightV3[1].y < GetWorldCorners(2, AxisType.Vertical))
                {
                    return 1;
                }
            }
            return 0;
        }

        /// <summary>
        /// Get the corners of the calculated rectangle in world space.
        /// <para>在世界空间中获得计算的矩形的角。</para>
        /// </summary>
        /// <param name="rect">Corner index.  <para>角索引</para></param>
        /// <param name="axis">Axis type.  <para>轴向</para></param>
        private float GetWorldCorners(int rect, AxisType axis)
        {
            Vector3[] _v3 = new Vector3[4];
            viewRectTran.GetWorldCorners(_v3);
            return axis == AxisType.Horizontal ? _v3[rect].x : _v3[rect].y;
        }

        /// <summary>
        /// 循环列表元素
        /// </summary>
        private void LoopElement(int dir)
        {
            if (dir == 0)
                return;

            RectTransform _moveCell;
            RectTransform _tarborder;
            Vector2 _tarPos;

            if (dir == 1)
            {
                _moveCell = GetChild(0);
                _tarborder = GetChild(ElementCount - 1);
                _moveCell.SetSiblingIndex(ElementCount - 1);
            }
            else
            {
                _moveCell = GetChild(ElementCount - 1);
                _tarborder = GetChild(0);
                _moveCell.SetSiblingIndex(0);
            }

            if (m_MoveAxis == AxisType.Horizontal)
                _tarPos = _tarborder.localPosition + new Vector3((m_ElementSize.x + m_Spacing.x) * dir, 0, 0);
            else
                _tarPos = (Vector2)_tarborder.localPosition + new Vector2(0, (m_ElementSize.y + m_Spacing.y) * dir);

            _moveCell.localPosition = _tarPos;
        }

        /// <summary>
        /// Calculate the correct position for the current loop.
        /// <para>计算当前循环的正确位置</para>
        /// </summary>
        private Vector2 CalcCorrectDeltaPos()
        {
            Vector2 _v2 = Vector2.zero;
            float _dis = float.MaxValue;
            foreach (RectTransform i in viewRectTran)
            {
                var td = Mathf.Abs(i.localPosition.x) + Mathf.Abs(i.localPosition.y);
                if (td <= _dis)
                {
                    _dis = td;
                    _v2 = i.localPosition;
                }
                else
                {
                    break;
                }
            }
            return _v2;
        }

        /// <summary>
        /// Retrieve child element by index.
        /// <para>通过索引检索子元素</para>
        /// </summary>
        private RectTransform GetChild(int index)
        {
            if (index >= m_elementCount)
            {
                return null;
            }
            return transform.GetChild(index) as RectTransform;
        }

        #endregion

        /// <summary>
        /// 移动到指定索引
        /// </summary>
        /// <param name="ind"></param>
        public void MoveToIndex(int ind)
        {
            if (m_IsNormalizing)
            {
                return;
            }
            if (ind == m_index)
            {
                return;
            }
            this.m_IsNormalizing = true;
            Vector2 offset;
            if (m_MoveAxis == AxisType.Horizontal)
            {
                offset = new Vector2(m_ElementSize.x + m_Spacing.x, 0);
            }
            else
            {
                offset = new Vector2(0, m_ElementSize.y + m_Spacing.y);
            }
            var delta = CalcCorrectDeltaPos();
            int vindex = m_index;
            m_CurrentPos = delta + offset * (ind - vindex);
            m_currentStep = 0;
        }
    }
}

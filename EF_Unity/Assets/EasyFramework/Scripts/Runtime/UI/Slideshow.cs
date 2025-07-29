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
        private Slideshow() { }

        [SerializeField]
        private AxisType _moveAxis;

        [SerializeField]
        private LoopDirectionType _loopDirection = LoopDirectionType.LeftOrDown;

        [SerializeField]
        private bool _canDrag = false;

        [SerializeField]
        private bool _autoLoop = true;

        [SerializeField]
        private int _spacingTime = 150;

        [SerializeField]
        private float _loopSpaceTime = 1;

        [SerializeField]
        private Vector2 _spacing = new Vector2(10, 10);

        [SerializeField]
        private Vector2 _elementSize = new Vector2(100, 100);
        public UnityEvent<int> OnIndexChanged { get; set; }

        /// <summary>
        /// 当前处于正中的元素
        /// </summary>
        public int CurrentIndex => _index;

        /// <summary>
        /// 元素总数
        /// </summary>
        public int ElementCount => _elementCount;

        private int _index = 0;
        private int _preIndex = 0;
        private int _currentStep = 0;
        private int _elementCount = 0;
        private float _currTimeDelta = 0;
        private bool _dragging = false;
        private bool _isNormalizing = false;
        private bool _contentCheckCache = true;

        private Vector2 _prePos;
        private Vector2 _currentPos;
        private RectTransform _header;
        private RectTransform _viewRectTran;

        protected override void Awake()
        {
            _elementCount = transform.childCount;
            _viewRectTran = GetComponent<RectTransform>();
            _header = GetChild(0);
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
            if (_isNormalizing && !_dragging)
            {
                if (_currentStep == _spacingTime)
                {
                    _isNormalizing = false;
                    _currentStep = 0;
                    _currentPos = Vector2.zero;
                    return;
                }
                Vector2 delta = _currentPos / _spacingTime;
                _currentStep++;

                foreach (RectTransform i in _viewRectTran)
                {
                    i.localPosition -= (Vector3)delta;
                }
            }
            //自动loop
            if (_autoLoop && !_isNormalizing && !_dragging)
            {
                _currTimeDelta += Time.deltaTime;
                if (_currTimeDelta > _loopSpaceTime)
                {
                    _currTimeDelta = 0;
                    MoveToIndex(_index + (int)_loopDirection);
                }
            }
            //检测index是否变化
            if (_moveAxis == AxisType.Horizontal)
            {
                _index = (int)(_header.localPosition.x / (_elementSize.x + _spacing.x - 1));
            }
            else
            {
                _index = (int)(_header.localPosition.y / (_elementSize.y + _spacing.y - 1));
            }
            if (_index <= 0)
            {
                _index = Mathf.Abs(_index);
            }
            else
            {
                _index = ElementCount - _index;
            }
            if (_index != _preIndex)
            {
                OnIndexChanged?.Invoke(_index);
            }
            _preIndex = _index;

        }

        #region Drag Handler
        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (!_canDrag || !_contentCheckCache)
            {
                return;
            }
            if (((eventData.button == PointerEventData.InputButton.Left) && IsActive()) && RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewRectTran, eventData.position, eventData.pressEventCamera, out Vector2 vector))
            {
                _dragging = true;
                _prePos = vector;
            }
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (!_canDrag || !_contentCheckCache)
            {
                return;
            }
            if (((eventData.button == PointerEventData.InputButton.Left) && IsActive()) && RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewRectTran, eventData.position, eventData.pressEventCamera, out Vector2 vector))
            {
                _isNormalizing = false;
                _currentPos = Vector2.zero;
                _currentStep = 0;

                Vector2 vector2 = vector - _prePos;
                if (_moveAxis == AxisType.Horizontal)
                    vector2.y = 0;
                else
                    vector2.x = 0;

                foreach (RectTransform i in _viewRectTran)
                {
                    i.localPosition += (Vector3)vector2;
                }

                _prePos = vector;
            }
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (!_canDrag || !_contentCheckCache)
            {
                return;
            }
            _dragging = false;
            _isNormalizing = true;
            _currentPos = CalcCorrectDeltaPos();
            _currentStep = 0;
        }
        #endregion

        #region Private Function
        /// <summary>
        /// Resize the elements
        /// <para>调整元素尺寸</para>
        /// </summary>
        private void ResizeChildren()
        {
            Vector2 delta = _moveAxis == AxisType.Horizontal ? new Vector2(_elementSize.x + _spacing.x, 0) : new Vector2(0, _elementSize.y + _spacing.y);

            for (int i = 0; i < ElementCount; i++)
            {
                var t = GetChild(i);
                if (t)
                {
                    t.localPosition = delta * i;
                    t.sizeDelta = _elementSize;
                }
            }
            _isNormalizing = false;
            _currentPos = Vector2.zero;
            _currentStep = 0;
        }

        /// <summary>
        /// Whether the content is larger than itself
        /// <para>内容是否比自身范围大</para>
        /// </summary>
        private bool ContentIsLongerThanSelf()
        {
            float contentLen;
            float rectLen;
            if (_moveAxis == AxisType.Horizontal)
            {
                contentLen = ElementCount * (_elementSize.x + _spacing.x) - _spacing.x;
                rectLen = _viewRectTran.rect.xMax - _viewRectTran.rect.xMin;
            }
            else
            {
                contentLen = ElementCount * (_elementSize.y + _spacing.y) - _spacing.y;
                rectLen = _viewRectTran.rect.yMax - _viewRectTran.rect.yMin;
            }
            _contentCheckCache = contentLen > rectLen;
            return _contentCheckCache;
        }

        /// <summary>
        /// Boundary cases are detected and divided into 0 not touching the boundary, -1 left (bottom) touching the boundary, and 1 right (top) touching the boundary.
        /// <para>检测边界情况，分为0未触界，-1左(下)触界，1右(上)触界</para>
        /// </summary>
        private int GetBoundaryState()
        {
            RectTransform left;
            RectTransform right;
            left = GetChild(0);
            right = GetChild(ElementCount - 1);

            Vector3[] _leftV3 = new Vector3[4];
            Vector3[] _rightV3 = new Vector3[4];
            left.GetWorldCorners(_leftV3);
            right.GetWorldCorners(_rightV3);

            if (_moveAxis == AxisType.Horizontal)
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
            Vector3[] v3Array = new Vector3[4];
            _viewRectTran.GetWorldCorners(v3Array);
            return axis == AxisType.Horizontal ? v3Array[rect].x : v3Array[rect].y;
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

            if (_moveAxis == AxisType.Horizontal)
                _tarPos = _tarborder.localPosition + new Vector3((_elementSize.x + _spacing.x) * dir, 0, 0);
            else
                _tarPos = (Vector2)_tarborder.localPosition + new Vector2(0, (_elementSize.y + _spacing.y) * dir);

            _moveCell.localPosition = _tarPos;
        }

        /// <summary>
        /// Calculate the correct position for the current loop.
        /// <para>计算当前循环的正确位置</para>
        /// </summary>
        private Vector2 CalcCorrectDeltaPos()
        {
            Vector2 v2 = Vector2.zero;
            float dis = float.MaxValue;
            foreach (RectTransform i in _viewRectTran)
            {
                var td = Mathf.Abs(i.localPosition.x) + Mathf.Abs(i.localPosition.y);
                if (td <= dis)
                {
                    dis = td;
                    v2 = i.localPosition;
                }
                else
                {
                    break;
                }
            }
            return v2;
        }

        /// <summary>
        /// Retrieve child element by index.
        /// <para>通过索引检索子元素</para>
        /// </summary>
        private RectTransform GetChild(int index)
        {
            if (index >= _elementCount)
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
            if (_isNormalizing)
            {
                return;
            }
            if (ind == _index)
            {
                return;
            }
            this._isNormalizing = true;
            Vector2 offset;
            if (_moveAxis == AxisType.Horizontal)
            {
                offset = new Vector2(_elementSize.x + _spacing.x, 0);
            }
            else
            {
                offset = new Vector2(0, _elementSize.y + _spacing.y);
            }
            var delta = CalcCorrectDeltaPos();
            int vindex = _index;
            _currentPos = delta + offset * (ind - vindex);
            _currentStep = 0;
        }
    }
}

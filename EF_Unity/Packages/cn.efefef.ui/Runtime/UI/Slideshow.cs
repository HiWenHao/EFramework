/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2023-04-10 15:11:01
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-11-23 17:44:19
 * ScriptVersion:   0.1
 * ===============================================
 */

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace EasyFramework.Ui
{
    /// <summary>
    /// 轮播图、幻灯片
    /// <para>Carousel / slideshow component with loop, drag and auto-play support</para>
    /// </summary>
    [AddComponentMenu("UI/Slideshow", 103)]
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(UnityEngine.UI.Mask))]
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public class Slideshow : UIBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const int LoopGuardExtra = 1; //  边界循环保护额外余量

        [SerializeField] private UnityEvent<int> onIndexChanged = new UnityEvent<int>(); //  索引变化事件

        /// <summary>索引变化事件 <para>Raised when the centered index changes</para></summary>
        public UnityEvent<int> OnIndexChanged => onIndexChanged;

        /// <summary>当前处于正中的元素 <para>Index of the element currently at the center</para></summary>
        public int CurrentIndex => _index;

        /// <summary>元素总数 <para>Total number of elements</para></summary>
        public int ElementCount => _elementCount;

        protected Slideshow()
        {
        }

        [HeaderPro("行为", "Behavior", HeaderProType.Title)]
        [HeaderPro("移动轴", "Move Axis", HeaderProType.Note)]
        [SerializeField]
        private AxisType _moveAxis = AxisType.Horizontal; //  移动轴

        [HeaderPro("循环方向", "Loop Direction", HeaderProType.Note)] [SerializeField]
        private LoopDirectionType _loopDirection = LoopDirectionType.LeftOrDown; //  循环方向

        [HeaderPro("允许拖拽", "Can Drag", HeaderProType.Note)] [SerializeField]
        private bool _canDrag = false; //  是否允许拖拽

        [HeaderPro("自动循环", "Auto Loop", HeaderProType.Note)] [SerializeField]
        private bool _autoLoop = true; //  是否自动循环

        [HeaderPro("时序", "Timing", HeaderProType.Title)]
        [HeaderPro("切换间隔（帧）", "Spacing Time (frames)", HeaderProType.Note)]
        [SerializeField]
        private int _spacingTime = 150; //  缓动帧数

        [HeaderPro("循环间隔（秒）", "Loop Space Time (s)", HeaderProType.Note)] [SerializeField]
        private float _loopSpaceTime = 1; //  自动循环间隔

        [HeaderPro("布局", "Layout", HeaderProType.Title)]
        [HeaderPro("元素间距", "Spacing", HeaderProType.Note)]
        [SerializeField]
        private Vector2 _spacing = new Vector2(10, 10); //  元素间距

        [HeaderPro("元素尺寸", "Element Size", HeaderProType.Note)] [SerializeField]
        private Vector2 _elementSize = new Vector2(100, 100); //  元素尺寸

        private int _index = 0; //  当前索引（可能为无界计数值）
        private int _preIndex = 0; //  上次上报的索引
        private int _currentStep = 0; //  缓动已走帧数
        private int _elementCount = 0; //  子元素数量
        private float _currTimeDelta = 0; //  自动循环计时
        private bool _dragging = false; //  是否正在拖拽
        private bool _isNormalizing = false; //  是否正在缓动归位
        private bool _contentCheckCache = true; //  内容是否超过视口

        private Vector2 _prePos; //  上次指针本地坐标
        private Vector2 _currentPos; //  缓动总位移
        private RectTransform _header; //  首元素引用
        private RectTransform _viewRectTran; //  视口 RectTransform

        protected override void Awake()
        {
            base.Awake();
            _viewRectTran = GetComponent<RectTransform>();
            _elementCount = transform.childCount;
            _header = GetChild(0);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResizeChildren();
            if (!ContentIsLongerThanSelf())
                return;

            //  边界循环最多执行元素数次，避免异常数据导致死循环
            int guard = 0;
            int s;
            do
            {
                s = GetBoundaryState();
                LoopElement(s);
                guard++;
            } while (s != 0 && guard <= _elementCount + LoopGuardExtra);
        }

        protected virtual void Update()
        {
            if (_viewRectTran == null || !ContentIsLongerThanSelf())
                return;

            int s = GetBoundaryState();
            LoopElement(s);
            //缓动回指定位置
            if (_isNormalizing && !_dragging)
            {
                int spacingFrames = Mathf.Max(1, _spacingTime);
                if (_currentStep >= spacingFrames)
                {
                    _isNormalizing = false;
                    _currentStep = 0;
                    _currentPos = Vector2.zero;
                }
                else
                {
                    Vector2 delta = _currentPos / spacingFrames;
                    _currentStep++;

                    foreach (RectTransform i in _viewRectTran)
                    {
                        i.localPosition -= (Vector3)delta;
                    }
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

            //检测index是否变化：保留原始的无界计数语义，不做 Clamp
            UpdateIndex();
        }

        #region Drag Handler

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (!_canDrag || !_contentCheckCache || _viewRectTran == null)
                return;

            if (eventData.button == PointerEventData.InputButton.Left && IsActive() &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewRectTran, eventData.position,
                    eventData.pressEventCamera, out Vector2 vector))
            {
                _dragging = true;
                _prePos = vector;
            }
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (!_canDrag || !_contentCheckCache || !_dragging || _viewRectTran == null)
                return;

            if (eventData.button == PointerEventData.InputButton.Left && IsActive() &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewRectTran, eventData.position,
                    eventData.pressEventCamera, out Vector2 vector))
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
            if (!_canDrag || !_contentCheckCache || !_dragging || _viewRectTran == null)
                return;

            _dragging = false;
            _isNormalizing = true;
            _currentPos = CalcCorrectDeltaPos();
            _currentStep = 0;
        }

        #endregion

        #region Private Function

        //  按 header 累积位置反推索引；减 1 用于规避整步长时的浮点舍入误差
        private void UpdateIndex()
        {
            if (_header == null)
                return;

            float step = GetElementStep();
            if (step <= 1f)
                return;

            float position = _moveAxis == AxisType.Horizontal ? _header.localPosition.x : _header.localPosition.y;
            int index = (int)(position / (step - 1f));
            _index = index <= 0 ? Mathf.Abs(index) : ElementCount - index;

            if (_index != _preIndex)
            {
                _preIndex = _index;
                onIndexChanged.Invoke(_index);
            }
        }

        //  单个元素在布局轴上占据的步长
        private float GetElementStep()
        {
            return _moveAxis == AxisType.Horizontal
                ? _elementSize.x + _spacing.x
                : _elementSize.y + _spacing.y;
        }

        /// <summary>
        /// 调整元素尺寸与初始位置
        /// <para>Resizes elements and resets their initial positions</para>
        /// </summary>
        private void ResizeChildren()
        {
            Vector2 delta = _moveAxis == AxisType.Horizontal
                ? new Vector2(_elementSize.x + _spacing.x, 0)
                : new Vector2(0, _elementSize.y + _spacing.y);

            for (int i = 0; i < ElementCount; i++)
            {
                RectTransform t = GetChild(i);
                if (t != null)
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
        /// 内容是否比视口长
        /// <para>Whether the content is longer than the viewport</para>
        /// </summary>
        private bool ContentIsLongerThanSelf()
        {
            if (_viewRectTran == null)
                return false;

            float step = GetElementStep();
            float contentLen = ElementCount * step - (_moveAxis == AxisType.Horizontal ? _spacing.x : _spacing.y);
            float rectLen = _moveAxis == AxisType.Horizontal
                ? _viewRectTran.rect.xMax - _viewRectTran.rect.xMin
                : _viewRectTran.rect.yMax - _viewRectTran.rect.yMin;

            _contentCheckCache = contentLen > rectLen;
            return _contentCheckCache;
        }

        /// <summary>
        /// 检测边界：0 未触界，-1 左(下)触界，1 右(上)触界
        /// <para>Detects boundary: 0 none, -1 left/bottom, 1 right/top</para>
        /// </summary>
        private int GetBoundaryState()
        {
            RectTransform left = GetChild(0);
            RectTransform right = GetChild(ElementCount - 1);
            if (left == null || right == null)
                return 0;

            Vector3[] leftCorners = new Vector3[4];
            Vector3[] rightCorners = new Vector3[4];
            left.GetWorldCorners(leftCorners);
            right.GetWorldCorners(rightCorners);

            if (_moveAxis == AxisType.Horizontal)
            {
                if (leftCorners[0].x >= GetWorldCorner(0, AxisType.Horizontal))
                    return -1;
                if (rightCorners[3].x < GetWorldCorner(3, AxisType.Horizontal))
                    return 1;
            }
            else
            {
                if (leftCorners[0].y >= GetWorldCorner(0, AxisType.Vertical))
                    return -1;
                if (rightCorners[1].y < GetWorldCorner(2, AxisType.Vertical))
                    return 1;
            }

            return 0;
        }

        /// <summary>
        /// 获取视口指定角的世界坐标
        /// <para>Gets a viewport corner in world space</para>
        /// </summary>
        /// <param name="corner">角索引 <para>Corner index</para></param>
        /// <param name="axis">坐标轴 <para>Axis</para></param>
        private float GetWorldCorner(int corner, AxisType axis)
        {
            Vector3[] corners = new Vector3[4];
            _viewRectTran.GetWorldCorners(corners);
            return axis == AxisType.Horizontal ? corners[corner].x : corners[corner].y;
        }

        /// <summary>
        /// 把边界元素搬到另一端
        /// <para>Moves the boundary element to the opposite end</para>
        /// </summary>
        /// <param name="dir">循环方向 <para>Loop direction</para></param>
        private void LoopElement(int dir)
        {
            if (dir == 0)
                return;

            RectTransform moveCell;
            RectTransform border;
            if (dir == 1)
            {
                moveCell = GetChild(0);
                border = GetChild(ElementCount - 1);
                if (moveCell == null || border == null)
                    return;
                moveCell.SetSiblingIndex(ElementCount - 1);
            }
            else
            {
                moveCell = GetChild(ElementCount - 1);
                border = GetChild(0);
                if (moveCell == null || border == null)
                    return;
                moveCell.SetSiblingIndex(0);
            }

            Vector2 target = _moveAxis == AxisType.Horizontal
                ? (Vector2)border.localPosition + new Vector2(GetElementStep() * dir, 0)
                : (Vector2)border.localPosition + new Vector2(0, GetElementStep() * dir);
            moveCell.localPosition = target;
        }

        /// <summary>
        /// 计算最接近原点的位置作为归位基准
        /// <para>Calculates the position closest to the origin as the normalize target</para>
        /// </summary>
        private Vector2 CalcCorrectDeltaPos()
        {
            if (_viewRectTran == null)
                return Vector2.zero;

            Vector2 result = Vector2.zero;
            float minDistance = float.MaxValue;
            foreach (RectTransform child in _viewRectTran)
            {
                float distance = Mathf.Abs(child.localPosition.x) + Mathf.Abs(child.localPosition.y);
                if (distance >= minDistance)
                    continue;

                minDistance = distance;
                result = child.localPosition;
            }

            return result;
        }

        /// <summary>
        /// 通过索引检索子元素
        /// <para>Retrieves a child by index</para>
        /// </summary>
        /// <param name="index">子元素索引 <para>Child index</para></param>
        private RectTransform GetChild(int index)
        {
            if (index < 0 || index >= _elementCount || _elementCount != transform.childCount)
                return null;

            return transform.GetChild(index) as RectTransform;
        }

        #endregion

        /// <summary>
        /// 移动到指定索引（相对当前滚动位置的增量目标）
        /// <para>Moves to the target index relative to the current scroll position</para>
        /// </summary>
        /// <param name="ind">目标索引 <para>Target index</para></param>
        public void MoveToIndex(int ind)
        {
            if (_isNormalizing)
                return;
            if (ind == _index)
                return;

            _isNormalizing = true;
            Vector2 offset = _moveAxis == AxisType.Horizontal
                ? new Vector2(GetElementStep(), 0)
                : new Vector2(0, GetElementStep());

            Vector2 delta = CalcCorrectDeltaPos();
            _currentPos = delta + offset * (ind - _index);
            _currentStep = 0;
        }
    }
}

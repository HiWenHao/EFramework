using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using UnityEngine;

/// <summary>
/// 图片轮播组件
/// </summary>
public class Slideshow : UIBehaviour, IBeginDragHandler, IInitializePotentialDragHandler, IDragHandler, IEndDragHandler, ICanvasElement
{
    /// <summary>
    /// 滚动方向H or V
    /// </summary>
    public enum AxisType
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// 图片轮播方向
    /// </summary>
    public enum LoopDirType
    {
        RightOrUp = -1,
        LeftOrDown = 1,
    }

    /// <summary>
    /// 子物体size
    /// </summary>
    public Vector2 mCellSize;

    /// <summary>
    /// 子物体间隔
    /// </summary>
    public Vector2 mSpacing;

    /// <summary>
    /// 方向
    /// </summary>
    public AxisType MMoveAxisType;

    /// <summary>
    /// 轮播方向-- 1为向左移动，-1为向右移动
    /// </summary>
    public LoopDirType mLoopDirType = LoopDirType.LeftOrDown;

    /// <summary>
    /// Tween时的步数
    /// </summary>
    [Range(1, 500)]
    public int mTweenStepNum = 150;

    /// <summary>
    /// 自动轮播
    /// </summary>
    public bool mAutoLoop = false;

    /// <summary>
    /// 可否拖动
    /// </summary>
    public bool mDrag = false;

    /// <summary>
    /// 下一次播放间隔时间
    /// </summary>
    public float mLoopSpaceTime = 1;

    /// <summary>
    /// 位于正中的子元素变化的事件,参数为index
    /// </summary>
    [HideInInspector]
    public Action<int> mOnIndexChange;
    /// <summary>
    /// 当前处于正中的元素
    /// </summary>
    public int CurrentIndex
    {
        get
        {
            return m_index;
        }
    }

    private bool m_Dragging = false;
    private bool m_IsNormalizing = false;
    private Vector2 m_CurrentPos;
    private int m_currentStep = 0;
    private RectTransform viewRectTran;
    private Vector2 m_PrePos;
    private int m_index = 0, m_preIndex = 0;
    private RectTransform header;
    private bool contentCheckCache = true;

    private float currTimeDelta = 0;
    private float viewRectXMin
    {
        get
        {
            Vector3[] v = new Vector3[4];
            viewRectTran.GetWorldCorners(v);
            return v[0].x;
        }
    }
    private float viewRectXMax
    {
        get
        {
            Vector3[] v = new Vector3[4];
            viewRectTran.GetWorldCorners(v);
            return v[3].x;
        }
    }
    private float viewRectYMin
    {
        get
        {
            Vector3[] v = new Vector3[4];
            viewRectTran.GetWorldCorners(v);
            return v[0].y;
        }
    }
    private float viewRectYMax
    {
        get
        {
            Vector3[] v = new Vector3[4];
            viewRectTran.GetWorldCorners(v);
            return v[2].y;
        }
    }

    public int CellCount
    {
        get
        {
            return transform.childCount;
        }
    }
    protected override void Awake()
    {
        viewRectTran = GetComponent<RectTransform>();
        header = GetChild(viewRectTran, 0);
    }



    public void resizeChildren()
    {
        Vector2 delta = MMoveAxisType == AxisType.Horizontal ? new Vector2(mCellSize.x + mSpacing.x, 0) : new Vector2(0, mCellSize.y + mSpacing.y);

        for (int i = 0; i < CellCount; i++)
        {
            var t = GetChild(viewRectTran, i);
            if (t)
            {
                t.localPosition = delta * i;
                t.sizeDelta = mCellSize;
            }
        }
        m_IsNormalizing = false;
        m_CurrentPos = Vector2.zero;
        m_currentStep = 0;
    }
    /// <summary>
    /// 加子物体到当前列表的最后面
    /// </summary>
    /// <param name="t"></param>
    public virtual void AddChild(RectTransform t)
    {
        if (t != null)
        {
            t.SetParent(viewRectTran, false);
            t.SetAsLastSibling();
            Vector2 delta;
            if (MMoveAxisType == AxisType.Horizontal)
            {
                delta = new Vector2(mCellSize.x + mSpacing.x, 0);
            }
            else
            {
                delta = new Vector2(0, mCellSize.y + mSpacing.y);
            }
            if (CellCount == 0)
            {
                t.localPosition = Vector3.zero;
                header = t;
            }
            else
            {
                t.localPosition = delta + (Vector2)GetChild(viewRectTran, CellCount - 1).localPosition;
            }
        }
    }
    protected override void OnEnable()
    {
        base.OnEnable();

        mOnIndexChange += OnChangeIndex;

        resizeChildren();
        //return;
        if (Application.isPlaying)
        {
            if (ContentIsLongerThanRect())
            {
                int s;
                do
                {
                    s = GetBoundaryState();
                    LoopCell(s);
                } while (s != 0);
            }
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        mOnIndexChange -= OnChangeIndex;

    }

    protected virtual void Update()
    {
        if (ContentIsLongerThanRect())
        {
            //实现在必要时loop子元素
            if (Application.isPlaying)
            {
                int s = GetBoundaryState();
                LoopCell(s);
            }
            //缓动回指定位置
            if (m_IsNormalizing && EnsureListCanAdjust())
            {
                if (m_currentStep == mTweenStepNum)
                {
                    m_IsNormalizing = false;
                    m_currentStep = 0;
                    m_CurrentPos = Vector2.zero;
                    return;
                }
                Vector2 delta = m_CurrentPos / mTweenStepNum;
                m_currentStep++;
                TweenToCorrect(-delta);
            }
            //自动loop
            if (mAutoLoop && !m_IsNormalizing && EnsureListCanAdjust())
            {
                currTimeDelta += Time.deltaTime;
                if (currTimeDelta > mLoopSpaceTime)
                {
                    currTimeDelta = 0;
                    MoveToIndex(m_index + (int)mLoopDirType);
                }
            }
            //检测index是否变化
            if (MMoveAxisType == AxisType.Horizontal)
            {
                m_index = (int)(header.localPosition.x / (mCellSize.x + mSpacing.x - 1));
            }
            else
            {
                m_index = (int)(header.localPosition.y / (mCellSize.y + mSpacing.y - 1));
            }
            if (m_index <= 0)
            {
                m_index = Mathf.Abs(m_index);
            }
            else
            {
                m_index = CellCount - m_index;
            }
            if (m_index != m_preIndex)
            {
                if (mOnIndexChange != null)
                {
                    mOnIndexChange(m_index);
                }
            }
            m_preIndex = m_index;
        }
    }
    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        if (!mDrag || !contentCheckCache)
        {
            return;
        }
        Vector2 vector;
        if (((eventData.button == PointerEventData.InputButton.Left) && this.IsActive()) && RectTransformUtility.ScreenPointToLocalPointInRectangle(this.viewRectTran, eventData.position, eventData.pressEventCamera, out vector))
        {
            this.m_Dragging = true;
            m_PrePos = vector;
        }
    }

    public virtual void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (!mDrag)
        {
            return;
        }
        return;
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        if (!mDrag || !contentCheckCache)
        {
            return;
        }
        Vector2 vector;
        if (((eventData.button == PointerEventData.InputButton.Left) && this.IsActive()) && RectTransformUtility.ScreenPointToLocalPointInRectangle(this.viewRectTran, eventData.position, eventData.pressEventCamera, out vector))
        {
            m_IsNormalizing = false;
            m_CurrentPos = Vector2.zero;
            m_currentStep = 0;
            Vector2 vector2 = vector - this.m_PrePos;
            Vector2 vec = CalculateOffset(vector2);
            this.SetContentPosition(vec);
            m_PrePos = vector;
        }
    }
    /// <summary>
    /// 移动到指定索引
    /// </summary>
    /// <param name="ind"></param>
    public virtual void MoveToIndex(int ind)
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
        if (MMoveAxisType == AxisType.Horizontal)
        {
            offset = new Vector2(mCellSize.x + mSpacing.x, 0);
        }
        else
        {
            offset = new Vector2(0, mCellSize.y + mSpacing.y);
        }
        var delta = CalcCorrectDeltaPos();
        int vindex = m_index;
        m_CurrentPos = delta + offset * (ind - vindex);
        m_currentStep = 0;
    }
    private Vector2 CalculateOffset(Vector2 delta)
    {
        if (MMoveAxisType == AxisType.Horizontal)
        {
            delta.y = 0;
        }
        else
        {
            delta.x = 0;
        }
        return delta;
    }
    private void SetContentPosition(Vector2 position)
    {
        foreach (RectTransform i in viewRectTran)
        {
            i.localPosition += (Vector3)position;
        }
        return;
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (!mDrag || !contentCheckCache)
        {
            return;
        }
        this.m_Dragging = false;
        this.m_IsNormalizing = true;
        m_CurrentPos = CalcCorrectDeltaPos();
        m_currentStep = 0;
    }

    public virtual void Rebuild(CanvasUpdate executing)
    {
        return;
    }
    /// <summary>
    /// List是否处于可自由调整状态
    /// </summary>
    /// <returns></returns>
    public virtual bool EnsureListCanAdjust()
    {
        return !m_Dragging && ContentIsLongerThanRect();
    }
    /// <summary>
    /// 内容是否比显示范围大
    /// </summary>
    /// <returns></returns>
    public virtual bool ContentIsLongerThanRect()
    {
        float contentLen;
        float rectLen;
        if (MMoveAxisType == AxisType.Horizontal)
        {
            contentLen = CellCount * (mCellSize.x + mSpacing.x) - mSpacing.x;
            rectLen = viewRectTran.rect.xMax - viewRectTran.rect.xMin;
        }
        else
        {
            contentLen = CellCount * (mCellSize.y + mSpacing.y) - mSpacing.y;
            rectLen = viewRectTran.rect.yMax - viewRectTran.rect.yMin;
        }
        contentCheckCache = contentLen > rectLen;
        return contentCheckCache;
    }
    /// <summary>
    /// 检测边界情况，分为0未触界，-1左(下)触界，1右(上)触界
    /// </summary>
    /// <returns></returns>
    public virtual int GetBoundaryState()
    {
        RectTransform left;
        RectTransform right;
        left = GetChild(viewRectTran, 0);
        right = GetChild(viewRectTran, CellCount - 1);
        Vector3[] l = new Vector3[4];
        left.GetWorldCorners(l);
        Vector3[] r = new Vector3[4];
        right.GetWorldCorners(r);
        if (MMoveAxisType == AxisType.Horizontal)
        {
            if (l[0].x >= viewRectXMin)
            {
                return -1;
            }
            else if (r[3].x < viewRectXMax)
            {
                return 1;
            }
        }
        else
        {
            if (l[0].y >= viewRectYMin)
            {
                return -1;
            }
            else if (r[1].y < viewRectYMax)
            {
                return 1;
            }
        }
        return 0;
    }
    /// <summary>
    /// Loop列表，分为-1把最右(上)边一个移到最左(下)边，1把最左(下)边一个移到最右(上)边
    /// </summary>
    /// <param name="dir"></param>
    protected virtual void LoopCell(int dir)
    {
        if (dir == 0)
        {
            return;
        }
        RectTransform MoveCell;
        RectTransform Tarborder;
        Vector2 TarPos;
        if (dir == 1)
        {
            MoveCell = GetChild(viewRectTran, 0);
            Tarborder = GetChild(viewRectTran, CellCount - 1);
            MoveCell.SetSiblingIndex(CellCount - 1);
        }
        else
        {
            Tarborder = GetChild(viewRectTran, 0);
            MoveCell = GetChild(viewRectTran, CellCount - 1);
            MoveCell.SetSiblingIndex(0);
        }
        if (MMoveAxisType == AxisType.Horizontal)
        {
            TarPos = Tarborder.localPosition + new Vector3((mCellSize.x + mSpacing.x) * dir, 0, 0);
        }
        else
        {
            TarPos = (Vector2)Tarborder.localPosition + new Vector2(0, (mCellSize.y + mSpacing.y) * dir);
        }
        MoveCell.localPosition = TarPos;
    }
    /// <summary>
    /// 计算一个最近的正确位置
    /// </summary>
    /// <returns></returns>
    public virtual Vector2 CalcCorrectDeltaPos()
    {
        Vector2 delta = Vector2.zero;
        float distance = float.MaxValue;
        foreach (RectTransform i in viewRectTran)
        {
            var td = Mathf.Abs(i.localPosition.x) + Mathf.Abs(i.localPosition.y);
            if (td <= distance)
            {
                distance = td;
                delta = i.localPosition;
            }
            else
            {
                break;
            }
        }
        return delta;
    }
    /// <summary>
    /// 移动指定增量
    /// </summary>
    protected virtual void TweenToCorrect(Vector2 delta)
    {
        foreach (RectTransform i in viewRectTran)
        {
            i.localPosition += (Vector3)delta;
        }
    }

    private static RectTransform GetChild(RectTransform parent, int index)
    {
        if (parent == null || index >= parent.childCount)
        {
            return null;
        }
        return parent.GetChild(index) as RectTransform;
    }

    public void LayoutComplete()
    {
    }

    public void GraphicUpdateComplete()
    {
    }

    /// <summary>
    /// 当前中心位置index回调
    /// </summary>
    /// <param name="index"></param>
    public void OnChangeIndex(int index)
    {
        Debug.Log(index + "    is   index");
    }

}

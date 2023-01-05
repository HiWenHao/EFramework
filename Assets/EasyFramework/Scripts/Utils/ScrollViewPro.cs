/* 
 * ================================================
 * Describe:      This script is Infinite sliding. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-10-28 10:22:39
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-10-28 10:22:39
 * ScriptVersion: 0.1
 * ===============================================
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XHTools;

namespace EasyFramework.UI
{
    /// <summary>
    /// Infinite sliding. 无限滑动
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    [RequireComponent(typeof(ContentSizeFitter))]
    public class ScrollViewPro : MonoBehaviour
    {
        /*  要确定当前所属canvas的rect下方scale缩放比例，要与之保持一样。  */
        float _scale = 1;

        /* 实现无限滚动，需要的最少的child数量。
         * 屏幕上能看到的+一行看不到的，比如我在屏幕上能看到 4 行，每一行 3 个。
         * 则这个值为 4行 * 3个 + 1行 * 3个 = 15个。*/
        int childrenAmount = 0;

        #region Private Attribute
        ScrollRect scrollRect;
        RectTransform rectTransform;
        GridLayoutGroup gridLayoutGroup;
        ContentSizeFitter contentSizeFitter;
        List<RectTransform> children = new List<RectTransform>();

        int amount = 0;
        int constraintCount;
        int realIndex = -1;

        float childHeight;

        bool hasInit = false;
        Vector2 startPosition;
        Vector2 gridLayoutSize;
        Vector2 gridLayoutPos;
        Dictionary<Transform, Vector2> childsAnchoredPosition = new Dictionary<Transform, Vector2>();
        Dictionary<Transform, int> childsSiblingIndex = new Dictionary<Transform, int>();
        #endregion

        public delegate void UpdateChildrenCallbackDelegate(int index, Transform trans);
        public UpdateChildrenCallbackDelegate updateChildrenCallback = null;

        void Start() => childrenAmount = transform.childCount;

        IEnumerator InitChildren()
        {
            yield return 0;

            if (!hasInit)
            {
                //获取Grid的宽度;
                rectTransform = GetComponent<RectTransform>();

                gridLayoutGroup = GetComponent<GridLayoutGroup>();
                gridLayoutGroup.enabled = false;
                constraintCount = gridLayoutGroup.constraintCount;
                childHeight = gridLayoutGroup.cellSize.y;
                contentSizeFitter = GetComponent<ContentSizeFitter>();
                contentSizeFitter.enabled = false;

                gridLayoutPos = rectTransform.anchoredPosition;
                gridLayoutSize = rectTransform.sizeDelta;


                //注册ScrollRect滚动回调;
                scrollRect = transform.parent.GetComponent<ScrollRect>();
                scrollRect.onValueChanged.AddListener((data) => { ScrollCallback(data); });

                //获取所有child anchoredPosition 以及 SiblingIndex;
                for (int index = 0; index < childrenAmount; index++)
                {
                    Transform child = transform.GetChild(index);
                    RectTransform childRectTrans = child.GetComponent<RectTransform>();
                    childsAnchoredPosition.Add(child, childRectTrans.anchoredPosition);
                    childsSiblingIndex.Add(child, child.GetSiblingIndex());
                }
            }
            else
            {
                rectTransform.anchoredPosition = gridLayoutPos;
                rectTransform.sizeDelta = gridLayoutSize;

                children.Clear();

                realIndex = -1;

                //children重新设置上下顺序;
                foreach (var info in childsSiblingIndex)
                {
                    info.Key.SetSiblingIndex(info.Value);
                }

                //children重新设置anchoredPosition;
                for (int index = 0; index < childrenAmount; index++)
                {
                    Transform child = transform.GetChild(index);

                    RectTransform childRectTrans = child.GetComponent<RectTransform>();
                    if (childsAnchoredPosition.ContainsKey(child))
                    {
                        childRectTrans.anchoredPosition = childsAnchoredPosition[child];
                    }
                    else
                    {
                        Debug.LogError("Unity Error Log : childs Anchored Position are no contain " + child.name);
                    }
                }
            }

            //int needCount = (minAmount < amount) ? minAmount : amount;
            //获取所有child;
            for (int _idx = 0; _idx < childrenAmount; _idx++)
            {
                Transform child = transform.GetChild(_idx);
                child.gameObject.SetActive(true);

                RectTransform rect = child.GetComponent<RectTransform>();
                children.Add(rect);

                //初始化前面几个;
                if (_idx < amount)
                {
                    UpdateChildrenInfoCallback(_idx, child);
                }
            }

            startPosition = rectTransform.anchoredPosition;

            realIndex = children.Count - 1;

            //Debug.Log( scrollRect.transform.TransformPoint(Vector3.zero));
            //Debug.Log(transform.TransformPoint(children[0].localPosition));

            hasInit = true;

            //如果需要显示的个数小于设定的个数;
            for (int index = 0; index < childrenAmount; index++)
            {
                children[index].gameObject.SetActive(index < amount);
            }

            if (gridLayoutGroup.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            {
                //如果小了一行，则需要把GridLayout的高度减去一行的高度;
                int row = (childrenAmount - amount) / constraintCount;
                //Debug.Log($"---------minAmount = {minAmount}----amount = {amount}-----constraintCount = {constraintCount}-------row = {row}--- ");
                if (row > 0)
                {
                    rectTransform.sizeDelta -= new Vector2(0, (gridLayoutGroup.cellSize.y + gridLayoutGroup.spacing.y) * row);
                }
            }
            else
            {
                //如果小了一列，则需要把GridLayout的宽度减去一列的宽度;
                int column = (childrenAmount - amount) / constraintCount;
                if (column > 0)
                {
                    rectTransform.sizeDelta -= new Vector2((gridLayoutGroup.cellSize.x + gridLayoutGroup.spacing.x) * column, 0);
                }
            }
        }

        /// <summary>
        /// 设置总的个数;
        /// </summary>
        /// <param name="count">总个数</param>
        public void InitSetAmount(int count)
        {
            amount = count;
            StartCoroutine(InitChildren());
        }

        /// <summary>
        /// 滑动回调
        /// </summary>
        void ScrollCallback(Vector2 data)
        {
            if (data.y >= 1.0f)
                return;
            UpdateChildrenInfo();
        }

        /// <summary>
        /// 子物体的更改
        /// </summary>
        void UpdateChildrenInfo()
        {
            if (childrenAmount < transform.childCount)
                return;

            Vector2 currentPos = rectTransform.anchoredPosition;

            if (gridLayoutGroup.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            {
                float offsetY = currentPos.y - startPosition.y;

                //Debug.Log("offsetY is " + (offsetY > 0.0f));
                if (offsetY > 0)
                {
                    //向上拉，向下扩展;
                    {
                        if (realIndex >= amount - 1)
                        {
                            startPosition = currentPos;
                            return;
                        }

                        float scrollRectUp = scrollRect.transform.TransformPoint(Vector3.zero).y;

                        Vector3 childBottomLeft = new Vector3(children[0].anchoredPosition.x, children[0].anchoredPosition.y - gridLayoutGroup.cellSize.y, 0f);
                        float childBottom = transform.TransformPoint(childBottomLeft).y;

                        if (childBottom >= scrollRectUp + childHeight * _scale)
                        {
                            //移动到底部;
                            for (int index = 0; index < constraintCount; index++)
                            {
                                children[index].SetAsLastSibling();

                                children[index].anchoredPosition = new Vector2(children[index].anchoredPosition.x, children[children.Count - 1].anchoredPosition.y - gridLayoutGroup.cellSize.y - gridLayoutGroup.spacing.y);

                                realIndex++;

                                if (realIndex > amount - 1)
                                {
                                    children[index].gameObject.SetActive(false);
                                }
                                else
                                {
                                    UpdateChildrenInfoCallback(realIndex, children[index]);
                                }
                            }

                            //GridLayoutGroup 底部加长;
                            rectTransform.sizeDelta += new Vector2(0, gridLayoutGroup.cellSize.y + gridLayoutGroup.spacing.y);

                            //更新child;
                            for (int index = 0; index < children.Count; index++)
                            {
                                children[index] = transform.GetChild(index).GetComponent<RectTransform>();
                            }
                        }
                    }
                }
                else
                {
                    //向下拉，下面收缩;
                    if (realIndex + 1 <= children.Count)
                    {
                        startPosition = currentPos;
                        return;
                    }
                    RectTransform scrollRectTransform = scrollRect.GetComponent<RectTransform>();
                    Vector3 scrollRectAnchorBottom = new Vector3(0, -scrollRectTransform.rect.height - gridLayoutGroup.spacing.y, 0f);
                    float scrollRectBottom = scrollRect.transform.TransformPoint(scrollRectAnchorBottom).y;

                    Vector3 childUpLeft = new Vector3(children[children.Count - 1].anchoredPosition.x, children[children.Count - 1].anchoredPosition.y, 0f);

                    float childUp = transform.TransformPoint(childUpLeft).y;

                    if (childUp < scrollRectBottom)
                    {
                        //把底部的一行 移动到顶部
                        for (int index = 0; index < constraintCount; index++)
                        {
                            children[children.Count - 1 - index].SetAsFirstSibling();

                            children[children.Count - 1 - index].anchoredPosition = new Vector2(children[children.Count - 1 - index].anchoredPosition.x, children[0].anchoredPosition.y + gridLayoutGroup.cellSize.y + gridLayoutGroup.spacing.y);

                            children[children.Count - 1 - index].gameObject.SetActive(true);

                            UpdateChildrenInfoCallback(realIndex - children.Count - index, children[children.Count - 1 - index]);
                        }

                        realIndex -= constraintCount;

                        //GridLayoutGroup 底部缩短;
                        rectTransform.sizeDelta -= new Vector2(0, gridLayoutGroup.cellSize.y + gridLayoutGroup.spacing.y);

                        //更新child;
                        for (int index = 0; index < children.Count; index++)
                        {
                            children[index] = transform.GetChild(index).GetComponent<RectTransform>();
                        }
                    }
                }
            }
            else
            {
                float offsetX = currentPos.x - startPosition.x;

                if (offsetX < 0)
                {
                    //向左拉，向右扩展;
                    {
                        if (realIndex >= amount - 1)
                        {
                            startPosition = currentPos;
                            return;
                        }

                        float scrollRectLeft = scrollRect.transform.TransformPoint(Vector3.zero).x;

                        Vector3 childBottomRight = new Vector3(children[0].anchoredPosition.x + gridLayoutGroup.cellSize.x, children[0].anchoredPosition.y, 0f);
                        float childRight = transform.TransformPoint(childBottomRight).x;

                        if (childRight <= scrollRectLeft)
                        {
                            //移动到右边;
                            for (int index = 0; index < constraintCount; index++)
                            {
                                children[index].SetAsLastSibling();

                                children[index].anchoredPosition = new Vector2(children[children.Count - 1].anchoredPosition.x + gridLayoutGroup.cellSize.x + gridLayoutGroup.spacing.x, children[index].anchoredPosition.y);

                                realIndex++;

                                if (realIndex > amount - 1)
                                {
                                    children[index].gameObject.SetActive(false);
                                }
                                else
                                {
                                    UpdateChildrenInfoCallback(realIndex, children[index]);
                                }
                            }

                            //GridLayoutGroup 右侧加长;
                            rectTransform.sizeDelta += new Vector2(gridLayoutGroup.cellSize.x + gridLayoutGroup.spacing.x, 0);

                            //更新child;
                            for (int index = 0; index < children.Count; index++)
                            {
                                children[index] = transform.GetChild(index).GetComponent<RectTransform>();
                            }
                        }
                    }
                }
                else
                {
                    //向右拉，右边收缩;
                    if (realIndex + 1 <= children.Count)
                    {
                        startPosition = currentPos;
                        return;
                    }
                    RectTransform scrollRectTransform = scrollRect.GetComponent<RectTransform>();
                    Vector3 scrollRectAnchorRight = new Vector3(scrollRectTransform.rect.width + gridLayoutGroup.spacing.x, 0, 0f);
                    float scrollRectRight = scrollRect.transform.TransformPoint(scrollRectAnchorRight).x;

                    Vector3 childUpLeft = new Vector3(children[children.Count - 1].anchoredPosition.x, children[children.Count - 1].anchoredPosition.y, 0f);

                    float childLeft = transform.TransformPoint(childUpLeft).x;

                    if (childLeft >= scrollRectRight)
                    {
                        //把右边的一行 移动到左边;
                        for (int index = 0; index < constraintCount; index++)
                        {
                            children[children.Count - 1 - index].SetAsFirstSibling();

                            children[children.Count - 1 - index].anchoredPosition = new Vector2(children[0].anchoredPosition.x - gridLayoutGroup.cellSize.x - gridLayoutGroup.spacing.x, children[children.Count - 1 - index].anchoredPosition.y);

                            children[children.Count - 1 - index].gameObject.SetActive(true);

                            UpdateChildrenInfoCallback(realIndex - children.Count - index, children[children.Count - 1 - index]);
                        }

                        //GridLayoutGroup 右侧缩短;
                        rectTransform.sizeDelta -= new Vector2(gridLayoutGroup.cellSize.x + gridLayoutGroup.spacing.x, 0);

                        //更新child;
                        for (int index = 0; index < children.Count; index++)
                        {
                            children[index] = transform.GetChild(index).GetComponent<RectTransform>();
                        }

                        realIndex -= constraintCount;
                    }
                }
            }

            startPosition = currentPos;
        }

        /// <summary>
        /// 更新回调
        /// </summary>
        /// <param name="index">当前索引</param>
        /// <param name="trans">当前物体</param>
        void UpdateChildrenInfoCallback(int index, Transform trans)
        {
            updateChildrenCallback?.Invoke(index, trans);
        }
    }
}

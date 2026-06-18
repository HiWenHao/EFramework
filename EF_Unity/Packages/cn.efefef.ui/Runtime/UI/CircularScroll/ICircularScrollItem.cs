/*
 * ================================================
 * Describe:        循环滚动列表元素接口
 * Author:          Alvin8412
 * CreationTime:    2026-06-18 15:39:20
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-06-18 15:39:20
 * ScriptVersion:   0.1
 * ================================================
 */

using UnityEngine;

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// 循环滚动列表项接口<br/>
    /// 任何需要加入滚动列表的 Item 实现此接口即可
    /// <para>Circular scrolling list item interface<br/>
    /// Any Item that needs to be included in a scrollable list should implement this interface</para>
    /// </summary>
    public interface ICircularScrollItem
    {
        /// <summary>
        /// 当前绑定的数据索引
        /// <para>Currently bound data index</para>
        /// </summary>
        int DataIndex { get; set; }

        /// <summary>
        /// 元素对象
        /// <para>Element object</para>
        /// </summary>
        RectTransform RectTransform { get; }

        /// <summary>
        /// 当 Item 被分配数据时调用，在此处更新 UI
        /// <para>Called when the Item is assigned data, update UI here</para>
        /// </summary>
        void OnSetup(int dataIndex);

        /// <summary>
        /// 当 Item 被回收时调用，在此处清理状态
        /// <para>Called when the Item is recycled, clean up here</para>
        /// </summary>
        void OnRecycle();
    }
}
/*
 * ================================================
 * Describe:        无限不规则滚动列表的 item 接口
 * Author:          Alvin8412
 * CreationTime:    2026-06-04 14:48:05
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-06-04 14:48:05
 * ScriptVersion:   0.1
 * ================================================
 */

using UnityEngine;

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// 无限滚动列表的 item 必须实现此接口。
    /// <para>Must be implemented by scroll items managed by InfiniteIrregularScrollList.</para>
    /// </summary>
    public interface IScrollProItem
    {
        /// <summary>
        /// 缓存的测量尺寸
        /// <para>Cached measured size. 由 OnShow 设置，列表读取 / Set by OnShow, read by list.</para>
        /// </summary>
        float MeasuredSize { get; set; }

        /// <summary>
        /// 首次被创建时调用一次
        /// <para>Called once on first creation</para>
        /// <param name="rt">被创建的对象<para>The object created</para></param>
        /// </summary>
        void OnCreate(RectTransform rt);

        /// <summary>
        /// 填充第 dataIndex 条数据，自动测量并锁定尺寸
        /// <para>Fill data, auto-measure, lock size.</para>
        /// <param name="dataIndex">数据索引<para>Data Index</para></param>
        /// <returns>返回实测高度<para>Returns measured height.</para></returns>
        /// </summary>
        float OnShow(int dataIndex);

        /// <summary>
        /// item 离开可视区被回收隐藏
        /// <para>Called when item leaves visible area and is recycled.</para>
        /// </summary>
        void OnHide();

        /// <summary>
        /// item 被销毁清理
        /// <para>Called when item is destroyed for cleanup.</para>
        /// </summary>
        void OnDestroyed();
    }
}
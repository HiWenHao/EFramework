/*
 * ================================================
 * Describe:        滚动列表 item 契约（生命周期 + 按需测量填充）
 * Author:          Alvin8412
 * CreationTime:    2026-06-04 14:48:05
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-26 16:13:00
 * ScriptVersion:   0.3
 * ================================================
 */

using UnityEngine;

namespace EasyFramework.Ui
{
    /// <summary>
    /// 滚动列表 item 的完整契约：生命周期 + 内容填充/尺寸测量。
    /// <para>Full scroll item contract: lifecycle + content fill and size measuring.</para>
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
        /// </summary>
        /// <param name="rt">被创建的对象<para>The object created</para></param>
        void OnCreate(RectTransform rt);

        /// <summary>
        /// 填充第 dataIndex 条数据。measure=true 时执行布局测量并锁定尺寸、返回实测值；
        /// measure=false 时只填充内容、返回 0（尺寸由列表的 OnGetItemSize 估算值锁定）。
        /// <para>Fill data at dataIndex. With measure=true, rebuild layout, lock size and return the
        /// measured size; with measure=false, fill only and return 0 (size comes from the list's estimate).</para>
        /// </summary>
        /// <param name="dataIndex">数据索引<para>Data index</para></param>
        /// <param name="measure">是否测量并锁定尺寸<para>Whether to measure and lock size</para></param>
        /// <returns>实测尺寸（measure=true 时有效）<para>Measured size (valid when measure=true)</para></returns>
        float OnShow(int dataIndex, bool measure);

        /// <summary>
        /// item 离开可视区被回收隐藏
        /// <para>Called when the item leaves the visible area and is recycled.</para>
        /// </summary>
        void OnHide();

        /// <summary>
        /// item 被销毁清理
        /// <para>Called when the item is destroyed for cleanup.</para>
        /// </summary>
        void OnDestroyed();
    }
}
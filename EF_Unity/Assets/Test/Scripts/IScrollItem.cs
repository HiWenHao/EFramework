// ================================================================
// IScrollItem.cs
// 无限不规则滚动列表的 item 接口
// ================================================================

using UnityEngine;

namespace EFExample
{
    /// <summary>
    /// 无限滚动列表的 item 必须实现此接口。
    /// item 的生命周期由 InfiniteIrregularScrollList 管理：
    ///   Instantiate → OnCreate → (OnShow...OnHide) × N → OnDestroyed
    /// </summary>
    public interface IScrollItem
    {
        /// <summary>首次被创建时调用（仅一次），做一次性初始化引用</summary>
        void OnCreate(RectTransform rt);

        /// <summary>填充第 dataIndex 条数据，自动测量并锁定尺寸。返回实测高度。</summary>
        float OnShow(int dataIndex);

        /// <summary>item 离开可视区被回收隐藏</summary>
        void OnHide();

        /// <summary>item 被销毁清理</summary>
        void OnDestroyed();

        /// <summary>缓存的测量尺寸（由 OnShow 设置，列表读取）</summary>
        float MeasuredSize { get; set; }
    }
}

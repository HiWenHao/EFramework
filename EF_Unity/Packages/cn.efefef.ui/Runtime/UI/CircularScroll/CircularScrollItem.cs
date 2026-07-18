/*
 * ================================================
 * Describe:        循环滚动列表 - 列表项基类
 * Author:          Alvin8412
 * CreationTime:    2026-06-18 16:42:21
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-06-18 16:42:21
 * ScriptVersion:   0.1
 * ================================================
 */

using UnityEngine;
using EasyFramework.Managers.Pool;

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// 循环滚动列表项基类 —— 挂载到列表项 Prefab 上
    /// <para>Base class for circular scrolling list items — attach to the item Prefab</para>
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CircularScrollItem : MonoBehaviour, ICircularScrollItem, IPoolable
    {
        private RectTransform _rectTransform;

        /// <summary>
        /// 当前绑定的数据索引
        /// <para>Currently bound data index</para>
        /// </summary>
        public int DataIndex { get; set; } = -1;

        /// <summary>
        /// 缓存的 RectTransform
        /// <para>Cached RectTransform reference</para>
        /// </summary>
        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                    _rectTransform = transform as RectTransform;
                return _rectTransform;
            }
        }

        /// <summary>
        /// 是否来自对象池（由 PooledObject 组件同步）
        /// <para>Whether this item originates from a pool (synced by PooledObject component)</para>
        /// </summary>
        public bool IsFromPool { get; set; }

        /// <summary>
        /// 从池中取出时由 PoolManager 调用
        /// <para>Called by PoolManager when retrieved from the pool</para>
        /// </summary>
        public virtual void OnSpawn() { }

        /// <summary>
        /// 放回池中时由 PoolManager 调用，委托到 OnRecycle
        /// <para>Called by PoolManager when returned to the pool, delegates to OnRecycle</para>
        /// </summary>
        public virtual void OnDespawn()
        {
            OnRecycle();
        }

        /// <summary>
        /// 当 Item 被分配数据时由列表控制器调用<br/>
        /// 子类重写此方法绑定业务数据
        /// <para>Called by the list controller when the Item is assigned data<br/>
        /// Override this method in subclasses to bind business data</para>
        /// </summary>
        /// <param name="dataIndex">数据索引</param>
        public virtual void OnSetup(int dataIndex)
        {
            DataIndex = dataIndex;
        }

        /// <summary>
        /// 当 Item 被回收到对象池时调用<br/>
        /// 子类重写此方法清理业务状态（移除事件监听等）
        /// <para>Called when the Item is recycled back to the pool<br/>
        /// Override this method in subclasses to clean up business state (remove event listeners, etc.)</para>
        /// </summary>
        public virtual void OnRecycle()
        {
            DataIndex = -1;
        }
    }
}

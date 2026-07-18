/*
 * ================================================
 * Describe:        IScrollProItem 的默认实现, 可直接挂载到 prefab 上使用，子类重写 OnShowContent 即可。
 * Author:          Alvin8412
 * CreationTime:    2026-06-04 14:53:23
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-06-04 14:53:23
 * ScriptVersion:   0.1
 * ================================================
 */

using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// IScrollProItem 的默认实现，可直接挂载到 prefab 上使用。
    /// <para>Default IScrollProItem implementation.</para>
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ScrollProItemBase : MonoBehaviour, IScrollProItem
    {
        private bool _created;          // 是否已完成 OnCreate
        private RectTransform _rt;      // 缓存 RectTransform
        private ContentSizeFitter _csf; // 缓存 ContentSizeFitter 

        float IScrollProItem.MeasuredSize { get; set; }

        void IScrollProItem.OnCreate(RectTransform rt)
        {
            if (_created) return;
            _rt = rt;
            _csf = GetComponent<ContentSizeFitter>();
            _created = true;
            OnCreate();
        }

        float IScrollProItem.OnShow(int dataIndex)
        {
            PrepareContent(dataIndex);
            return ForceMeasureAndLock();
        }

        /// <summary>
        /// 仅填充内容，不测量, 用于滚动中的快速路径
        /// <para>Fill content only, no layout rebuild. Fast path during scroll.</para>
        /// <param name="dataIndex">数据索引<para>Data Index</para></param>
        /// </summary>
        public void PrepareContent(int dataIndex)
        {
            if (_csf != null) _csf.enabled = true;
            OnShowContent(dataIndex);
            _rt.sizeDelta = Vector2.zero;
        }

        /// <summary>
        /// 执行双 ForceRebuildLayoutImmediate 并锁定尺寸
        /// <para>Force-rebuild layout ×2 and lock size.</para>
        /// <returns>返回实测高度<para>Returns measured height.</para></returns>
        /// </summary>
        public float ForceMeasureAndLock()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rt);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rt);
            float measured = _rt.rect.height;
            measured = Mathf.Max(1f, measured);
            _rt.sizeDelta = new Vector2(_rt.sizeDelta.x, measured);
            if (_csf != null) _csf.enabled = false;
            ((IScrollProItem)this).MeasuredSize = measured;
            return measured;
        }

        void IScrollProItem.OnHide()
        {
            OnHide();
        }

        void IScrollProItem.OnDestroyed()
        {
            OnDestroyed();
        }

        /// <summary>
        /// 首次创建时调用一次
        /// <para>Called once on creation.</para>
        /// </summary>
        protected virtual void OnCreate()
        {
        }

        /// <summary>
        /// 填充第 index 条数据
        /// <para>Fill data for index.</para>
        /// <param name="dataIndex">数据索引<para>Data Index</para></param>
        /// </summary>
        protected virtual void OnShowContent(int dataIndex)
        {
        }

        /// <summary>
        /// 回收隐藏
        /// <para>Recycled and hidden.</para>
        /// </summary>
        protected virtual void OnHide()
        {
        }

        /// <summary>
        /// 销毁清理
        /// <para>Destroy cleanup.</para>
        /// </summary>
        protected virtual void OnDestroyed()
        {
        }
    }
}
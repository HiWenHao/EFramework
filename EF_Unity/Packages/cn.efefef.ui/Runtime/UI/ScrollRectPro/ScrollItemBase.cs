/*
 * ================================================
 * Describe:        IScrollItem 的默认实现, 可直接挂载到 item prefab 上使用，子类重写 OnShowContent 即可。
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
    /// IScrollItem 的默认实现，可直接挂载到 item prefab 上使用。
    /// <para>Default IScrollItem implementation.</para>
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ScrollItemBase : MonoBehaviour, IScrollItem
    {
        private bool _created;          // 是否已完成 OnCreate
        private RectTransform _rt;      // 缓存 RectTransform
        private ContentSizeFitter _csf; // 缓存 ContentSizeFitter 

        float IScrollItem.MeasuredSize { get; set; }

        /// <summary>
        /// 首次创建时调用一次
        /// <para>Called once on first creation</para>
        /// </summary>
        void IScrollItem.OnCreate(RectTransform rt)
        {
            if (_created) return;
            _rt = rt;
            _csf = GetComponent<ContentSizeFitter>();
            _created = true;
            OnCreate();
        }

        /// <summary>
        /// 填充数据 + 测量 + 锁定
        /// <para>Fill data + measure + lock.</para>
        /// <param name="dataIndex">数据索引<para>Data Index</para></param>
        /// <returns>返回实测高度<para>Returns measured height.</para></returns>
        /// </summary>
        float IScrollItem.OnShow(int dataIndex)
        {
            PrepareContent(dataIndex);
            return ForceMeasureAndLock();
        }

        /// <summary>
        /// 仅填充内容，不测量
        /// <para>Fill content only, no layout rebuild. 用于滚动中的快速路径 / Fast path during scroll.</para>
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
            ((IScrollItem)this).MeasuredSize = measured;
            return measured;
        }

        // 隐藏 / Hide
        void IScrollItem.OnHide()
        {
            OnHide();
        }

        // 销毁清理 / Destroy cleanup
        void IScrollItem.OnDestroyed()
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
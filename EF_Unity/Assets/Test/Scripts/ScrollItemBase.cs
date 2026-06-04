// ================================================================
// ScrollItemBase.cs
// IScrollItem 的默认实现，可直接挂载到 item prefab 上使用。
// 子类重写 OnShowContent 来填充自定义内容。
// ================================================================

using UnityEngine;
using UnityEngine.UI;

namespace EFExample
{
    /// <summary>
    /// IScrollItem 的默认实现。
    /// OnShow 自动完成：填充内容 → 开 CSF → 双 ForceRebuildLayoutImmediate → 锁定 → 关 CSF。
    /// 子类重写 OnShowContent(int dataIndex) 即可。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ScrollItemBase : MonoBehaviour, IScrollItem
    {
        private RectTransform       _rt;
        private ContentSizeFitter   _csf;
        private bool                _created;

        float IScrollItem.MeasuredSize { get; set; }

        void IScrollItem.OnCreate(RectTransform rt)
        {
            if (_created) return;
            _rt   = rt;
            _csf  = GetComponent<ContentSizeFitter>();
            _created = true;
            OnCreate();
        }

        float IScrollItem.OnShow(int dataIndex)
        {
            PrepareContent(dataIndex);
            return ForceMeasureAndLock();
        }

        /// <summary>仅填充内容，不测量（用于滚动中快速路径，测量延后异步）</summary>
        public void PrepareContent(int dataIndex)
        {
            if (_csf != null) _csf.enabled = true;
            OnShowContent(dataIndex);
            _rt.sizeDelta = Vector2.zero;
        }

        /// <summary>执行昂贵的 ForceRebuildLayoutImmediate 并锁定尺寸，返回实测高度</summary>
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

        void IScrollItem.OnHide()
        {
            OnHide();
        }

        void IScrollItem.OnDestroyed()
        {
            OnDestroyed();
        }

        // ================================================================
        // 子类可重写
        // ================================================================

        /// <summary>首次创建时调用一次（初始化引用等）</summary>
        protected virtual void OnCreate() { }

        /// <summary>填充第 dataIndex 条数据</summary>
        protected virtual void OnShowContent(int dataIndex) { }

        /// <summary>回收隐藏</summary>
        protected virtual void OnHide() { }

        /// <summary>销毁清理</summary>
        protected virtual void OnDestroyed() { }
    }
}

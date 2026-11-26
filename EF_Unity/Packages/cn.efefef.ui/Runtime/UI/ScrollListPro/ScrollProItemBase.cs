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

namespace EasyFramework.Ui
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

        float IScrollProItem.OnShow(int dataIndex, bool measure)
        {
            if (!measure)
            {
                // 仅填充：不启用 CSF、不动 sizeDelta、不测量，尺寸由列表估算值锁定
                OnShowContent(dataIndex);
                return 0f;
            }

            // 测量路径：启用 CSF + 清零滚动轴，让 preferred size 生效
            if (_csf != null) _csf.enabled = true;
            OnShowContent(dataIndex);
            _rt.sizeDelta = Vector2.zero;
            return ForceMeasureAndLock();
        }

        /// <summary>
        /// 执行双 ForceRebuildLayoutImmediate 并锁定尺寸（按滚动方向测量对应轴）
        /// <para>Force-rebuild layout ×2 and lock size along the scroll axis.</para>
        /// <returns>返回滚动轴实测尺寸<para>Returns measured size along the scroll axis.</para></returns>
        /// </summary>
        public float ForceMeasureAndLock()
        {
            // 列表在 FillAndMeasureNew 中先设锚点再调 OnShow：
            // 垂直锚点 X 拉伸 (0,1)-(1,1)，水平锚点 X 相等 (0,0)-(0,1)。据此推断方向。
            bool horizontal = Mathf.Approximately(_rt.anchorMin.x, _rt.anchorMax.x);

            // 按方向设置 CSF 适配轴：横向按宽度、纵向按高度
            if (_csf != null)
            {
                _csf.horizontalFit = horizontal
                    ? ContentSizeFitter.FitMode.PreferredSize
                    : ContentSizeFitter.FitMode.Unconstrained;
                _csf.verticalFit = horizontal
                    ? ContentSizeFitter.FitMode.Unconstrained
                    : ContentSizeFitter.FitMode.PreferredSize;
                // 清空滚动轴让 CSF 用 preferred size 重算；拉伸轴保持 0
                _rt.sizeDelta = horizontal ? new Vector2(0f, 0f) : new Vector2(_rt.sizeDelta.x, 0f);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_rt);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rt);

            float measured = horizontal ? _rt.rect.width : _rt.rect.height;
            measured = Mathf.Max(1f, measured);
            // 只锁滚动轴：横向写 X（Y 是拉伸轴保持 0），纵向写 Y
            _rt.sizeDelta = horizontal
                ? new Vector2(measured, 0f)
                : new Vector2(_rt.sizeDelta.x, measured);
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
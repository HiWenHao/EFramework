/*
 * ================================================
 * Describe:        ScrollListPro 分部类 — 布局计算、累积位置与可见范围
 * Author:          Alvin8412
 * CreationTime:    2026-11-26 15:40:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-26 15:40:00
 * ScriptVersion:   0.1
 * ================================================
 */

using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.Ui
{
    public partial class ScrollListPro
    {
        // 确保必要引用全都存在
        private void EnsureReferences()
        {
            if (scrollRect == null)
                scrollRect = GetComponent<ScrollRect>();

            _contentRect = scrollRect.content;
            if (_contentRect == null)
            {
                D.Error("[ UiManager ] [ ScrollListPro ] ScrollRect.Content is null.");
                return;
            }

            _viewportRect = scrollRect.viewport != null
                ? scrollRect.viewport
                : scrollRect.GetComponent<RectTransform>();

            _viewportSize = GetViewportSize();

            if (itemPrefab != null)
            {
                var pfRt = itemPrefab.transform as RectTransform;
                if (pfRt != null)
                {
                    _prefabDefaultSize = (direction == Direction.Vertical)
                        ? pfRt.rect.height
                        : pfRt.rect.width;
                }
            }

            StripConflictingLayoutComponents(_contentRect);
        }

        // 清空全部
        private void ClearAll()
        {
            // 先取消在飞动画并复位瞬时状态：否则动画下一帧仍写 _itemSizes[index]，
            // 而数组即将被清空 → ArgumentOutOfRangeException。
            CancelAllAnimations();
            _animating = false;
            _wasMoving = false;
            _batchDirty = false;

            // 销毁活跃 item（Despawn 归还池，池会自动管理生命周期）
            // 框架未运行时池不存在，仍通知 item 自身清理，但跳过 Despawn
            bool hasPool = TryGetPool(out var pool);
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                if (_activeItems[i].Target == null) continue;
                _activeItems[i].ScrollItem?.OnDestroyed();
                if (hasPool)
                    pool.Despawn(_activeItems[i].Target);
            }

            _activeItems.Clear();
            _activeIndexMap.Clear();

            DestroyPool();

            _onCreateCalled.Clear();
            _itemSizes.Clear();
            _cumulativePositions.Clear();
            FirstVisibleIndex = -1;
            LastVisibleIndex = -1;
        }

        // 移除 content 上的 LayoutGroup、ContentSizeFitter 和 AspectRatioFitter。
        // 本组件完全手动管理 content 的布局，任何自动布局组件都会产生冲突。
        private static void StripConflictingLayoutComponents(RectTransform content)
        {
            bool stripped = false;

            var csf = content.GetComponent<ContentSizeFitter>();
            if (csf != null)
            {
                csf.enabled = false;
                Destroy(csf);
                stripped = true;
            }

            var layout = content.GetComponent<LayoutGroup>();
            if (layout != null)
            {
                layout.enabled = false;
                Destroy(layout);
                stripped = true;
            }

            var arf = content.GetComponent<AspectRatioFitter>();
            if (arf != null)
            {
                arf.enabled = false;
                Destroy(arf);
                stripped = true;
            }

            if (stripped)
                D.Warning(
                    "[ UiManager ] [ ScrollListPro ] Content 上的 LayoutGroup/ContentSizeFitter/AspectRatioFitter 已自动移除。" +
                    "本组件手动管理 content 布局，这些组件会导致定位异常。");
        }

        // 获取元素尺寸
        private float GetItemSize(int index)
        {
            return OnGetItemSize != null ? Mathf.Max(1f, OnGetItemSize(index)) : _prefabDefaultSize;
        }

        // 计算最大滚动偏移 
        private float GetMaxScrollOffset()
        {
            float totalSize = CalculateTotalSize();
            return Mathf.Max(0, totalSize - _viewportSize);
        }

        // 结构变更（插入/删除）在视口上方时，后续 item 的累积位置会整体平移，
        // 必须同步补偿滚动偏移，否则画面会跳一格。操作点在视口内/下方时无需补偿。
        // 数据已变更、RebuildCumulativePositions 之后调用。
        private void CompensateOffsetForStructuralChange(int index, float cumDelta)
        {
            if (!IsInitialized || Mathf.Approximately(cumDelta, 0f)) return;
            // 操作点位于当前视口起点之前（含正好在起点）才需要补偿
            int anchor = FirstVisibleIndex >= 0 ? FirstVisibleIndex : 0;
            if (index > anchor) return;

            float newOffset = Mathf.Clamp(GetContentScrollOffset() + cumDelta, 0f, GetMaxScrollOffset());
            SetContentScrollOffset(newOffset);
        }

        // 全量重建 
        private void FullRebuild()
        {
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                // 与其它回收路径（RefreshVisibleItemsInternal）保持一致：回收前通知业务隐藏，
                // 避免对象池复用后 item 的事件监听/动画等业务状态残留
                _activeItems[i].ScrollItem?.OnHide();
                Recycle(_activeItems[i].Target);
            }

            _activeItems.Clear();
            _activeIndexMap.Clear();
            FirstVisibleIndex = -1;
            LastVisibleIndex = -1;

            RebuildCumulativePositions(); // 确保 ScrollToIndex/ScrollToBottom 能读到最新位置
            RefreshVisibleItems(true);
        }

        // Content 布局设置 ================================================================

        // 应用content布局
        private void ApplyContentLayout(bool preserveScroll = true)
        {
            float total = CalculateTotalSize();

            // 保存当前滚动偏移，避免 sizeDelta 变更时 ScrollRect 自动钳位丢失位置
            float savedOffset = preserveScroll ? GetContentScrollOffset() : 0f;

            if (direction == Direction.Vertical)
            {
                _contentRect.anchorMin = new Vector2(0, 1);
                _contentRect.anchorMax = new Vector2(1, 1);
                _contentRect.pivot = new Vector2(0.5f, 1);
                _contentRect.sizeDelta = new Vector2(_contentRect.sizeDelta.x, total);

                // 恢复滚动位置（钳位到新的有效范围）
                if (preserveScroll)
                {
                    float maxOff = Mathf.Max(0, total - _viewportSize);
                    _contentRect.anchoredPosition = new Vector2(
                        _contentRect.anchoredPosition.x,
                        Mathf.Clamp(savedOffset, 0, maxOff));
                }
                else
                {
                    _contentRect.anchoredPosition = Vector2.zero;
                }
            }
            else
            {
                // 水平方向：锚点与 pivot 都取顶边对齐（与垂直分支同理），
                // 否则 anchor=(0,1)+pivot=(0,0.5) 会把 content 中点钉在视窗顶边，
                // 上半部分跑到视窗外；sizeDelta.y=0 时 item 垂直拉伸为 0 高而不可见。
                _contentRect.anchorMin = new Vector2(0, 1);
                _contentRect.anchorMax = new Vector2(0, 1);
                _contentRect.pivot = new Vector2(0, 1);
                _contentRect.sizeDelta = new Vector2(total, _viewportSize);

                if (preserveScroll)
                {
                    float maxOff = Mathf.Max(0, total - _viewportSize);
                    _contentRect.anchoredPosition = new Vector2(
                        -Mathf.Clamp(savedOffset, 0, maxOff),
                        0f);
                }
                else
                {
                    _contentRect.anchoredPosition = Vector2.zero;
                }
            }
        }

        // 计算总尺寸
        private float CalculateTotalSize()
        {
            float total = 0f;
            for (int i = 0; i < _itemSizes.Count; i++)
            {
                float s = _itemSizes[i];
                // 未测量过的 item 用 OnGetItemSize 估算，避免 total = 0 导致滚动范围错误
                if (s <= UnmeasuredSizeThreshold) s = GetItemSize(i);
                total += s;
                if (i < _itemSizes.Count - 1) total += itemSpacing;
            }

            return total;
        }

        // 计算索引从 0 到index的累积偏移（含间距），未初始化时回退到线性计算
        private float GetCumulativePosition(int index)
        {
            if (index <= 0) return 0f;
            if (index < _cumulativePositions.Count)
                return _cumulativePositions[index];
            // 防御：_itemSizes 新增但 _cumulativePositions 尚未重建时，回退到线性计算
            float pos = 0f;
            for (int i = 0; i < index && i < _itemSizes.Count; i++)
                pos += _itemSizes[i] + itemSpacing;
            return pos;
        }

        // 重建累积位置数组 _itemSizes 变更后调用，fromIndex=0 表示全量重建。
        private void RebuildCumulativePositions(int fromIndex = 0)
        {
            if (_cumulativePositions.Count != _itemSizes.Count)
            {
                _cumulativePositions.Clear();
                for (int i = 0; i < _itemSizes.Count; i++)
                    _cumulativePositions.Add(0f);
            }

            if (_cumulativePositions.Count == 0) return;

            if (fromIndex <= 0)
            {
                _cumulativePositions[0] = 0f;
                fromIndex = 1;
            }

            for (int i = fromIndex; i < _cumulativePositions.Count; i++)
            {
                float prevSize = _itemSizes[i - 1];
                if (prevSize <= UnmeasuredSizeThreshold) prevSize = GetItemSize(i - 1);
                _cumulativePositions[i] = _cumulativePositions[i - 1] + prevSize + itemSpacing;
            }
        }

        // 二分查找第一个可见 item 索引。返回第一个结束位置 > scrollOffset 的索引。
        private int BinarySearchVisibleIndex(float scrollOffset)
        {
            // 上界取两数组较小值，防止批处理期间长度失配时 _itemSizes[mid] 越界
            int hi = Mathf.Min(_cumulativePositions.Count, _itemSizes.Count) - 1;
            if (hi < 0) return 0;
            int lo = 0;
            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                float itemEnd = _cumulativePositions[mid] + _itemSizes[mid] + itemSpacing;
                if (itemEnd <= scrollOffset)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }

            return Mathf.Min(lo, _itemSizes.Count - 1);
        }

        // 可视区域计算 ================================================================

        // 获取视窗尺寸
        private float GetViewportSize()
        {
            if (_viewportRect == null) return 0f;
            return direction == Direction.Vertical
                ? _viewportRect.rect.height
                : _viewportRect.rect.width;
        }

        // 获取内容滚动偏移
        private float GetContentScrollOffset()
        {
            return direction == Direction.Vertical
                ? _contentRect.anchoredPosition.y
                : -_contentRect.anchoredPosition.x;
        }

        // 计算可见范围
        private (int firstIndex, int lastIndex) CalculateVisibleRange()
        {
            int count = _itemSizes.Count;
            if (count == 0) return (-1, -1);

            _viewportSize = GetViewportSize();

            float rawOffset = GetContentScrollOffset();
            float maxScroll = Mathf.Max(0, CalculateTotalSize() - _viewportSize);
            float scrollOffset = Mathf.Clamp(rawOffset, 0, maxScroll);

            int first = BinarySearchVisibleIndex(scrollOffset);
            int totalBuffer = bufferCount + preCreateBuffer;
            first = Mathf.Max(0, first - totalBuffer);

            float visibleEnd = scrollOffset + _viewportSize;
            int last = first;
            // 上界取两数组较小值，防止插入操作时 cum 短于 sizes 导致越界
            int scanLimit = Mathf.Min(count, _cumulativePositions.Count);
            for (int i = first; i < scanLimit; i++)
            {
                float itemEnd = _cumulativePositions[i] + _itemSizes[i] + itemSpacing;
                last = i;
                if (itemEnd >= visibleEnd) break;
            }

            last = Mathf.Min(count - 1, last + totalBuffer);
            return (first, last);
        }
    }
}
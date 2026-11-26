/*
 * ================================================
 * Describe:        ScrollListPro 分部类 — 可见 item 编排、填充与定位
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
        // 刷新可见 item
        private void RefreshVisibleItemsInternal(bool force)
        {
            if (!IsInitialized || _itemSizes.Count == 0) return;

            // 无条件同步 content 尺寸：AppendData 追加少量 item 时可见范围不变，
            // 若放在 autoRebuildLayout 分支或提前 return 之后，滚动范围会停留在旧值，新 item 永远滚不到。
            SyncContentSizeDelta();

            var (newFirst, newLast) = CalculateVisibleRange();
            if (newFirst < 0) return;

            // force 或 范围变化 才重建
            if (!force && newFirst == FirstVisibleIndex && newLast == LastVisibleIndex)
                return;

            // 回收不再可见的 item
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                var item = _activeItems[i];
                if (item.Index >= newFirst && item.Index <= newLast) continue;
                _activeIndexMap.Remove(item.Index);
                OnItemVisibilityChanged?.Invoke(item.Index, false);
                item.ScrollItem?.OnHide();
                Recycle(item.Target);
                _activeItems.RemoveAt(i);
            }

            // 为缺失索引补齐 item
            for (int i = newFirst; i <= newLast; i++)
            {
                if (_activeIndexMap.ContainsKey(i)) continue;
                var go = Rent();
                if (go == null) continue; // 框架未运行/池不可用：跳过该索引，待下次刷新补齐
                go.SetActive(false);
                _activeItems.Add(new ActiveItem
                {
                    Target = go,
                    ScrollItem = go.GetComponent<IScrollProItem>(),
                    Index = i, Size = _itemSizes[i], JustCreated = true
                });
                _activeIndexMap[i] = go;
                OnItemVisibilityChanged?.Invoke(i, true);
            }

            // 保存锚点, 在填充前记录，填充后 justCreated 全被清除无法区分
            int anchorIdx = -1;
            foreach (var ai in _activeItems)
            {
                if (ai.Target != null && !ai.JustCreated && ai.Index >= newFirst && ai.Index <= newLast)
                {
                    anchorIdx = ai.Index;
                    break;
                }
            }

            if (anchorIdx < 0) anchorIdx = newFirst;
            float oldAnchorPos = anchorIdx < _itemSizes.Count ? GetCumulativePosition(anchorIdx) : 0f;

            // 在填充之前禁用 content 布局, 防冒泡篡改 item 位置
            if (autoRebuildLayout)
            {
                var contentLayout = _contentRect.GetComponent<LayoutGroup>();
                if (contentLayout != null) contentLayout.enabled = false;
                var contentCsf = _contentRect.GetComponent<ContentSizeFitter>();
                if (contentCsf != null) contentCsf.enabled = false;
                var contentArf = _contentRect.GetComponent<AspectRatioFitter>();
                if (contentArf != null) contentArf.enabled = false;
            }

            // 填充新 item, FillAndMeasureNew 内完成测量、锁定
            bool anyFilled = false;
            for (int i = 0; i < _activeItems.Count; i++)
            {
                var ai = _activeItems[i];
                if (!ai.JustCreated) continue;
                _activeItems[i] = FillAndMeasureNew(ai);
                anyFilled = true;
            }

            // Batch flush + 重建累积位置
            if (anyFilled)
            {
                Canvas.ForceUpdateCanvases();
                RebuildCumulativePositions();
            }

            // 补偿顶部尺寸变化导致的视觉偏移
            if (anyFilled && anchorIdx < _itemSizes.Count)
            {
                float delta = GetCumulativePosition(anchorIdx) - oldAnchorPos;
                if (Mathf.Abs(delta) > PositionEpsilon)
                {
                    if (direction == Direction.Vertical)
                        _contentRect.anchoredPosition = new Vector2(_contentRect.anchoredPosition.x,
                            _contentRect.anchoredPosition.y + delta);
                    else
                        _contentRect.anchoredPosition = new Vector2(_contentRect.anchoredPosition.x - delta,
                            _contentRect.anchoredPosition.y);
                }
            }

            // 验证 content sizeDelta
            if (anyFilled && autoRebuildLayout)
            {
                float expectedTotal = CalculateTotalSize();
                if (direction == Direction.Vertical)
                {
                    if (Mathf.Abs(_contentRect.sizeDelta.y - expectedTotal) > PositionEpsilon)
                        _contentRect.sizeDelta = new Vector2(_contentRect.sizeDelta.x, expectedTotal);
                }
                else
                {
                    if (Mathf.Abs(_contentRect.sizeDelta.x - expectedTotal) > PositionEpsilon)
                        _contentRect.sizeDelta = new Vector2(expectedTotal, _contentRect.sizeDelta.y);
                }
            }

            for (int i = newFirst; i <= newLast; i++)
            {
                var go = FindActiveByIndex(i);
                if (go == null) continue;
                var rt = go.transform as RectTransform;
                if (rt == null) continue;
                float pos = _cumulativePositions[i];
                rt.anchoredPosition = (direction == Direction.Vertical)
                    ? new Vector2(0, -pos)
                    : new Vector2(pos, 0);
            }

            FirstVisibleIndex = newFirst;
            LastVisibleIndex = newLast;

            // 定位后再次同步 content 尺寸, 以防 ScrollRect 篡改
            SyncContentSizeDelta();

            // 最终保护：flush + 强制修正漂移
            // 双 flush 确保 ForceRebuildLayoutImmediate 冒泡 dirty 在同一帧全部结算。
            if (!anyFilled) return;
            Canvas.ForceUpdateCanvases();
            Canvas.ForceUpdateCanvases();

            // 强制修正所有 item 位置
            for (int i = newFirst; i <= newLast; i++)
            {
                var go = FindActiveByIndex(i);
                if (go == null) continue;
                var rt = go.transform as RectTransform;
                if (rt == null) continue;
                float expectedPos = GetCumulativePosition(i);
                float actualPos = (direction == Direction.Vertical)
                    ? -rt.anchoredPosition.y
                    : rt.anchoredPosition.x;
                if (Mathf.Abs(actualPos - expectedPos) > PositionEpsilon)
                {
                    rt.anchoredPosition = direction == Direction.Vertical
                        ? new Vector2(rt.anchoredPosition.x, -expectedPos)
                        : new Vector2(expectedPos, rt.anchoredPosition.y);
                }
            }

            // 最终 content sizeDelta 校验
            float expectedTotalValue = CalculateTotalSize();
            if (direction == Direction.Vertical)
            {
                if (Mathf.Abs(_contentRect.sizeDelta.y - expectedTotalValue) > PositionEpsilon)
                    _contentRect.sizeDelta = new Vector2(_contentRect.sizeDelta.x, expectedTotalValue);
            }
            else
            {
                if (Mathf.Abs(_contentRect.sizeDelta.x - expectedTotalValue) > PositionEpsilon)
                    _contentRect.sizeDelta = new Vector2(expectedTotalValue, _contentRect.sizeDelta.y);
            }
        }

        // 查找指定索引的 item
        private GameObject FindActiveByIndex(int index)
        {
            _activeIndexMap.TryGetValue(index, out var go);
            return go;
        }

        // 仅对新创建的 item 填充内容 + 测量布局。不设置位置，由统一的锚点链 pass 处理
        private ActiveItem FillAndMeasureNew(ActiveItem ai)
        {
            var go = ai.Target;
            int index = ai.Index;
            var rt = go.transform as RectTransform;
            if (rt == null)
            {
                ai.JustCreated = false;
                return ai;
            }

            // 设置锚点
            if (direction == Direction.Vertical)
            {
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
            }
            else
            {
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 0.5f);
            }

            // 把新 item 放在内容底部，锚点链定位前绝对不可见。
            float offScreenY = -CalculateTotalSize() - _viewportSize;
            rt.anchoredPosition = (direction == Direction.Vertical)
                ? new Vector2(0, offScreenY)
                : new Vector2(offScreenY, 0);

            go.name = $"Item[{index}]";

            // OnCreate 已在 Rent() 中调用，此处不重复
            // 单一 OnShow(index, measure)：item 只负责填充，measure 决定是否测量锁定尺寸。
            if (ai.ScrollItem == null)
            {
                D.Error("[ UiManager ] [ ScrollListPro ] ItemPrefab '" + itemPrefab.name +
                        "' has no IScrollProItem component. Item must implement IScrollProItem.");
                ai.Size = _itemSizes[index];
            }
            else
            {
                go.SetActive(true);
                float measured = ai.ScrollItem.OnShow(index, autoRebuildLayout);
                if (autoRebuildLayout)
                {
                    // 测量路径：item 已锁定尺寸并返回实测值
                    _itemSizes[index] = Mathf.Max(1f, measured);
                    ai.Size = measured;
                }
                else
                {
                    // 估算路径：OnShow(measure=false) 只填充，尺寸用 OnGetItemSize 估算值锁定
                    float estimated = Mathf.Max(1f, _itemSizes[index]);
                    rt.sizeDelta = direction == Direction.Vertical
                        ? new Vector2(rt.sizeDelta.x, estimated)
                        : new Vector2(estimated, rt.sizeDelta.y);
                    ai.Size = estimated;
                }
            }

            ai.JustCreated = false;
            return ai;
        }
    }
}
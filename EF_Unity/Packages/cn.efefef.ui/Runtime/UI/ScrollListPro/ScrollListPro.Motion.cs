/*
 * ================================================
 * Describe:        ScrollListPro 分部类 — 布局与滚动动画
 * Author:          Alvin8412
 * CreationTime:    2026-11-26 15:40:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-26 15:40:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Ui
{
    public partial class ScrollListPro
    {
        // 异步布局动画
        private async UniTaskVoid LayoutAnimationAsync(int index, float from, float to, float duration, CancellationToken ct)
        {
            try
            {
                float elapsed = 0f;
                while (elapsed < duration && !ct.IsCancellationRequested)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    t = Mathf.SmoothStep(0f, 1f, t);
                    if (index < _itemSizes.Count)
                        _itemSizes[index] = Mathf.Max(1f, Mathf.Lerp(from, to, t));
                    RebuildCumulativePositions();
                    RepositionAllActive(); // 轻量重定位，不重建 item
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }

                if (!ct.IsCancellationRequested)
                {
                    if (index < _itemSizes.Count)
                        _itemSizes[index] = Mathf.Max(1f, to);
                    RebuildCumulativePositions();
                    RefreshVisibleItems(true); // 动画结束，全量同步
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                if (_layoutAnimSkipSizeRestore)
                {
                    // 结构变更导致取消：索引已失效，绝不写回尺寸（会串位到别的 item）
                    _layoutAnimSkipSizeRestore = false;
                }
                else if (index < _itemSizes.Count)
                {
                    // 新动画/Clear/OnDisable 打断：回退到起点，避免停在 lerp 中间值
                    _itemSizes[index] = Mathf.Max(1f, from);
                }
            }
            finally
            {
                // 仅当前动画有权清理令牌：新动画可能已在 await 期间接管 _layoutAnimCts，
                // 无条件 Dispose 会误伤新动画并使其首帧抛 ObjectDisposedException。
                if (_layoutAnimCts == null || _layoutAnimCts.Token == ct)
                {
                    _layoutAnimCts?.Dispose();
                    _layoutAnimCts = null;
                }
            }
        }

        // 仅重新定位所有活跃 item + 同步 sizeDelta, 用于动画期间轻量更新
        private void RepositionAllActive()
        {
            SyncContentSizeDelta();
            for (int i = 0; i < _activeItems.Count; i++)
            {
                var ai = _activeItems[i];
                if (ai.Target == null || ai.Index < 0 || ai.Index >= _cumulativePositions.Count) continue;
                var rt = ai.Target.transform as RectTransform;
                if (rt == null) continue;

                // 同步 sizeDelta：动画期间 _itemSizes 在 lerp，GO 的高度必须跟上
                float cachedSize = _itemSizes[ai.Index];
                ai.Size = cachedSize;
                _activeItems[i] = ai;
                rt.sizeDelta = (direction == Direction.Vertical)
                    ? new Vector2(rt.sizeDelta.x, cachedSize)
                    : new Vector2(cachedSize, rt.sizeDelta.y);

                float pos = _cumulativePositions[ai.Index];
                rt.anchoredPosition = (direction == Direction.Vertical)
                    ? new Vector2(0, -pos)
                    : new Vector2(pos, 0);
            }
        }

        // 如果在批处理中则标记脏位，否则立即应用布局+重建
        private void ApplyAndRebuildOrMarkDirty()
        {
            if (_batchDepth > 0)
            {
                _batchDirty = true;
            }
            else
            {
                ApplyContentLayout();
                if (!IsInitialized)
                {
                    IsInitialized = true;
                    _wasAtTop = true;
                    _wasAtBottom = IsAtBottom();
                }

                FullRebuild();
            }
        }

        // 同步内容尺寸
        private void SyncContentSizeDelta()
        {
            float total = CalculateTotalSize();
            _contentRect.sizeDelta = direction == Direction.Vertical
                ? new Vector2(_contentRect.sizeDelta.x, total)
                : new Vector2(total, _contentRect.sizeDelta.y);
        }

        // 设置 content 的 anchoredPosition
        private void SetContentScroll(float clamped)
        {
            _contentRect.anchoredPosition = direction == Direction.Vertical
                ? new Vector2(_contentRect.anchoredPosition.x, clamped)
                : new Vector2(-clamped, _contentRect.anchoredPosition.y);
        }

        // 同步滚动动画
        private async UniTaskVoid AnimateScrollAsync(float from, float to, float duration,
            AnimationCurve curve, CancellationToken ct)
        {
            _animating = true;
            try
            {
                float elapsed = 0f;
                while (elapsed < duration && !ct.IsCancellationRequested)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    if (curve != null && curve.length > 0)
                        t = curve.Evaluate(t);

                    float currentPos = Mathf.Lerp(from, to, t);
                    SetContentScrollOffset(currentPos);

                    // 动画期间 OnScrollChanged 被 _animating 抑制（567），可见 item 不会随滚动补充，
                    // 跨屏滚动时起点区的 item 移出视口后即空白。这里按当前动画位置主动补齐可见项。
                    // 用 force=false：仅在可见范围真正变化时补/回收，不做全量重建；
                    // 刷新内的锚点补偿可能微调 anchoredPosition，随后重钉回动画位置避免抖动。
                    RefreshVisibleItems(false);
                    SetContentScrollOffset(currentPos);

                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }

                if (!ct.IsCancellationRequested)
                {
                    SetContentScrollOffset(to);
                    RefreshVisibleItems(true);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // 本令牌被取消：新滚动/OnDisable 打断，交由 finally 收尾
            }
            finally
            {
                // 仅"当前动画"有权复位状态与清理令牌：
                //  - _scrollAnimCts == null → 已被 CancelAllAnimations 清理，无新动画接管
                //  - Token == ct → 自己仍是当前活动动画
                // 否则已被新动画取代，_animating/CTS 由新动画负责，禁止误清
                // （若旧动画无条件复位 _animating，新动画期间 onValueChanged 不再早退 → 视窗飞走）
                if (_scrollAnimCts == null || _scrollAnimCts.Token == ct)
                {
                    _animating = false;
                    if (_scrollAnimCts != null)
                    {
                        _scrollAnimCts.Dispose();
                        _scrollAnimCts = null;
                    }
                }
            }
        }

        // 设置 content 滚动偏移
        private void SetContentScrollOffset(float offset)
        {
            _contentRect.anchoredPosition = direction == Direction.Vertical
                ? new Vector2(_contentRect.anchoredPosition.x, offset)
                : new Vector2(-offset, _contentRect.anchoredPosition.y);
        }
    }
}
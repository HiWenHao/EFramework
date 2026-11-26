/*
 * ================================================
 * Describe:        ScrollListPro 分部类 — 滚动事件、吸附与边缘检测
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
        // 当滚动位置改变时
        private void OnScrollChanged(Vector2 _)
        {
            // 用户拖动时立即终止正在运行的滚动动画，避免手势与动画叠加造成视窗漂移。
            // 必须用真实拖拽事件标志而非 velocity：动画自身逐帧改 anchoredPosition 也会产生
            // 巨大 velocity（尤其大列表跨屏跳转），旧判断会让动画在首帧被自己的回调 cancel，
            // 表现为"轻微滚动后立刻停止"。_isDragging 只在用户按下并拖动时为 true。
            if (_animating)
            {
                if (_isDragging)
                {
                    _scrollAnimCts?.Cancel(); // 取消滚动动画（而非布局动画），让拖动接管
                    _animating = false; // 立即放行刷新，避免早退导致拖动时 item 不更新
                }
                else
                    return; // 非拖动的动画自驱动回调：跳过刷新，防止累积位置变化干扰动画
            }

            RefreshVisibleItems(false);
            CheckEdgeReached();
            CheckSnap();
        }

        // 检查吸附状态
        private void CheckSnap()
        {
            // 自定义策略优先：即使枚举为 None，注入策略后仍启用吸附
            if (_snapStrategy == null && snapAlignment == SnapAlignment.None) return;
            if (scrollRect == null) return;
            if (_isDragging) return; // 用户拖拽中不启动吸附，避免打断手势

            float vel = direction == Direction.Vertical
                ? Mathf.Abs(scrollRect.velocity.y)
                : Mathf.Abs(scrollRect.velocity.x);

            bool moving = vel > snapVelocityThreshold;

            if (_wasMoving && !moving && !_refreshing)
            {
                _snapDebounceCts?.Cancel();
                var cts = new CancellationTokenSource();
                _snapDebounceCts = cts;
                SnapDebounceAsync(cts.Token).Forget();
            }

            _wasMoving = moving;
        }

        // 吸附防抖
        private async UniTaskVoid SnapDebounceAsync(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(SnapDebounceSeconds), cancellationToken: ct);
                if (!ct.IsCancellationRequested)
                    PerformSnap();
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // 被下一次滑动/new 防抖取消，交由 finally 清理
            }
            finally
            {
                // 仅当前防抖令牌才有权清理：若已被新防抖接管，旧任务不能 Dispose 新 CTS，
                // 否则新防抖的 Delay 会因令牌已释放而失效。与 AnimateScrollAsync 的归属判断一致
                if (_snapDebounceCts != null && _snapDebounceCts.Token == ct)
                {
                    _snapDebounceCts.Dispose();
                    _snapDebounceCts = null;
                }
            }
        }

        // 执行吸附：策略决定 item/视窗的对齐锚点，主类只负责找最近 item 并补偿偏移
        private void PerformSnap()
        {
            if (!IsInitialized || _itemSizes.Count == 0) return;

            // 用户注入的策略优先，未注入时按 snapAlignment 枚举回退内置策略
            ISnapStrategy strategy = _snapStrategy ?? SnapStrategyFactory.Create(snapAlignment);
            if (strategy == null) return;

            float currentOffset = GetContentScrollOffset();
            float viewportSize = GetViewportSize();
            float viewAnchor = strategy.GetViewAnchor(currentOffset, viewportSize);

            // 找到锚点最接近视窗锚点的 item
            int bestIdx = -1;
            float bestDist = float.MaxValue;

            // 两数组在批处理/数据变更期间可能长度失配，钳制到较小值防止 _itemSizes[i] 越界
            int upperBound = Mathf.Min(_cumulativePositions.Count, _itemSizes.Count) - 1;
            for (int i = FirstVisibleIndex; i <= LastVisibleIndex && i <= upperBound; i++)
            {
                if (i < 0) continue;
                float itemAnchor = strategy.GetItemAnchor(_cumulativePositions[i], _itemSizes[i]);
                float dist = Mathf.Abs(itemAnchor - viewAnchor);
                if (!(dist < bestDist)) continue;
                bestDist = dist;
                bestIdx = i;
            }

            if (bestIdx < 0) return;
            // 通用目标偏移：把 item 锚点与视窗锚点的差值补偿到当前偏移上。
            // 等价于旧式 cum + size*alignment - viewport*alignment，但不依赖具体对齐数值。
            float itemAnchor1 = strategy.GetItemAnchor(GetCumulativePosition(bestIdx), _itemSizes[bestIdx]);
            float target = currentOffset + (itemAnchor1 - viewAnchor);
            float total = CalculateTotalSize();
            float maxOff = Mathf.Max(0, total - viewportSize);
            float clamped = Mathf.Clamp(target, 0, maxOff);

            // 关键：清零 ScrollRect 残留惯性，否则吸附动画与惯性逐帧叠加，
            // 表现为"已吸附到位却仍在滑动"。StopMovement 必须在读 currentOffset 之后、
            // 启动动画之前调用，避免读到已变的值。
            scrollRect.StopMovement();
            _wasMoving = false; // 惯性已清，重置移动基线，防止动画结束后 CheckSnap 误判再吸附

            var cts = new CancellationTokenSource();
            _scrollAnimCts?.Cancel();
            _scrollAnimCts?.Dispose();
            _scrollAnimCts = cts;
            AnimateScrollAsync(currentOffset, clamped, snapDuration, null, cts.Token).Forget();
        }

        // 检查是否已到达边缘
        private void CheckEdgeReached()
        {
            bool atTop = IsAtTop();
            bool atBottom = IsAtBottom();

            if (!_wasAtTop && atTop)
                OnReachTop?.Invoke();

            if (!_wasAtBottom && atBottom)
                OnReachBottom?.Invoke();

            _wasAtTop = atTop;
            _wasAtBottom = atBottom;
        }
    }
}
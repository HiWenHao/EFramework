/*
 * ================================================
 * Describe:      HTTP 重试策略
 * Author:        Alvin5100
 * CreationTime:  2026-07-30 17:24:17
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-07-30 17:24:17
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Systems.Http
{
    /// <summary>
    /// HTTP 重试策略：控制失败时的重试次数与退避延迟。
    /// <para>Retry policy controlling retry count and backoff delay on failure.</para>
    /// </summary>
    [Serializable]
    public sealed class HttpRetryPolicy
    {
        /// <summary>
        /// 最大重试次数（0 表示不重试）
        /// <para>Max retry count (0 = no retry).</para>
        /// </summary>
        public int MaxRetries = 0;

        /// <summary> 基础延迟
        /// <para>Base delay in milliseconds.</para>
        /// </summary>
        public int BaseDelayMs = 200;

        /// <summary>
        /// 是否使用指数退避（延迟 = BaseDelayMs * 2^(attempt-1)）
        /// <para>Use exponential backoff (delay = BaseDelayMs * 2^(attempt-1)).</para>
        /// </summary>
        public bool ExponentialBackoff = true;

        /// <summary>
        /// 第 attempt 次重试（从 1 开始）对应的延迟毫秒数
        /// <para>Delay (ms) for the given retry attempt (1-based).</para>
        /// </summary>
        public int DelayMs(int attempt)
        {
            if (!ExponentialBackoff) return BaseDelayMs;
            // 防止指数爆炸：限制单次延迟上限 30s
            long delay = BaseDelayMs * (1L << (attempt - 1));
            return (int)Math.Min(delay, 30_000L);
        }
    }
}
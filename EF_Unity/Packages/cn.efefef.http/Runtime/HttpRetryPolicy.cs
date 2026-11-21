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

namespace EasyFramework.Http
{
    /// <summary>
    /// HTTP 重试策略：控制失败时的重试次数与退避延迟。
    /// <para>Retry policy controlling retry count and backoff delay on failure.</para>
    /// </summary>
    public sealed class HttpRetryPolicy : IHttpRetryPolicy
    {
        /// <summary>
        /// 最大重试次数（0 表示不重试）
        /// <para>Max retry count (0 = no retry).</para>
        /// </summary>
        public int MaxRetries { get; set; } = 0;

        /// <summary>
        /// 基础延迟
        /// <para>Base delay in milliseconds.</para>
        /// </summary>
        public int BaseDelayMs { get; set; } = 200;

        /// <summary>
        /// 是否使用指数退避（延迟 = BaseDelayMs * 2^(attempt-1)）
        /// <para>Use exponential backoff (delay = BaseDelayMs * 2^(attempt-1)).</para>
        /// </summary>
        public bool ExponentialBackoff { get; set; } = true;

        /// <summary>
        /// 第 attempt 次重试（从 1 开始）对应的延迟毫秒数
        /// <para>Delay (ms) for the given retry attempt (1-based).</para>
        /// </summary>
        // 指数退避的移位上限：1L << 30 已远超 30s 延迟上限，再大只会在 long 上溢出反转
        private const int MaxBackoffShift = 30;

        // 单次延迟上限（毫秒）
        private const int MaxDelayMs = 30_000;

        public int DelayMs(int attempt)
        {
            if (!ExponentialBackoff) return BaseDelayMs;
            // 钳制指数再移位：attempt 异常大时避免 long 移位回绕导致延迟骤降
            int shift = Math.Min(Math.Max(attempt - 1, 0), MaxBackoffShift);
            long delay = BaseDelayMs * (1L << shift);
            return (int)Math.Min(delay, MaxDelayMs);
        }
    }
}
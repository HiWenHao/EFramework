/*
 * ================================================
 * Describe:      HTTP 重试策略接口（③ 执行策略）：纯策略，无生命周期；由门面解析后写入上下文供发送器使用。
 * Author:        Alvin8412
 * CreationTime:  2026-11-13 14:40
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-13 14:40
 * ScriptVersion: 1.0
 * ================================================
 */

namespace EasyFramework.Systems.Http
{
    /// <summary>
    /// HTTP 重试策略接口，决定失败时的重试次数与退避延迟
    /// <para>HTTP retry policy deciding retry count and backoff delay on failure</para>
    /// </summary>
    public interface IHttpRetryPolicy
    {
        /// <summary>
        /// 最大重试次数（0 表示不重试）
        /// <para>Max retry count (0 = no retry)</para>
        /// </summary>
        int MaxRetries { get; }

        /// <summary>
        /// 第 attempt 次重试（从 1 开始）对应的延迟毫秒数
        /// <para>Delay (ms) for the given retry attempt (1-based)</para>
        /// </summary>
        /// <param name="attempt">重试序号 <para>Retry attempt</para></param>
        /// <returns>延迟毫秒 <para>Delay in milliseconds</para></returns>
        int DelayMs(int attempt);
    }
}
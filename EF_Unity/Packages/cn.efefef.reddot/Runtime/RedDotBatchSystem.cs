/*
 * ================================================
 * Describe:      This script is used to supports batch modifications and unified refresh.
 * Author:        Alvin5100, AI
 * CreationTime:  2026-05-13 15:12:19
 * ModifyAuthor:  Alvin5100, AI
 * ModifyTime:    2026-11-20
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework.RedDot
{
    /// <summary>
    /// 批处理系统：支持批量修改后统一刷新
    /// <para>Batch system - supports batch modifications and unified refresh</para>
    /// </summary>
    public class RedDotBatchSystem
    {
        private int _batchDepth; // 批处理嵌套深度
        private readonly IRedDotContext _context; // 子系统上下文（可注入）

        /// <summary>
        /// 构造批处理系统
        /// <para>Construct the batch system.</para>
        /// </summary>
        /// <param name="context">子系统上下文（null 则回退门面单例） <para>Context (null falls back to the facade singleton)</para></param>
        public RedDotBatchSystem(IRedDotContext context = null)
        {
            _context = context;
        }

        // 延迟解析上下文
        private IRedDotContext Context => _context ?? RedDotSystem.Instance;

        /// <summary>
        /// 是否处于批处理模式
        /// <para>Whether in batch mode</para>
        /// </summary>
        public bool IsBatching => _batchDepth > 0;

        /// <summary>
        /// 开始批处理
        /// <para>Begin batch processing</para>
        /// </summary>
        public void Begin()
        {
            _batchDepth++;
        }

        /// <summary>
        /// 结束批处理，若深度归零则触发刷新
        /// <para>End batch processing, trigger flush when depth reaches zero</para>
        /// </summary>
        public void End()
        {
            // 未配对的 End：忽略并告警，不再减负、不再误触发 Flush（旧实现钳到 0 后仍会 Flush，掩盖 Begin/End 失衡）
            if (_batchDepth <= 0)
            {
                D.Warning("[ RedDotBatchSystem ] End() called without a matching Begin(). Ignored.");
                return;
            }

            _batchDepth--;
            if (_batchDepth == 0)
                Context.DirtySystem.Flush();
        }
    }
}
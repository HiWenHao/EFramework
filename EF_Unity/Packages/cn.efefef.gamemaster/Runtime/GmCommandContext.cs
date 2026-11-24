/*
 * ================================================
 * Describe:      GM 命令执行上下文：参数、取消标记、日志与可选结果。
 * Author:        Alvin8412
 * CreationTime:  2026-11-24
 * ModifyTime:    2026-11-24
 * Version:       0.1
 * ===============================================
 */

using System.Collections.Generic;
using System.Threading;

namespace EasyFramework.GameMaster
{
    /// <summary>
    /// 单次 GM 命令执行上下文
    /// <para>Per-run context of a GM command.</para>
    /// </summary>
    public sealed class GmCommandContext
    {
        /// <summary> 命令参数（只读字典）。<para>Read-only command arguments.</para> </summary>
        public IReadOnlyDictionary<string, object> Args { get; }

        /// <summary> 本次执行的取消标记。<para>Cancellation token of this execution.</para> </summary>
        public CancellationToken Cancellation { get; }

        /// <summary>
        /// 创建执行上下文。
        /// <para>Creates an execution context.</para>
        /// </summary>
        /// <param name="args">参数字典。<para>Argument dictionary.</para></param>
        /// <param name="cancellation">取消标记。<para>Cancellation token.</para></param>
        public GmCommandContext(IReadOnlyDictionary<string, object> args, CancellationToken cancellation)
        {
            Args = args;
            Cancellation = cancellation;
        }
    }
}
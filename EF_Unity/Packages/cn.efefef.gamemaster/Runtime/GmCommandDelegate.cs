/*
 * ================================================
 * Describe:      GM 命令委托与执行结果。
 * Author:        Alvin8412
 * CreationTime:  2026-11-24
 * ModifyTime:    2026-11-24
 * Version:       0.1
 * ===============================================
 */

using Cysharp.Threading.Tasks;

namespace EasyFramework.GameMaster
{
    /// <summary>
    /// GM 命令执行委托（非泛型，HybridCLR 跨程序集安全）。
    /// <para>GM command delegate (non-generic, safe across HybridCLR assembly boundaries).</para>
    /// </summary>
    /// <param name="context">本次执行的上下文。<para>Context of this execution.</para></param>
    /// <returns>命令异步操作。<para>The command's asynchronous operation.</para></returns>
    public delegate UniTask GmCommandDelegate(GmCommandContext context);

    /// <summary>
    /// GM 命令执行结果。
    /// <para>Result of a GM command execution.</para>
    /// </summary>
    public sealed class GmResult
    {
        /// <summary> 命令 ID。<para>Command ID.</para> </summary>
        public string Id { get; }

        /// <summary> 是否成功。<para>Whether it succeeded.</para> </summary>
        public bool Success { get; }

        /// <summary> 失败或取消原因；成功时为 null。<para>Failure or cancellation reason; null on success.</para> </summary>
        public string Message { get; }

        /// <summary> 执行时刻（UTC）。<para>Execution timestamp in UTC.</para> </summary>
        public System.DateTime TimestampUtc { get; }

        /// <summary> 执行时的参数原始文本。<para>Raw argument text used at execution.</para> </summary>
        public string Args { get; }

        /// <summary>
        /// 创建执行结果。
        /// <para>Creates an execution result.</para>
        /// </summary>
        /// <param name="id">命令 ID。<para>Command ID.</para></param>
        /// <param name="success">是否成功。<para>Whether it succeeded.</para></param>
        /// <param name="message">原因。<para>Reason.</para></param>
        /// <param name="args">参数原始文本（可选）。<para>Raw argument text (optional).</para></param>
        public GmResult(string id, bool success, string message, string args = null)
        {
            Id = id;
            Success = success;
            Message = message;
            TimestampUtc = System.DateTime.UtcNow;
            Args = args ?? string.Empty;
        }
    }
}
/*
 * ================================================
 * Describe:        用来控制程序总流程步骤的接口
 * Author:          Alvin5100
 * CreationTime:    2026-05-07 14:29:52
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-09 23:07:04
 * ScriptVersion:   0.1
 * ===============================================
 */

using System.Threading;
using Cysharp.Threading.Tasks;

namespace EasyFramework.Systems.Procedure
{
    /// <summary>
    /// 流程节点接口
    /// </summary>
    public interface IProcedure
    {
        /// <summary>
        /// 进入流程（异步），传入上下文与取消标记
        /// <para>Enter procedure asynchronously with context and cancellation token</para>
        /// </summary>
        /// <param name="context">上下文内容</param>
        /// <param name="token">取消标记</param>
        /// <returns>异步操作</returns>
        UniTask OnEnter(ProcedureContext context, CancellationToken token);

        /// <summary>
        /// 离开流程（异步），无论正常或异常退出均会调用
        /// <para>Leave procedure asynchronously, called on both normal and abnormal exit</para>
        /// </summary>
        /// <param name="token">取消标记</param>
        /// <returns>异步操作</returns>
        UniTask OnLeave(CancellationToken token);
        
        /// <summary>
        /// 更新本流程
        /// <para>Update this procedure</para>
        /// </summary>
        /// <param name="elapse">逻辑流逝时间</param>
        /// <param name="realElapse">真实流逝时间</param>
        void OnUpdate(float elapse, float realElapse);
    }
}

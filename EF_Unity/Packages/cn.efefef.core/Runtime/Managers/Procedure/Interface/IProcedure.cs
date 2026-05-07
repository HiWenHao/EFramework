/*
 * ================================================
 * Describe:      用来控制程序总流程步骤的接口
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 14:29:52
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-07 14:29:52
 * ScriptVersion: 0.1
 * ===============================================
 */

using Cysharp.Threading.Tasks;

namespace EasyFramework.Managers.Procedure
{
    /// <summary>
    /// 流程节点接口
    /// </summary>
    public interface IProcedure
    {
        /// <summary>
        /// 进入流程（异步），传入上下文
        /// </summary>
        UniTask OnEnter(ProcedureContext context);
        
        /// <summary>
        /// 离开流程（异步），无论正常或异常退出均会调用
        /// </summary>
        UniTask OnLeave();
        
        /// <summary>
        /// 更新本流程
        /// </summary>
        void OnUpdate(float elapse, float realElapse);
    }
}
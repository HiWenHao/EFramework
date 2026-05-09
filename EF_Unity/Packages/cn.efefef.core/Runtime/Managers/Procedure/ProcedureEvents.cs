/*
 * ================================================
 * Describe:      流程生命周期事件定义，用于事件系统发布/订阅
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 18:52:16
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-08
 * ScriptVersion: 0.2
 * ===============================================
 */

using System;

namespace EasyFramework.Managers.Procedure
{
    /// <summary>
    /// 流程进入事件（当新流程实例被创建并即将执行 OnEnter 时发布）
    /// </summary>
    public struct ProcedureEnterEvent
    {
        /// <summary>新流程实例的唯一标识符</summary>
        public uint Uid { get; }

        /// <summary>父流程实例的 Uid（0 表示根流程）</summary>
        public uint ParentUid { get; }

        /// <summary>流程的具体类型</summary>
        public Type ProcedureType { get; }

        /// <summary>当前嵌套深度（根深度为 1）</summary>
        public int Depth { get; }

        public ProcedureEnterEvent(uint uid, uint parentUid, Type procedureType, int depth)
        {
            Uid = uid;
            ParentUid = parentUid;
            ProcedureType = procedureType;
            Depth = depth;
        }
    }

    /// <summary>
    /// 流程激活事件（当流程的 OnEnter 异步方法正常完成，并且未在进入过程中退出时发布）
    /// 此时流程已成为栈顶活动流程，开始接收 OnUpdate 调用
    /// </summary>
    public struct ProcedureActivateEvent
    {
        /// <summary>被激活的流程实例 Uid</summary>
        public uint Uid { get; }

        /// <summary>流程的具体类型</summary>
        public Type ProcedureType { get; }

        /// <summary>当前嵌套深度</summary>
        public int Depth { get; }

        public ProcedureActivateEvent(uint uid, Type procedureType, int depth)
        {
            Uid = uid;
            ProcedureType = procedureType;
            Depth = depth;
        }
    }

    /// <summary>
    /// 流程挂起事件（当新子流程启动前，当前活动流程被压栈并暂停 OnUpdate 时发布）
    /// </summary>
    public struct ProcedureSuspendEvent
    {
        /// <summary>被挂起的流程实例 Uid</summary>
        public uint Uid { get; }

        /// <summary>流程的具体类型</summary>
        public Type ProcedureType { get; }

        /// <summary>当前嵌套深度</summary>
        public int Depth { get; }

        public ProcedureSuspendEvent(uint uid, Type procedureType, int depth)
        {
            Uid = uid;
            ProcedureType = procedureType;
            Depth = depth;
        }
    }

    /// <summary>
    /// 流程恢复事件（当子流程退出后，父流程重新成为活动流程并恢复 OnUpdate 时发布）
    /// </summary>
    public struct ProcedureResumeEvent
    {
        /// <summary>被恢复的流程实例 Uid</summary>
        public uint Uid { get; }

        /// <summary>流程的具体类型</summary>
        public Type ProcedureType { get; }

        /// <summary>当前嵌套深度</summary>
        public int Depth { get; }

        public ProcedureResumeEvent(uint uid, Type procedureType, int depth)
        {
            Uid = uid;
            ProcedureType = procedureType;
            Depth = depth;
        }
    }

    /// <summary>
    /// 流程离开事件（当流程即将执行 OnLeave 并销毁时发布，无论是正常退出、超时强制退出还是异常退出）
    /// </summary>
    public struct ProcedureExitEvent
    {
        /// <summary>正在退出的流程实例 Uid</summary>
        public uint Uid { get; }

        /// <summary>流程的具体类型</summary>
        public Type ProcedureType { get; }

        /// <summary>当前嵌套深度</summary>
        public int Depth { get; }

        public ProcedureExitEvent(uint uid, Type procedureType, int depth)
        {
            Uid = uid;
            ProcedureType = procedureType;
            Depth = depth;
        }
    }
    
    /// <summary>
    /// 流程超时事件（当流程进入阶段超过设定时间未完成时发布）
    /// </summary>
    public struct ProcedureTimeoutEvent
    {
        /// <summary>超时的流程实例 UID</summary>
        public uint Uid { get; }
        
        /// <summary>流程的具体类型</summary>
        public Type ProcedureType { get; }

        /// <summary>当前嵌套深度</summary>
        public int Depth { get; }

        public ProcedureTimeoutEvent(uint uid, Type procedureType, int depth)
        {
            Uid = uid;
            ProcedureType = procedureType;
            Depth = depth;
        }
    }
}
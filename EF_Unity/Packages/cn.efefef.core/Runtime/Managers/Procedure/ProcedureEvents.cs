/*
 * ================================================
 * Describe:        流程生命周期事件定义，用于事件系统发布/订阅
 * Author:          Alvin5100
 * CreationTime:    2026-05-07 18:52:16
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-09 23:07:04
 * ScriptVersion:   0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Managers.Procedure
{
    /// <summary>
    /// 流程进入事件（当新流程实例被创建并即将执行 OnEnter 时发布）
    /// <para>Procedure enter event (published when a new procedure instance is created and is about to execute OnEnter)</para>
    /// </summary>
    public readonly struct ProcedureEnterEvent
    {
        /// <summary>
        /// 新流程实例的唯一标识符
        /// <para>Unique identifier of the new procedure instance</para>
        /// </summary>
        public long Uid { get; }

        /// <summary>
        /// 父流程实例的 Uid（0 表示根流程）
        /// <para>Uid of the parent procedure instance (0 indicates a root procedure)</para>
        /// </summary>
        public long ParentUid { get; }

        /// <summary>
        /// 流程的具体类型
        /// <para>Concrete type of the procedure</para>
        /// </summary>
        public Type ProcedureType { get; }

        /// <summary>
        /// 当前嵌套深度（根深度为 1）
        /// <para>Current nesting depth (root depth is 1)</para>
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// 初始化流程进入事件的新实例
        /// <para>Initializes a new instance of the procedure enter event</para>
        /// </summary>
        /// <param name="uid">新流程实例的唯一标识符 <para>Unique identifier of the new procedure instance</para></param>
        /// <param name="parentUid">父流程实例的 Uid（0 表示根流程） <para>Uid of the parent procedure instance (0 indicates a root procedure)</para></param>
        /// <param name="procedureType">流程的具体类型 <para>Concrete type of the procedure</para></param>
        /// <param name="depth">当前嵌套深度（根深度为 1） <para>Current nesting depth (root depth is 1)</para></param>
        public ProcedureEnterEvent(long uid, long parentUid, Type procedureType, int depth)
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
    /// <para>Procedure activate event (published when a procedure's async OnEnter method completes normally and the procedure did not exit during entry). At this point, the procedure becomes the active top-of-stack procedure and starts receiving OnUpdate calls</para>
    /// </summary>
    public readonly struct ProcedureActivateEvent
    {
        /// <summary>
        /// 被激活的流程实例 Uid
        /// <para>Uid of the activated procedure instance</para>
        /// </summary>
        public long Uid { get; }

        /// <summary>
        /// 流程的具体类型
        /// <para>Concrete type of the procedure</para>
        /// </summary>
        public Type ProcedureType { get; }

        /// <summary>
        /// 当前嵌套深度
        /// <para>Current nesting depth</para>
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// 初始化流程激活事件的新实例
        /// <para>Initializes a new instance of the procedure activate event</para>
        /// </summary>
        /// <param name="uid">被激活的流程实例 Uid <para>Uid of the activated procedure instance</para></param>
        /// <param name="procedureType">流程的具体类型 <para>Concrete type of the procedure</para></param>
        /// <param name="depth">当前嵌套深度 <para>Current nesting depth</para></param>
        public ProcedureActivateEvent(long uid, Type procedureType, int depth)
        {
            Uid = uid;
            ProcedureType = procedureType;
            Depth = depth;
        }
    }

    /// <summary>
    /// 流程挂起事件（当新子流程启动前，当前活动流程被压栈并暂停 OnUpdate 时发布）
    /// <para>Procedure suspend event (published when the current active procedure is pushed onto the stack and its OnUpdate is paused before a new child procedure starts)</para>
    /// </summary>
    public readonly struct ProcedureSuspendEvent
    {
        /// <summary>
        /// 被挂起的流程实例 Uid
        /// <para>Uid of the suspended procedure instance</para>
        /// </summary>
        public long Uid { get; }

        /// <summary>
        /// 流程的具体类型
        /// <para>Concrete type of the procedure</para>
        /// </summary>
        public Type ProcedureType { get; }

        /// <summary>
        /// 当前嵌套深度
        /// <para>Current nesting depth</para>
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// 初始化流程挂起事件的新实例
        /// <para>Initializes a new instance of the procedure suspend event</para>
        /// </summary>
        /// <param name="uid">被挂起的流程实例 Uid <para>Uid of the suspended procedure instance</para></param>
        /// <param name="procedureType">流程的具体类型 <para>Concrete type of the procedure</para></param>
        /// <param name="depth">当前嵌套深度 <para>Current nesting depth</para></param>
        public ProcedureSuspendEvent(long uid, Type procedureType, int depth)
        {
            Uid = uid;
            ProcedureType = procedureType;
            Depth = depth;
        }
    }

    /// <summary>
    /// 流程恢复事件（当子流程退出后，父流程重新成为活动流程并恢复 OnUpdate 时发布）
    /// <para>Procedure resume event (published when a child procedure exits and the parent becomes the active procedure again and resumes OnUpdate)</para>
    /// </summary>
    public readonly struct ProcedureResumeEvent
    {
        /// <summary>
        /// 被恢复的流程实例 Uid
        /// <para>Uid of the resumed procedure instance</para>
        /// </summary>
        public long Uid { get; }

        /// <summary>
        /// 流程的具体类型
        /// <para>Concrete type of the procedure</para>
        /// </summary>
        public Type ProcedureType { get; }

        /// <summary>
        /// 当前嵌套深度
        /// <para>Current nesting depth</para>
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// 初始化流程恢复事件的新实例
        /// <para>Initializes a new instance of the procedure resume event</para>
        /// </summary>
        /// <param name="uid">被恢复的流程实例 Uid <para>Uid of the resumed procedure instance</para></param>
        /// <param name="procedureType">流程的具体类型 <para>Concrete type of the procedure</para></param>
        /// <param name="depth">当前嵌套深度 <para>Current nesting depth</para></param>
        public ProcedureResumeEvent(long uid, Type procedureType, int depth)
        {
            Uid = uid;
            ProcedureType = procedureType;
            Depth = depth;
        }
    }

    /// <summary>
    /// 流程离开事件（当流程即将执行 OnLeave 并销毁时发布，无论是正常退出、超时强制退出还是异常退出）
    /// <para>Procedure exit event (published when a procedure is about to execute OnLeave and be destroyed, whether by normal exit, forced timeout exit, or exceptional exit)</para>
    /// </summary>
    public readonly struct ProcedureExitEvent
    {
        /// <summary>
        /// 正在退出的流程实例 Uid
        /// <para>Uid of the procedure instance that is exiting</para>
        /// </summary>
        public long Uid { get; }

        /// <summary>
        /// 流程的具体类型
        /// <para>Concrete type of the procedure</para>
        /// </summary>
        public Type ProcedureType { get; }

        /// <summary>
        /// 当前嵌套深度
        /// <para>Current nesting depth</para>
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// 初始化流程离开事件的新实例
        /// <para>Initializes a new instance of the procedure exit event</para>
        /// </summary>
        /// <param name="uid">正在退出的流程实例 Uid <para>Uid of the procedure instance that is exiting</para></param>
        /// <param name="procedureType">流程的具体类型 <para>Concrete type of the procedure</para></param>
        /// <param name="depth">当前嵌套深度 <para>Current nesting depth</para></param>
        public ProcedureExitEvent(long uid, Type procedureType, int depth)
        {
            Uid = uid;
            ProcedureType = procedureType;
            Depth = depth;
        }
    }
    
    /// <summary>
    /// 流程超时事件（当流程进入阶段超过设定时间未完成时发布）
    /// <para>Procedure timeout event (published when a procedure's entry phase exceeds the configured time without completing)</para>
    /// </summary>
    public readonly struct ProcedureTimeoutEvent
    {
        /// <summary>
        /// 超时的流程实例 UID
        /// <para>UID of the timed-out procedure instance</para>
        /// </summary>
        public long Uid { get; }
        
        /// <summary>
        /// 流程的具体类型
        /// <para>Concrete type of the procedure</para>
        /// </summary>
        public Type ProcedureType { get; }

        /// <summary>
        /// 当前嵌套深度
        /// <para>Current nesting depth</para>
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// 初始化流程超时事件的新实例
        /// <para>Initializes a new instance of the procedure timeout event</para>
        /// </summary>
        /// <param name="uid">超时的流程实例 UID <para>UID of the timed-out procedure instance</para></param>
        /// <param name="procedureType">流程的具体类型 <para>Concrete type of the procedure</para></param>
        /// <param name="depth">当前嵌套深度 <para>Current nesting depth</para></param>
        public ProcedureTimeoutEvent(long uid, Type procedureType, int depth)
        {
            Uid = uid;
            ProcedureType = procedureType;
            Depth = depth;
        }
    }
}

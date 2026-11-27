/*
 * ================================================
 * Describe:        This script is used to define base class for procedures.
 * Author:          Alvin5100
 * CreationTime:    2026-05-07 15:29:18
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-09 23:07:04
 * ScriptVersion:   0.1
 * ===============================================
 */

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Procedure
{
    /// <summary>
    /// 单流程基类
    /// </summary>
    public abstract class ProcedureBase : IProcedure
    {
        /// <summary>
        ///当前流程上下文
        /// <para>The context of the current procedure</para>
        /// </summary>
        protected ProcedureContext Context { get; private set; }

        /// <summary>
        /// 当前流程取消令牌
        /// <para>Current procedure cancellation token</para>
        /// </summary>
        protected CancellationToken Token { get; private set; }

        /// <summary>
        /// 当前流程实例唯一标识符
        /// <para>The unique identifier of the current process instance</para>
        /// </summary>
        protected long Uid => Context?.Uid ?? 0;

        /// <summary>
        /// 当前嵌套深度（根流程深度为1）
        /// <para>Current nesting depth (root process depth is 1)</para>
        /// </summary>
        protected int Depth => Context?.Depth ?? 0;

        async UniTask IProcedure.OnEnter(ProcedureContext context, CancellationToken token)
        {
            Context = context;
            Token = token;
            await OnEnterAsync();
        }

        async UniTask IProcedure.OnLeave(CancellationToken token)
        {
            Token = token;
            try
            {
                await OnLeaveAsync();
            }
            finally
            {
                try
                {
                    OnLeaveFinally();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

                Context = null;
                Token = CancellationToken.None;
            }
        }

        /// <summary>
        /// 以强类型读取启动参数，类型不符时返回 false
        /// <para>Read startup parameters as a strongly typed reference; returns false on type mismatch</para>
        /// </summary>
        /// <param name="param">参数对象或 null <para>Parameter object or null</para></param>
        /// <typeparam name="T">参数类型（引用类型） <para>Parameter type (reference type)</para></typeparam>
        /// <returns>参数是否为指定类型</returns>
        protected bool GetParams<T>(out T param) where T : class
        {
            if (Context.Params is T contextParams)
            {
                param = contextParams;
                return true;
            }

            param = null;
            return false;
        }

        /// <summary>
        /// 进入异步逻辑（子类实现）
        /// <para>Enter async logic (implemented by subclass)</para>
        /// </summary>
        /// <returns>异步操作</returns>
        protected abstract UniTask OnEnterAsync();

        /// <summary>
        /// 离开异步逻辑（子类实现）
        /// <para>Leave async logic (implemented by subclass)</para>
        /// </summary>
        /// <returns>异步操作</returns>
        protected abstract UniTask OnLeaveAsync();

        /// <summary>
        /// 离开收尾逻辑（子类可选重写），无论 OnLeaveAsync 是否异常都会执行
        /// <para>Finalization logic after leave, called even if OnLeaveAsync threw exception</para>
        /// </summary>
        protected virtual void OnLeaveFinally()
        {
        }

        /// <summary>
        /// 更新流程逻辑
        /// <para>Update procedure logic</para>
        /// </summary>
        /// <param name="elapse">逻辑流逝时间</param>
        /// <param name="realElapse">真实流逝时间</param>
        public virtual void OnUpdate(float elapse, float realElapse)
        {
        }
    }
}
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
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Systems.Procedure
{
    /// <summary>
    /// 单流程基类
    /// </summary>
    public abstract class ProcedureBase : IProcedure
    {
        protected ProcedureContext Ctx { get; private set; }
        protected CancellationToken Token { get; private set; }
        protected long Uid => Ctx?.Uid ?? 0;
        protected int Depth => Ctx?.Depth ?? 0;
        private IReadOnlyDictionary<string, object> Params => Ctx?.Params;

        async UniTask IProcedure.OnEnter(ProcedureContext context, CancellationToken token)
        {
            Ctx = context;
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

                Ctx = null;
                Token = CancellationToken.None;
            }
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

        /// <summary>
        /// 获取参数，若不存在或类型不匹配则返回默认值
        /// <para>Get parameter, return default if key missing or type mismatch</para>
        /// </summary>
        /// <param name="key">参数键名</param>
        /// <param name="defaultValue">默认值</param>
        /// <typeparam name="T">参数类型</typeparam>
        /// <returns>参数值或默认值</returns>
        protected T GetParam<T>(string key, T defaultValue = default)
        {
            if (Params != null && Params.TryGetValue(key, out var value) && value is T t)
                return t;
            return defaultValue;
        }
    }
}

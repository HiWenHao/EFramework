/*
 * ================================================
 * Describe:      This script is used to define base class for procedures.
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 15:29:18
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-08
 * ScriptVersion: 0.2
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Managers.Procedure
{
    public abstract class ProcedureBase : IProcedure
    {
        protected ProcedureContext Ctx { get; private set; }
        protected CancellationToken Token { get; private set; }

        protected uint UID => Ctx?.UID ?? 0;
        protected int Depth => Ctx?.Depth ?? 0;
        protected IReadOnlyDictionary<string, object> Params => Ctx?.Params;

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

        protected abstract UniTask OnEnterAsync();
        protected abstract UniTask OnLeaveAsync();

        protected virtual void OnLeaveFinally()
        {
        }

        public virtual void OnUpdate(float elapse, float realElapse)
        {
        }

        protected T GetParam<T>(string key, T defaultValue = default)
        {
            if (Params != null && Params.TryGetValue(key, out var value) && value is T t)
                return t;
            return defaultValue;
        }
    }
}
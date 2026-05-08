/*
 * ================================================
 * Describe:      内部流程实例数据
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 18:49:57
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-08
 * ScriptVersion: 0.2
 * ===============================================
 */
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace EasyFramework.Managers.Procedure
{
    internal sealed class ProcedureInstance
    {
        public uint UID;
        public uint ParentUID;
        public int Depth;
        public uint RuntimeVersion;
        public int ExitQueued;
        public Type ProcedureType;
        public IProcedure Procedure;
        public ProcedureContext Context;
        public Dictionary<string, object> Params;
        public CancellationTokenSource LifecycleCts;
        public CancellationTokenSource EnterTimeoutCts;
        public ProcedureState State;
        public int ExitState;
        public UniTaskCompletionSource CompletionSource;

        public bool IsExited => ExitState != 0;
        public bool IsActive => State == ProcedureState.Active;

        public void Reset()
        {
            // 先递增版本，避免旧引用误判
            RuntimeVersion++;

            UID = 0;
            ExitQueued = 0;
            ParentUID = 0;
            Depth = 0;
            ProcedureType = null;
            Procedure = null;
            Context = null;

            Params?.Clear();
            Params = null;

            try
            {
                LifecycleCts?.Cancel();
                LifecycleCts?.Dispose();
            }
            catch { }
            LifecycleCts = null;

            try
            {
                EnterTimeoutCts?.Cancel();
                EnterTimeoutCts?.Dispose();
            }
            catch { }
            EnterTimeoutCts = null;

            State = ProcedureState.None;
            ExitState = 0;
            CompletionSource = null;
        }
    }
}
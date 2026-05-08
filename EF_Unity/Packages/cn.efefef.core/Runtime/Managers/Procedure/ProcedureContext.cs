/*
 * ================================================
 * Describe:      流程上下文，提供子流程启动、结束自身等方法
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 18:46:41
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-08
 * ScriptVersion: 0.3
 * ===============================================
 */
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace EasyFramework.Managers.Procedure
{
    public sealed class ProcedureContext
    {
        public uint UID;
        public uint ParentUID;
        public int Depth;
        public uint RuntimeVersion;
        public IReadOnlyDictionary<string, object> Params;

        internal bool IsDisposed;

        internal void Reset()
        {
            UID = 0;
            ParentUID = 0;
            Depth = 0;
            RuntimeVersion = 0;
            Params = null;
            IsDisposed = false;
        }

        private void CheckDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(ProcedureContext));

            var inst = ProcedureManager.Instance.GetInstanceByUidInternal(UID);
            if (inst == null || inst.RuntimeVersion != RuntimeVersion)
                throw new ObjectDisposedException(nameof(ProcedureContext));
        }

        public async UniTask StartSubProcedure<T>(Dictionary<string, object> parameters = null)
            where T : IProcedure
        {
            CheckDisposed();
            await ProcedureManager.Instance.StartSubProcedureAndWait<T>(this, parameters);
        }

        public async UniTask EndProcedure()
        {
            CheckDisposed();
            await ProcedureManager.Instance.EndProcedureInternal(this);
        }
    }
}
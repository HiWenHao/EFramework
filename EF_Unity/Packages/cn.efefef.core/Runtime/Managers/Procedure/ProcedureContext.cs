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
    /// <summary>
    /// 单流程的上下文
    /// </summary>
    public sealed class ProcedureContext
    {
        /// <summary>当前流程实例唯一标识符</summary>
        public long Uid { get; internal set; }

        /// <summary>父流程实例唯一标识符（0表示根流程）</summary>
        public long ParentUid { get; internal set; }

        /// <summary>运行时版本号，用于检测上下文是否失效</summary>
        public uint RuntimeVersion { get; internal set; }

        /// <summary>当前嵌套深度（根流程深度为1）</summary>
        public int Depth { get; internal set; }

        /// <summary>启动时传入的参数集合（只读）</summary>
        public IReadOnlyDictionary<string, object> Params { get; internal set; }

        /// <summary>上下文是否已被释放</summary>
        internal bool IsDisposed { get; set; }

        /// <summary>
        /// 重置上下文状态，供对象池使用
        /// <para>Reset context state for object pool</para>
        /// </summary>
        internal void Reset()
        {
            Uid = 0;
            ParentUid = 0;
            Depth = 0;
            RuntimeVersion = 0;
            Params = null;
            IsDisposed = false;
        }

        // 检查上下文是否已释放，若已释放则抛出异常
        private void CheckDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(ProcedureContext));

            var inst = ProcedureSystem.Instance.GetInstanceByUidInternal(Uid);
            if (inst == null || inst.RuntimeVersion != RuntimeVersion)
                throw new ObjectDisposedException(nameof(ProcedureContext));
        }

        /// <summary>
        /// 启动子流程并等待其完全退出
        /// <para>Start a sub-procedure and wait for its full exit</para>
        /// </summary>
        /// <param name="parameters">启动参数</param>
        /// <typeparam name="T">子流程类型</typeparam>
        /// <returns>异步操作，等待子流程完全退出</returns>
        public async UniTask StartSubProcedure<T>(Dictionary<string, object> parameters = null)
            where T : IProcedure
        {
            CheckDisposed();
            await ProcedureSystem.Instance.StartSubProcedureAndWait<T>(this, parameters);
        }

        /// <summary>
        /// 结束当前流程（主动退出）
        /// <para>End the current procedure (voluntary exit)</para>
        /// </summary>
        /// <returns>异步操作，返回是否成功退出</returns>
        public async UniTask<bool> EndProcedure()
        {
            CheckDisposed();
            return await ProcedureSystem.Instance.EndProcedureInternal(this);
        }
    }
}
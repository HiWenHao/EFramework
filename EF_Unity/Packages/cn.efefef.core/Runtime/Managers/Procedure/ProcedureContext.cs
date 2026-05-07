/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 18:46:41
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-07 18:46:41
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace EasyFramework.Managers.Procedure
{
    /// <summary>
    /// 流程上下文提供子流程启动、结束自身、获取参数、获取 UID 等方法
    /// </summary>
    public sealed class ProcedureContext
    {
        /// <summary>
        /// 当前流程实例的唯一 ID
        /// </summary>
        public uint UID { get; internal set; }

        /// <summary>
        /// 父流程实例的 UID（0 表示根流程）
        /// </summary>
        public uint ParentUID { get; internal set; }

        /// <summary>
        /// 当前嵌套深度
        /// </summary>
        public int Depth { get; internal set; }

        /// <summary>
        /// 携带的参数
        /// </summary>
        public Dictionary<string, object> Params { get; internal set; }

        /// <summary>
        /// 在流程内部启动一个同步并行子流程（父流程被挂起，直到子流程退出）
        /// </summary>
        public async UniTask StartSubProcedure<T>(object parameters = null) where T : IProcedure
        {
            await ProcedureManager.Instance.StartSubProcedureInternal<T>(this, parameters);
        }

        /// <summary>
        /// 结束当前流程（如果是子流程则返回到父流程，否则整个流程系统停止）
        /// </summary>
        public void EndProcedure()
        {
            ProcedureManager.Instance.EndProcedureInternal(this);
        }
    }
}
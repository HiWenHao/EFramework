/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 18:49:57
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-07 18:49:57
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.Threading;

namespace EasyFramework.Managers.Procedure
{
	/// <summary>
	/// 流程实例运行时数据
	/// </summary>
	internal class ProcedureInstance
	{
		public uint UID;
		public uint ParentUID;
		public int Depth;
		public Type ProcedureType;
		public IProcedure Procedure;
		public ProcedureContext Context;
		public Dictionary<string, object> Params;
		public CancellationTokenSource TimeoutCts;
		
		/// <summary>
		/// 是否处于栈顶（即正在运行）
		/// </summary>
		public bool IsActive;
		
		/// <summary>
		/// 正在退出中，防止重入
		/// </summary>
		public bool IsExiting;

		/// <summary>
		/// 释放
		/// </summary>
		public void Dispose()
		{
			TimeoutCts?.Cancel();
			TimeoutCts?.Dispose();
			TimeoutCts = null;
		}
	}
}

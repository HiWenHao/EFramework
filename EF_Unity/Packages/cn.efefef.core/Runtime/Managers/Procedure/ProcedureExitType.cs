/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-09 14:37:04
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-09 14:37:04
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework.Managers.Procedure
{
	/// <summary>
	/// 流程退出原因类型
	/// <para>Procedure exit reason type</para>
	/// </summary>
	public enum ProcedureExitType
	{
		/// <summary>
		/// 正常执行完（调用了 EndProcedure 或自然退出）
		/// <para>Completed normally (EndProcedure called or natural exit)</para>
		/// </summary>
		Completed,

		/// <summary>
		/// 进入超时
		/// <para>Entry timeout</para>
		/// </summary>
		Timeout,

		/// <summary>
		/// OnEnter/OnUpdate 抛出未处理异常
		/// <para>Unhandled exception thrown by OnEnter/OnUpdate</para>
		/// </summary>
		Exception,

		/// <summary>
		/// 外部取消（如 Switch 清空栈）
		/// <para>External cancellation (e.g. Switch clears the stack)</para>
		/// </summary>
		Cancelled,

		/// <summary>
		/// 流程类型未注册
		/// <para>Procedure type not registered</para>
		/// </summary>
		NotRegistered,

		/// <summary>
		/// 超过最大深度
		/// <para>Maximum depth exceeded</para>
		/// </summary>
		DepthExceeded,

		/// <summary>
		/// 链式重复超限
		/// <para>Chain repetition limit exceeded</para>
		/// </summary>
		ChainRepeated,
	}
}

/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 15:29:18
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-07 15:29:18
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using EasyFramework;

namespace EasyFramework.Managers.Procedure
{
	/// <summary>
	/// 流程抽象基类（自动注入上下文，提供便捷的 UID 等属性）
	/// </summary>
	public abstract class ProcedureBase : IProcedure
	{
		protected ProcedureContext Ctx { get; private set; }
		protected uint UID => Ctx?.UID ?? 0;
		protected int Depth => Ctx?.Depth ?? 0;
		protected Dictionary<string, object> Params => Ctx?.Params;

		async UniTask IProcedure.OnEnter(ProcedureContext context)
		{
			Ctx = context;
			await OnEnterAsync();
		}

		protected abstract UniTask OnEnterAsync();
		public virtual void OnUpdate(float elapse, float realElapse) { }
		public abstract UniTask OnLeave();
	}
}

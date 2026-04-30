/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-04-30 16:30:28
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-30 16:30:28
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework.Managers.Pool
{
	/// <summary>
	/// 对象池清空接口
	/// </summary>
	public interface IClearablePool
	{
		/// <summary>
		/// 清空池（销毁空闲对象，不影响已取出的对象）
		/// </summary>
		void Clear();
	}
}

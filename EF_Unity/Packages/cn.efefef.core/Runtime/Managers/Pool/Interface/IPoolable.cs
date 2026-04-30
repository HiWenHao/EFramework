/*
 * ================================================
 * Describe:      This script is pooling object interface.
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-09 18:49:42
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-30 16:29:22
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework.Managers.Pool
{
	/// <summary>
	/// 池化对象接口
	/// </summary>
	public interface IPoolable
	{
		/// <summary>
		/// 来自池中
		/// </summary>
		bool IsFromPool { get; set; }
		
		/// <summary>
		/// 从池中取出时调用
		/// </summary>
		void OnSpawn();

		/// <summary>
		/// 放回池中时调用
		/// </summary>
		void OnDespawn();
	}
}

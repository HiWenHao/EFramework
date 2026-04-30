/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-04-30 16:30:28
 * ModifyAuthor:  Alvin8412
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
		/// 清空
		/// </summary>
		void Clear();
	}
}

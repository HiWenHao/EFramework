/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-10 13:43:43
 * ModifyAuthor:  Alvin5100(Wang)
 * ModifyTime:    2025-12-10 13:43:43
 * ScriptVersion: 0.1
 * ===============================================
*/

namespace EasyFramework.Managers.Pool
{
	/// <summary>
	/// 对象池元素事件类型
	/// </summary>
	public enum PoolEventType
	{
		/// <summary>
		/// 被创建
		/// </summary>
		Created,
		
		/// <summary>
		/// 被获取
		/// </summary>
		Get,
		
		/// <summary>
		/// 被回收
		/// </summary>
		Recycle,
		
		/// <summary>
		/// 被清理
		/// </summary>
		Cleared,
		
		/// <summary>
		/// 被销毁
		/// </summary>
		Destroyed,
		
		/// <summary>
		/// 对象池被创建
		/// </summary>
		PoolCleared,
		
		/// <summary>
		/// 对象池被销毁
		/// </summary>
		PoolDestroyed,
		
		/// <summary>
		/// 统计数据更新
		/// </summary>
		StatisticsUpdated
	}
}

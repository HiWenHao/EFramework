/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-10 14:33:51
 * ModifyAuthor:  Alvin5100(Wang)
 * ModifyTime:    2025-12-10 14:33:51
 * ScriptVersion: 0.1
 * ===============================================
*/

using System;

namespace EasyFramework.Managers.Pool
{
	/// <summary>
	/// 全局池统计信息
	/// </summary>
	public struct GlobalPoolStatistics
	{
		public int TotalPools;
		public int TotalAvailable;
		public int TotalActive;
		public int TotalCreated;
		public int PeakActive;
		public DateTime LastUpdateTime;
	}
}

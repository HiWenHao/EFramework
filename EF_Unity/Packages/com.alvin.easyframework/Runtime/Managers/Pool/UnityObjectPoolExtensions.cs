/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-10 09:53:09
 * ModifyAuthor:  Alvin5100(Wang)
 * ModifyTime:    2025-12-10 09:53:09
 * ScriptVersion: 0.1
 * ===============================================
*/

using System;
using System.Collections.Generic;

namespace EasyFramework.Managers.Pool
{
	/// <summary>
	/// 对象池扩展方法
	/// </summary>
	public static class UnityObjectPoolExtensions
	{
		/// <summary>
		/// 批量获取对象
		/// </summary>
		public static List<T> GetBatch<T>(this IPool<T> pool, int count) where T : class
		{
			var result = new List<T>();
			for (int i = 0; i < count; i++)
			{
				var obj = pool.Get();
				if (obj != null)
				{
					result.Add(obj);
				}
			}

			return result;
		}

		/// <summary>
		/// 批量回收对象
		/// </summary>
		public static int RecycleBatch<T>(this IPool<T> pool, IEnumerable<T> objects) where T : class
		{
			int count = 0;
			foreach (var obj in objects)
			{
				if (pool.Recycle(obj))
				{
					count++;
				}
			}

			return count;
		}

		/// <summary>
		/// 安全获取对象（带默认值）
		/// </summary>
		public static T GetSafe<T>(this IPool<T> pool, T defaultValue = null) where T : class
		{
			return pool.Get() ?? defaultValue;
		}

		/// <summary>
		/// 使用后自动回收的模式
		/// </summary>
		public static void Using<T>(this IPool<T> pool, Action<T> action) where T : class
		{
			var obj = pool.Get();
			if (obj == null) return;

			try
			{
				action(obj);
			}
			finally
			{
				pool.Recycle(obj);
			}
		}
	}

}

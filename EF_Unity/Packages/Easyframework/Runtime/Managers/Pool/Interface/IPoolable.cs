/*
 * ================================================
 * Describe:      This script is pooling object interface.
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-09 18:49:42
 * ModifyAuthor:  Alvin5100(Wang)
 * ModifyTime:    2025-12-09 18:49:42
 * ScriptVersion: 0.1
 * ===============================================
*/

using UnityEngine;

namespace EasyFramework.Managers.Pool
{
	/// <summary>
	/// 池化对象接口
	/// </summary>
	public interface IPoolable
	{
		/// <summary>
		/// 归属对象池
		/// </summary>
		IPool<GameObject> Pool { get; }
		
		/// <summary>
		/// 初始化，定义当前对象归属池
		/// </summary>
		/// <param name="ownerPool"></param>
		void Initialize(IPool<GameObject> ownerPool);
		
		/// <summary>
		/// 当从对象池中获取时调用
		/// </summary>
		void OnGet();
    
		/// <summary>
		/// 当返回到对象池时调用
		/// </summary>
		void OnRecycle();
    
		/// <summary>
		/// 重置对象
		/// </summary>
		void ResetState();
	}
}

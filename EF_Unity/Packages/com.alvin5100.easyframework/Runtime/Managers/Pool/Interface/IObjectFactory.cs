/*
 * ================================================
 * Describe:      This script is used to restricted object factory.
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-09 18:50:24
 * ModifyAuthor:  Alvin5100(Wang)
 * ModifyTime:    2025-12-09 18:50:24
 * ScriptVersion: 0.1
 * ===============================================
*/

namespace EasyFramework.Managers.Pool
{
	/// <summary>
	/// 对象工厂接口
	/// </summary>
	public interface IObjectFactory<T>
	{
		/// <summary>
		/// 创建新对象
		/// </summary>
		T Create();
    
		/// <summary>
		/// 释放并销毁对象
		/// </summary>
		void Dispose(T item);
    
		/// <summary>
		/// 验证对象是否有效
		/// </summary>
		bool IsValidity(T item);
    
		/// <summary>
		/// 重置对象状态
		/// </summary>
		void ResetState(T item);
	}
}

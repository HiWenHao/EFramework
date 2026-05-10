/*
 * ================================================
 * Describe:        This script is interface for managing a pool of objects of a certain type.
 * Author:          Alvin5100(Wang)
 * CreationTime:    2025-12-02 16:41:01
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-09 23:06:55
 * ScriptVersion:   0.1
 * ===============================================
 */

namespace EasyFramework.Systems.Pool
{
    /// <summary>
    /// 对象池接口
    /// <para>Object Pool Interface</para>
    /// </summary>
    /// <typeparam name="T"><para>泛型 Generics</para></typeparam>
    public interface IPool<T>
    {
        /// <summary>
        /// 从池中获取一个对象
        /// <para>Obtain an object from the pool</para>
        /// </summary>
        T Get(bool isFromPool);

        /// <summary>
        /// 将对象归还池中
        /// <para>Return the object to the pool.</para>
        /// </summary>
        /// <param name="item">池化对象 Poolable</param>
        void Recycle(T item);
    }
}

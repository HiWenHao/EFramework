/*
 * ================================================
 * Describe:        This script is pooling object interface.
 * Author:          Alvin5100(Wang)
 * CreationTime:    2025-12-09 18:49:42
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-09 23:06:55
 * ScriptVersion:   0.1
 * ===============================================
 */

namespace EasyFramework.Systems.Pool
{
    /// <summary>
    /// 池化对象接口
    /// <para>Interface for poolable objects</para>
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 来自池中
        /// <para>Indicates whether the object comes from a pool</para>
        /// </summary>
        bool IsFromPool { get; set; }

        /// <summary>
        /// 从池中取出时调用（此时对象已激活）
        /// <para>Called when the object is taken from the pool (the object is already active)</para>
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// 放回池中时调用（对象仍处于激活状态）
        /// <para>Called when the object is returned to the pool (the object is still active)</para>
        /// </summary>
        void OnDespawn();
    }
}

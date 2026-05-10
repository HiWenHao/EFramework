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
    /// <remarks>
    /// 实现此接口的组件应确保在对象池化期间始终存在（即预制体或创建时已挂载），
    /// 不支持运行时动态添加/删除，因为 PooledObject 会缓存所有 IPoolable 组件。
    /// <para>Components implementing this interface must always exist during object pooling (attached on prefab or at creation);
    /// dynamic addition/removal at runtime is not supported because PooledObject caches all IPoolable components.</para>
    /// </remarks>
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

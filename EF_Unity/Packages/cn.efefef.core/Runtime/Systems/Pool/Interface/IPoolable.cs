/*
 * ================================================
 * Describe:      This script is pooling object interface.
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-09 18:49:42
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-09 23:06:55
 * ScriptVersion:   0.1
 * ===============================================
 */

namespace EasyFramework.Systems.Pool
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
        ///  从池中取出时调用（此时对象已激活）
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// 放回池中时调用（对象仍处于激活状态）
        /// </summary>
        void OnDespawn();
    }
}

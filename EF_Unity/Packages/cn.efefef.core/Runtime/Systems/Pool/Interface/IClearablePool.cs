/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Alvin5100
 * CreationTime:    2026-04-30 16:30:28
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-09 23:06:55
 * ScriptVersion:   0.1
 * ===============================================
 */

namespace EasyFramework.Systems.Pool
{
    /// <summary>
    /// 对象池清空接口
    /// <para>Object Pool Clear Interface</para>
    /// </summary>
    public interface IClearablePool
    {
        /// <summary>
        /// 清空池（销毁空闲对象，不影响已取出的对象）
        /// <para>Clear the pool (destroy idle objects without affecting the retrieved objects)</para>
        /// </summary>
        void Clear();
    }
}

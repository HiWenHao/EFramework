/*
 * ================================================
 * Describe:		GameObject 对象池接口，扩展自 IPool
 * Author:			Alvin8412
 * CreationTime:	2026-05-10 19:09:43
 * ModifyAuthor:	Alvin8412
 * ModifyTime:		2026-05-10 19:09:43
 * ScriptVersion:	0.1
 * ===============================================
 */

using UnityEngine;

namespace EasyFramework.Systems.Pool
{
    /// <summary>
    /// GameObject 对象池接口，扩展自 IPool&lt;GameObject&gt;
    /// <para>GameObject pool interface, extending IPool&lt;GameObject&gt;.</para>
    /// </summary>
    public interface IGameObjectPool : IPool<GameObject>, IClearablePool
    {
        /// <summary>
        /// 空闲对象数量
        /// <para>Number of idle objects</para>
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 当前激活的对象数量
        /// <para>The current number of activated objects</para>
        /// </summary>
        int ActiveCount { get; }

        /// <summary>
        /// 总存活对象数量（激活 + 池中闲置）
        /// <para>Total number of surviving objects (activated + idle in the pool)</para>
        /// </summary>
        int TotalCount { get; }

        /// <summary>
        /// 是否开启调试日志
        /// <para>Whether to enable debug logging</para>
        /// </summary>
        bool OpenDebug { get; set; }

        /// <summary>
        /// 预先创建指定数量的对象放入池中。
        /// <para>Pre-create a specified number of objects and place them in the pool.</para>
        /// </summary>
        /// <param name="count">预创建数量</param>
        void Prewarm(int count);

        /// <summary>
        /// 输出所有泄漏的活动对象（仅当 OpenDebug = true 且定义了 POOL_DEBUG）。
        /// </summary>
        void DumpLeaks();

        /// <summary>
        /// 回收对象（无 GC 版本，参数为 PooledObject 组件）
        /// </summary>
        void Recycle(PooledObject pooledObject);
    }
}
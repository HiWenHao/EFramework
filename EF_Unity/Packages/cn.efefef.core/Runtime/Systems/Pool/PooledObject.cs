/*
 * ================================================
 * Describe:        挂载在池化 GameObject 上的组件，管理状态和生命周期回调。
 * Author:          Alvin5100
 * CreationTime:    2026-04-30 14:33:44
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-09 23:06:55
 * ScriptVersion:   0.1
 * ===============================================
 */

#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define POOL_DEBUG
#endif

using UnityEngine;
using System.Diagnostics;

namespace EasyFramework.Systems.Pool
{
    /// <summary>
    /// 池化对象组件，每个通过池生成的 GameObject 都会附带此组件。
    /// <para>Pooling object component. Each GameObject generated through the pool will come with this component.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PooledObject : MonoBehaviour
    {
        /// <summary>
        /// 原始预制体
        /// <para>Original prefabricated model</para>
        /// </summary>
        public GameObject Prefab { get; private set; }

        /// <summary>
        /// 缓存的 Transform 组件
        /// <para>Cached Transform component</para>
        /// </summary>
        public Transform CachedTransform { get; private set; }

        /// <summary>
        /// 所属的对象池实例
        /// <para>Owner object pool instance</para>
        /// </summary>
        public IPool<GameObject> OwnerPool { get; private set; }
        
        /// <summary>
        /// 是否处于池中（未激活状态）
        /// <para>Whether the object is in the pool (inactive)</para>
        /// </summary>
        public bool IsInPool { get; internal set; }

        /// <summary>
        /// 是否开启调试模式（记录泄漏堆栈）
        /// <para>Whether debug mode is enabled (record leak stack trace)</para>
        /// </summary>
        public bool OpenDebug { get; private set; }

#if POOL_DEBUG
        /// <summary>
        /// 产生该对象的调用堆栈（调试模式）
        /// <para>Call stack when spawning the object (debug mode)</para>
        /// </summary>
        public string SpawnStackTrace { get; private set; }

        /// <summary>
        /// 产生时的时间戳（调试模式）
        /// <para>Timestamp when spawning the object (debug mode)</para>
        /// </summary>
        public float SpawnTime { get; private set; }
#endif
        
        /// <summary>
        /// 本次进入池的时间（Time.time）
        /// <para>The time of entering the pool this time (Time.time)</para>
        /// </summary>
        internal float IdleEnterTime { get; set; }

        private IPoolable[] _poolables;     // 所有实现 IPoolable 的组件
        
        /// <summary>
        /// 由 GameObjectPool 调用，初始化组件。
        /// <para>It is called by GameObjectPool to initialize the components.</para>
        /// </summary>
        internal void Init(GameObject prefab, bool debug, GameObjectPool owner)
        {
            Prefab = prefab;
            OpenDebug = debug;
            OwnerPool = owner;
            CachedTransform = transform;

            _poolables = GetComponents<IPoolable>();
        }

        /// <summary>
        /// 对象从池中取出时调用（激活前）。
        /// <para>It is called when the object is retrieved from the pool (before activation).</para>
        /// </summary>
        internal void OnSpawn()
        {
            if (OpenDebug && !IsInPool)
                Warning($"[Pool] Spawn on active object: {name}, {this}");

            IsInPool = false;

#if POOL_DEBUG
            if (OpenDebug)
            {
                SpawnTime = Time.time;
                var stack = new StackTrace(2, true);   // 跳过当前方法和 OnSpawn 调用帧
                SpawnStackTrace = stack.ToString();
            }
#endif

            var arr = _poolables;
            if (arr == null)
                return;
            
            foreach (var t in arr)
                t.OnSpawn();
        }

        /// <summary>
        /// 对象放回池中时调用（停用前）。
        /// <para>This method is called when the object is returned to the pool (before being deactivated).</para>
        /// </summary>
        internal void OnDespawn()
        {
            if (IsInPool)
            {
                Warning($"[Pool] Double Return: {name}, {this}");
                return;
            }

            IsInPool = true;

#if POOL_DEBUG
            SpawnStackTrace = null;
#endif

            var arr = _poolables;
            if (arr == null) 
                return;
            
            foreach (var poolable in arr)
            {
                poolable.OnDespawn();
            }
        }

        /// <summary>
        /// 便捷方法：将此对象归还给所属池。
        /// <para>Easy method: Return this object to its corresponding pool.</para>
        /// </summary>
        internal void ReturnToPool()
        {
            if (OwnerPool is GameObjectPool pool)
            {
                pool.Recycle(this);
            }
            else
            {
                D.Error($"[Pool] Unsupported pool type: {OwnerPool?.GetType()}");
            }
        }
        
        // 内部警告日志
        private void Warning(string msg)
        {
            if (OpenDebug)
                D.Warning(msg);
        }
    }
}

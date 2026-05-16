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

namespace EasyFramework.Managers.Pool
{
    /// <summary>
    /// 池化对象组件，每个通过池生成的 GameObject 都会附带此组件。
    /// <para>Pooling object component. Each GameObject generated through the pool will come with this component.</para>
    /// </summary>
    /// <remarks>
    /// <b>重要：不支持运行时动态添加或删除 IPoolable 组件！</b>
    /// 所有需要响应 OnSpawn/OnDespawn 的组件必须在对象实例化（或预制体）时就已挂载，
    /// 因为在 Init 时已缓存所有 IPoolable 组件，后续增删不会被检测到。
    /// 如需动态控制生命周期行为，请使用 IPoolable 内部的状态标志或事件机制。
    /// <para><b>Important: Dynamically adding or removing IPoolable components at runtime is NOT supported!</b>
    /// All components that need to respond to OnSpawn/OnDespawn must be attached when the object is instantiated (or prefab),
    /// because all IPoolable components are cached during Init. Subsequent changes will not be detected.
    /// To dynamically control lifecycle behavior, use internal state flags or events inside IPoolable.</para>
    /// </remarks>
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

        /// <summary>
        /// 对象是否来自对象池（用于判断是否允许回池）
        /// <para>Whether the object comes from an object pool (used to determine whether it is allowed to be returned to the pool)</para>
        /// </summary>
        public bool IsFromPool { get; private set; }

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
        /// <remarks>
        /// 此处会缓存当前对象上的所有 IPoolable 组件，运行时不再更新。
        /// 因此请勿在运行时动态添加/删除实现了 IPoolable 的组件。
        /// <para>This caches all IPoolable components on the current object and will not be updated at runtime.
        /// Therefore, do not dynamically add/remove components implementing IPoolable at runtime.</para>
        /// </remarks>
        internal void Init(GameObject prefab, bool debug, GameObjectPool owner)
        {
            Prefab = prefab;
            OpenDebug = debug;
            OwnerPool = owner;
            CachedTransform = transform;

            _poolables = GetComponents<IPoolable>();
        }

        /// <summary>
        /// 内部方法：设置 IsFromPool 并同步所有 IPoolable 组件。
        /// <para>Internal method: Set IsFromPool and synchronize all IPoolable components.</para>
        /// </summary>
        internal void SetIsFromPool(bool value)
        {
            IsFromPool = value;
            if (_poolables == null) 
                return;
            
            foreach (var poolable in _poolables)
            {
                poolable.IsFromPool = value;
            }
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
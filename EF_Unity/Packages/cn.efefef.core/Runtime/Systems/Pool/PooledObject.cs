/*
 * ================================================
 * Describe:      挂载在池化 GameObject 上的组件，管理状态和生命周期回调。
 * Author:        Alvin5100
 * CreationTime:  2026-04-30 14:33:44
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
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PooledObject : MonoBehaviour
    {
        public GameObject Prefab { get; private set; }                // 原始预制体
        public Transform CachedTransform { get; private set; }        // 缓存的 Transform
        public IPool<GameObject> OwnerPool { get; private set; }      // 所属的 GameObjectPool
        public bool IsInPool { get; internal set; }                   // 是否处于池中（未激活）
        public bool DebugMode { get; private set; }                   // 是否开启调试（记录泄漏堆栈）

#if POOL_DEBUG
        public string SpawnStackTrace { get; private set; }           // 产生该对象的调用堆栈
        public float SpawnTime { get; private set; }                  // 产生时的时间戳
#endif

        private IPoolable[] _poolables;                               // 所有实现 IPoolable 的组件
        /// <summary>
        /// 由 GameObjectPool 调用，初始化组件。
        /// </summary>
        internal void Init(GameObject prefab, bool debug, GameObjectPool owner)
        {
            Prefab = prefab;
            CachedTransform = transform;
            DebugMode = debug;
            OwnerPool = owner;

            _poolables = GetComponents<IPoolable>();
        }

        /// <summary>
        /// 对象从池中取出时调用（激活前）。
        /// </summary>
        internal void OnSpawn()
        {
            if (DebugMode && !IsInPool)
            {
                UnityEngine.Debug.LogWarning($"[Pool] Spawn on active object: {name}", this);
            }

            IsInPool = false;

#if POOL_DEBUG
            if (DebugMode)
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
        /// </summary>
        internal void OnDespawn()
        {
            if (IsInPool)
            {
                if (DebugMode)
                    UnityEngine.Debug.LogWarning($"[Pool] Double Return: {name}", this);
                return;
            }

            IsInPool = true;

#if POOL_DEBUG
            SpawnStackTrace = null;
#endif

            var arr = _poolables;
            if (arr != null)
            {
                for (int i = 0; i < arr.Length; i++)
                    arr[i].OnDespawn();
            }
        }

        /// <summary>
        /// 便捷方法：将此对象归还给所属池。
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
    }
}

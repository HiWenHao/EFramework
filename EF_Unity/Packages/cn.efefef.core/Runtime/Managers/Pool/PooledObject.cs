/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-04-30 14:33:44
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-30 14:33:44
 * ScriptVersion: 0.3
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
    /// 池化对象
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PooledObject : MonoBehaviour
    {
        public GameObject Prefab { get; private set; }
        public Transform CachedTransform { get; private set; }
        public IPool<GameObject> OwnerPool { get; private set; }

        private IPoolable[] _poolables;

        public bool IsInPool { get; internal set; }
        public bool DebugMode { get; private set; }

#if POOL_DEBUG
        public string SpawnStackTrace { get; private set; }
        public float SpawnTime { get; private set; }
#endif

        internal void Init(GameObject prefab, bool debug, GameObjectPool owner)
        {
            Prefab = prefab;
            CachedTransform = transform;
            DebugMode = debug;
            OwnerPool = owner;

            _poolables = GetComponents<IPoolable>();
        }

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
                var stack = new StackTrace(2, true);
                SpawnStackTrace = stack.ToString();
            }
#endif

            var arr = _poolables;
            if (arr != null)
            {
                for (int i = 0; i < arr.Length; i++)
                    arr[i].OnSpawn();
            }
        }

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
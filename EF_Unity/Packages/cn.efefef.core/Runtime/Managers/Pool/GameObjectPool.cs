/*
 * ================================================
 * Describe:      GameObject pool using Stack, with prewarm and max size.
 * Author:        Alvin8412
 * CreationTime:  2026-04-30 14:22:56
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-30 14:22:56
 * ScriptVersion: 0.5
 * ===============================================
 */

#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define POOL_DEBUG
#endif

using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Managers.Pool
{
    public sealed class GameObjectPool : IPool<GameObject>, IClearablePool
    {
        private readonly GameObject _prefab;
        private Transform _root;

        private readonly Stack<PooledObject> _stack;
        private readonly int _maxSize;
        private readonly bool _debug;

        private int _aliveCount;
        private int _activeCount;

        private bool _disposed;

#if POOL_DEBUG
        private readonly HashSet<PooledObject> _active = new();
#endif

        public int Count => _stack.Count;
        public int ActiveCount => _activeCount;
        public int TotalCount => _aliveCount;

        public GameObjectPool(GameObject prefab, int initial, int max, Transform parent, bool debug)
        {
            _prefab = prefab;
            _maxSize = max > 0 ? max : int.MaxValue;
            _debug = debug;

            int init = Mathf.Min(initial > 0 ? initial : 4, _maxSize);
            _stack = new Stack<PooledObject>(init);

            _root = new GameObject($"{prefab.name}_Pool").transform;
            _root.SetParent(parent, false);

            Prewarm(init);
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
                _stack.Push(Create());
        }

        private PooledObject Create()
        {
            var go = Object.Instantiate(_prefab, _root);
            go.SetActive(false);

            var m = go.GetComponent<PooledObject>() ?? go.AddComponent<PooledObject>();
            m.Init(_prefab, _debug, this);
            m.IsInPool = true;

            _aliveCount++;
            return m;
        }

        public GameObject Get(bool isFromPool = true)
        {
            if (_disposed)
            {
                Debug.LogError($"[Pool] Pool disposed: {_prefab.name}");
                return null;
            }

            PooledObject m;

            while (_stack.Count > 0)
            {
                m = _stack.Pop();
                if (m != null)
                {
                    Activate(m);
                    return m.gameObject;
                }
            }

            m = Create();
            Activate(m);
            return m.gameObject;
        }

        private void Activate(PooledObject m)
        {
            var go = m.gameObject;
            go.SetActive(true);
            m.OnSpawn();

            _activeCount++;

#if POOL_DEBUG
            if (_debug) _active.Add(m);
#endif
        }
        
        [System.Obsolete("Use Return(PooledObject) instead. This is slower and allocates.")]
        public void Recycle(GameObject go)
        {
            if (go == null) return;

            var m = go.GetComponent<PooledObject>();
            Recycle(m);
        }
        // ✅ 唯一合法入口（无 GC）
        public void Recycle(PooledObject m)
        {
            if (m == null) return;

            if (_disposed)
            {
                DestroyInternal(m);
                return;
            }

            if (!ReferenceEquals(m.OwnerPool, this))
            {
                if (_debug)
                    Debug.LogWarning($"[Pool] Wrong pool return: {m.name}");
                DestroyInternal(m);
                return;
            }

            m.OnDespawn();

            _activeCount = Mathf.Max(0, _activeCount - 1);

#if POOL_DEBUG
            if (_debug) _active.Remove(m);
#endif

            var go = m.gameObject;
            go.SetActive(false);
            m.CachedTransform.SetParent(_root, false);

            if (_stack.Count < _maxSize)
            {
                _stack.Push(m);
            }
            else
            {
                DestroyInternal(m);
            }
        }

        private void DestroyInternal(PooledObject m)
        {
            if (m == null) return;

#if POOL_DEBUG
            _active.Remove(m);
#endif

            _activeCount = Mathf.Max(0, _activeCount - 1);
            _aliveCount = Mathf.Max(0, _aliveCount - 1);

            Object.Destroy(m.gameObject);
        }

        public void DumpLeaks()
        {
#if POOL_DEBUG
            if (!_debug) return;

            _active.RemoveWhere(x => x == null);

            foreach (var m in _active)
            {
                float t = Time.time - m.SpawnTime;

                Debug.LogError(
                    $"[Pool Leak] {m.name}\nAlive: {t:F2}s\n{m.SpawnStackTrace}",
                    m
                );
            }

            if (_active.Count == 0)
                Debug.Log("[Pool] No leaks.");
#endif
        }

        public void Clear()
        {
            // 防止销毁时继续操作池
            _disposed = true;

            // 销毁池中对象
            while (_stack.Count > 0)
            {
                var m = _stack.Pop();
                if (m != null)
                    DestroyInternal(m);
            }

#if POOL_DEBUG
            // 清空调试追踪的活动对象
            _active.Clear();
#endif

            // 销毁根对象
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
                _root = null;
            }
        }
    }
}
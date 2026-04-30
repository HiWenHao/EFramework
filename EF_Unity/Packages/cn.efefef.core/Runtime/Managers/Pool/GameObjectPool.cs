/*
 * ================================================
 * Describe:      GameObject 对象池，使用栈存储空闲对象，支持预热、最大容量限制和调试泄漏。
 * Author:        Alvin5100
 * CreationTime:  2026-04-30 14:22:56
 * ModifyAuthor:  Alvin5100
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
    /// <summary>
    /// GameObject 对象池，实现 IPool(GameObject)  和 IClearablePool。
    /// </summary>
    public sealed class GameObjectPool : IPool<GameObject>, IClearablePool
    {
        private readonly GameObject _prefab;          // 原始预制体
        private Transform _root;                      // 池中对象的父节点

        private readonly Stack<PooledObject> _stack;  // 空闲对象栈
        private readonly int _maxSize;                // 最大空闲对象数量（超过则直接销毁）
        private readonly bool _debug;                 // 是否开启调试（泄漏跟踪）

        private int _aliveCount;                      // 总存活对象数（包括激活和未激活）
        private int _activeCount;                     // 当前激活对象数

        private bool _disposed;                       // 池是否已被销毁（防止继续操作）

#if POOL_DEBUG
        private readonly HashSet<PooledObject> _active = new();   // 调试用：记录当前激活的对象
#endif

        /// <summary> 空闲对象数量 </summary>
        public int Count => _stack.Count;
        /// <summary> 当前激活的对象数量 </summary>
        public int ActiveCount => _activeCount;
        /// <summary> 总存活对象数量（激活 + 池中闲置） </summary>
        public int TotalCount => _aliveCount;

        /// <summary>
        /// 构造函数，创建并预热池。
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="initial">初始预热数量</param>
        /// <param name="max">最大空闲数量（≤0 表示无上限）</param>
        /// <param name="parent">池对象根节点的父节点</param>
        /// <param name="debug">是否启用调试模式（记录泄漏堆栈）</param>
        public GameObjectPool(GameObject prefab, int initial, int max, Transform parent, bool debug)
        {
            _prefab = prefab;
            _maxSize = max > 0 ? max : int.MaxValue;
            _debug = debug;

            int init = Mathf.Min(initial > 0 ? initial : 4, _maxSize);
            _stack = new Stack<PooledObject>(init);

            // 为这个池创建一个独立的根节点，方便管理
            _root = new GameObject($"{prefab.name}_Pool").transform;
            _root.SetParent(parent, false);

            Prewarm(init);
        }

        /// <summary>
        /// 预先创建指定数量的对象放入池中。
        /// </summary>
        /// <param name="count">预创建数量</param>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
                _stack.Push(Create());
        }

        /// <summary>
        /// 创建一个新实例（不激活）。
        /// </summary>
        private PooledObject Create()
        {
            var go = Object.Instantiate(_prefab, _root);
            go.SetActive(false);

            var m = go.GetComponent<PooledObject>() ?? go.AddComponent<PooledObject>();
            m.Init(_prefab, _debug, this);
            m.IsInPool = true;      // 创建时直接置为池中状态

            _aliveCount++;
            return m;
        }

        /// <summary>
        /// 从池中获取一个对象（激活并返回）。
        /// </summary>
        public GameObject Get(bool isFromPool = true)
        {
            if (_disposed)
            {
                Debug.LogError($"[Pool] Pool disposed: {_prefab.name}");
                return null;
            }

            PooledObject m;

            // 从栈中取，跳过可能为空的引用（理论上不会出现，但防御）
            while (_stack.Count > 0)
            {
                m = _stack.Pop();
                if (m != null)
                {
                    Activate(m);
                    return m.gameObject;
                }
            }

            // 池空则创建
            m = Create();
            Activate(m);
            return m.gameObject;
        }

        /// <summary>
        /// 激活一个池对象（设置 active 为 true，调用 OnSpawn）。
        /// </summary>
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

        /// <summary>
        /// 回收对象（无 GC 版本）。
        /// </summary>
        /// <param name="m">PooledObject 组件</param>
        public void Recycle(PooledObject m)
        {
            if (m == null) return;

            // 池已销毁 → 直接销毁对象
            if (_disposed)
            {
                DestroyInternal(m);
                return;
            }

            // 防止误归还到错误池
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

            // 如果空闲栈容量未满则存入，否则直接销毁
            if (_stack.Count < _maxSize)
            {
                _stack.Push(m);
            }
            else
            {
                DestroyInternal(m);
            }
        }

        /// <summary>
        /// 彻底销毁一个 PooledObject 对应的 GameObject。
        /// </summary>
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

        /// <summary>
        /// 输出所有泄漏的活动对象（仅当 debug = true 且定义了 POOL_DEBUG）。
        /// </summary>
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

        /// <summary>
        /// 清空池：销毁所有空闲对象以及根节点，并标记为 disposed。
        /// 注意：不会销毁当前激活的对象，只是防止它们后续再被回收。
        /// </summary>
        public void Clear()
        {
            _disposed = true;

            // 销毁栈中所有对象
            while (_stack.Count > 0)
            {
                var m = _stack.Pop();
                if (m != null)
                    DestroyInternal(m);
            }

#if POOL_DEBUG
            _active.Clear();
#endif

            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
                _root = null;
            }
        }
    }
}
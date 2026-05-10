/*
 * ================================================
 * Describe:        GameObject 对象池，支持空闲超时自动销毁。
 * Author:          Alvin5100
 * CreationTime:    2026-04-30 14:22:56
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-10 14:23:33
 * ScriptVersion:   0.2
 * ================================================
 */

#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define POOL_DEBUG
#endif

using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Systems.Pool
{
    /// <summary>
    /// GameObject 对象池，实现 IPool(GameObject)  和 IClearablePool。
    /// </summary>
    public sealed class GameObjectPool : IGameObjectPool
    {
        private bool _disposed;                         // 池是否已被销毁（防止继续操作）

        private int _aliveCount;                        // 总存活对象数（包括激活和未激活）
        private int _activeCount;                       // 当前激活对象数

        private float _lastCleanupTime;                 // 上次清理时间
        private readonly float _idleTimeout;            // 闲置超时秒数（ >0 启用，<=0 禁用）

        private Transform _root;                        // 池中对象的父节点
        private readonly GameObject _prefab;            // 原始预制体

        private readonly int _maxSize;                  // 最大空闲对象数量（超过则直接销毁）
        private readonly Stack<PooledObject> _stack;    // 空闲对象栈

        private readonly List<PooledObject> _tempKeepList;          // 临时保存列表

#if POOL_DEBUG
        private readonly HashSet<PooledObject> _active = new();     // 调试用：记录当前激活的对象
        private readonly HashSet<PooledObject> _pooledSet = new();  // 调试用：记录池中空闲对象（用于重复回收检测）
#endif
        /// <summary>
        /// 池是否未被销毁（可用状态）。
        /// <para>Whether the pool is not disposed (available).</para>
        /// </summary>
        public bool IsAlive => !_disposed;
        
        /// <summary>
        /// 是否开启日志（泄漏跟踪）
        /// <para>Whether to enable the log (leak tracking)</para>
        /// </summary>
        public bool OpenDebug { get; set; }
        
        /// <summary>
        /// 空闲对象数量
        /// <para>Number of idle objects</para>
        /// </summary>
        public int Count => _stack.Count;
        
        /// <summary>
        /// 当前激活的对象数量
        /// <para>The current number of activated objects</para>
        /// </summary>
        public int ActiveCount => _activeCount;
        
        /// <summary>
        /// 总存活对象数量（激活 + 池中闲置）
        /// <para>Total number of surviving objects (activated + idle in the pool)</para>
        /// </summary>
        public int TotalCount => _aliveCount;

        /// <summary>
        /// 构造函数，创建并预热池。
        /// <para>Constructor, creates and preheats the pool.</para>
        /// </summary>
        /// <param name="prefab">预制体 Prefab</param>
        /// <param name="initial">初始预热数量
        /// <para>Initial preheating quantity</para></param>
        /// <param name="max">最大空闲数量（≤0 表示无上限）
        /// <para>Maximum idle quantity (≤ 0 indicates no limit)</para></param>
        /// <param name="parent">池对象根节点的父节点
        /// <para>The parent node of the root node of the pool object</para></param>
        /// <param name="idleTimeout">空闲超时销毁时间（秒），≤0 表示不启用
        /// <para>Idle timeout destruction time (seconds), ≤ 0 indicates that it is not enabled.</para></param>
        public GameObjectPool(GameObject prefab, int initial, int max, Transform parent, float idleTimeout = -1f)
        {
            _prefab = prefab;
            _maxSize = max > 0 ? max : int.MaxValue;
            
            _idleTimeout = idleTimeout > 0f ? idleTimeout : -1f;   // -1 表示禁用

            int init = Mathf.Min(initial > 0 ? initial : 4, _maxSize);
            _stack = new Stack<PooledObject>(init);
            _tempKeepList = new List<PooledObject>();

            // 为这个池创建一个独立的根节点，方便管理
            _root = new GameObject($"{prefab.name}_Pool").transform;
            _root.SetParent(parent, false);

            Prewarm(init);
        }

        #region 公共函数
        
        /// <summary>
        /// 预先创建指定数量的对象放入池中。
        /// <para>Pre-create a specified number of objects and place them in the pool.</para>
        /// </summary>
        /// <param name="count">预创建数量
        /// <para>Pre-allocated quantity</para></param>
        public void Prewarm(int count)
        {
            if (_disposed)
            {
                if (OpenDebug) Warning($"[Pool] Cannot Prewarm on disposed pool: {_prefab.name}");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                var obj = Create();
                _stack.Push(obj);
#if POOL_DEBUG
                _pooledSet.Add(obj);
#endif
            }
        }

        /// <summary>
        /// 从池中获取一个对象（激活并返回）。
        /// <para>Obtain an object from the pool (activate and return it).</para>
        /// </summary>
        /// <returns>游戏对象 GameObject</returns>
        public GameObject Get()
        {
            if (_disposed)
            {
                Error($"[Pool] Pool disposed: {_prefab.name}");
                return null;
            }

            if (_stack.TryPop(out var m))
            {
#if POOL_DEBUG
                _pooledSet.Remove(m);
#endif
                Activate(m);
                return m.gameObject;
            }

            m = Create();
            Activate(m);
            return m.gameObject;
        }

        [System.Obsolete("Use Return(PooledObject) instead. This is slower and allocates.")]
        public void Recycle(GameObject go)
        {
            if (go == null)
                return;

            var m = go.GetComponent<PooledObject>();
            Recycle(m);
        }

        /// <summary>
        /// 回收对象（无 GC 版本）。
        /// <para>Recycling target (without GC version).</para>
        /// </summary>
        /// <param name="m">PooledObject 组件</param>
        public void Recycle(PooledObject m)
        {
            if (m == null) 
                return;

            if (!m.IsFromPool)
            {
                if (OpenDebug)
                    Warning($"[Pool] Object not from pool, destroy directly: {m.name}");
                DestroyInternal(m);
                return;
            }

            // 池已销毁 → 直接销毁对象
            if (_disposed)
            {
                DestroyInternal(m);
                return;
            }

            // 防止误归还到错误池
            if (!ReferenceEquals(m.OwnerPool, this))
            {
                if (OpenDebug)
                    Warning($"[Pool] Wrong pool return: {m.name}");
                DestroyInternal(m);
                return;
            }

            m.OnDespawn();

            _activeCount = Mathf.Max(0, _activeCount - 1);

#if POOL_DEBUG
            // 重复回收检测：如果对象已经在 _pooledSet 中，说明已经处于空闲池
            if (!_pooledSet.Add(m))
            {
                if (OpenDebug) Error($"[Pool] Duplicate recycle detected! Object {m.name} is already in the pool.");
                // 根据需求：可以选择直接销毁对象，或者直接返回不处理
                // 这里选择销毁，避免池状态混乱
                DestroyInternal(m);
                return;
            }
            _active.Remove(m);
#endif

            var go = m.gameObject;
            go.SetActive(false);
            m.CachedTransform.SetParent(_root, false);

            // 记录进入池的时间
            m.IdleEnterTime = Time.time;

            // 如果空闲栈容量未满则存入，否则直接销毁
            if (_stack.Count < _maxSize)
                _stack.Push(m);
            else
                DestroyInternal(m);
        }

        /// <summary>
        /// 输出所有泄漏的活动对象（仅当 OpenDebug = true 且定义了 POOL_DEBUG）。
        /// <para>Output all leaked activity objects (only when OpenDebug = true and POOL_DEBUG is defined).</para>
        /// </summary>
        public void DumpLeaks()
        {
#if POOL_DEBUG
            if (!OpenDebug) 
                return;

            _active.RemoveWhere(x => x == null);

            foreach (var m in _active)
            {
                float t = Time.time - m.SpawnTime;
                D.Error($"[Pool Leak] {m.name}\nAlive: {t:F2}s\n{m.SpawnStackTrace}, {m}");
            }

            if (_active.Count == 0)
                Log("[Pool] No leaks.");
#endif
        }

        /// <summary>
        /// 清空池：销毁所有空闲对象以及根节点，并标记为 disposed。注意：不会销毁当前激活的对象，只是防止它们后续再被回收。
        /// <para>Clear Pool: Destroy all idle objects and root nodes, and mark them as disposed.
        /// Note: It will not destroy the currently active objects; it merely prevents them from being recycled in the future.</para>
        /// </summary>
        public void Clear()
        {
            _disposed = true;

            while (_stack.Count > 0)
            {
                var m = _stack.Pop();
                if (m != null) DestroyInternal(m);
            }

#if POOL_DEBUG
            // 注意：不清除 _active，让激活对象仍然可以被泄漏检测
            _pooledSet.Clear();
#endif

            if (_root == null)
                return;
            
            Object.Destroy(_root.gameObject);
            _root = null;
        }

        #endregion

        #region 内部函数

        /// <summary>
        /// 内部预热辅助：创建一个新对象并直接压入空闲栈（不激活）。
        /// <para>Internal preheating helper: create a new object and push it directly to the idle stack (not activated).</para>
        /// </summary>
        internal void CreateOneAndPush()
        {
            if (_disposed) 
                return;
            
            var obj = Create();
            _stack.Push(obj);
#if POOL_DEBUG
            // 注意：这里不要加 _active.Add(obj)，因为对象尚未激活
            _pooledSet.Add(obj);
#endif
        }
        
        /// <summary>
        /// 清理闲置超时的对象（由外部定时调用）。
        /// </summary>
        internal void CleanupIdleObjects(float now)
        {
            if (_idleTimeout <= 0f) 
                return;

            // 复用列表，避免 GC 分配
            _tempKeepList.Clear();

            // 遍历原栈，将未超时的对象暂存
            while (_stack.Count > 0)
            {
                var m = _stack.Pop();
                if (m == null) continue;

                // 空闲超时则销毁，否则重新入栈
                if (now - m.IdleEnterTime > _idleTimeout)
                {
                    DestroyInternal(m);
                }
                else
                {
                    _tempKeepList.Add(m);
                }
            }

            // 将未超时的对象重新压回栈（顺序会被反转，但栈顺序不重要）
            for (int i = _tempKeepList.Count - 1; i >= 0; i--)
            {
                _stack.Push(_tempKeepList[i]);
            }
            _tempKeepList.Clear();
        }

        #endregion

        #region 私有函数

        // 创建一个新实例（不激活），并标记 IsFromPool = true。
        private PooledObject Create()
        {
            var go = Object.Instantiate(_prefab, _root);
            go.SetActive(false);

            var m = go.GetComponent<PooledObject>() ?? go.AddComponent<PooledObject>();
            m.Init(_prefab, OpenDebug, this);
            m.IsInPool = true;
            
            // 标记该对象是由池创建的，之后不再更改
            m.SetIsFromPool(true);
            
            // 初始进入池也记录时间
            m.IdleEnterTime = Time.time;

            _aliveCount++;
            return m;
        }
        
        // 激活一个池对象（设置 active 为 true，调用 OnSpawn）。
        private void Activate(PooledObject m)
        {
            var go = m.gameObject;
            go.SetActive(true);
            m.OnSpawn();

            _activeCount++;

#if POOL_DEBUG
            if (OpenDebug) _active.Add(m);
#endif
        }
        
        // 彻底销毁一个 PooledObject 对应的 GameObject。
        private void DestroyInternal(PooledObject m)
        {
            if (m == null) 
                return;

#if POOL_DEBUG
            _active.Remove(m);
            _pooledSet.Remove(m);
#endif

            if (!m.IsInPool)
                _activeCount = Mathf.Max(0, _activeCount - 1);
            _aliveCount = Mathf.Max(0, _aliveCount - 1);

            Object.Destroy(m.gameObject);
        }

        private void Log(string msg)
        {
            if (OpenDebug)
                D.Log(msg);
        }
        private void Warning(string msg)
        {
            if (OpenDebug)
                D.Warning(msg);
        }
        private void Error(string msg)
        {
            if (OpenDebug)
                D.Error(msg);
        }
        
        #endregion
    }
}
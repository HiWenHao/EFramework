/*
 * ================================================
 * Describe:        GameObject 对象池，支持空闲超时自动销毁。
 * Author:          Alvin5100
 * CreationTime:    2026-04-30 14:22:56
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-25
 * ScriptVersion:   0.3
 * ================================================
 */

#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define POOL_DEBUG
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyFramework.Pool
{
    /// <summary>
    /// GameObject 对象池，实现 IPool(GameObject)  和 IClearablePool。
    /// </summary>
    public sealed class GameObjectPool : IGameObjectPool
    {
        private bool _disposed;                         // 池是否已被销毁（防止继续操作）
        private bool _releaseRequested;                 // 占用者已全部释放，等待活跃对象归零后销毁
        private int _retainCount;                       // 当前占用该池的消费者数量

        private int _aliveCount;                        // 总存活对象数（包括激活和未激活）
        private int _activeCount;                       // 当前激活对象数

        private readonly float _idleTimeout;            // 闲置超时秒数（ >0 启用，<=0 禁用）

        private Transform _root;                        // 池中对象的父节点
        private Transform _sharedRoot;                  // 共享时使用的全局根节点，由管理器注入
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
        /// 是否已请求释放、正在等待活跃对象归零（此状态下池仍可用）。
        /// <para>Whether a release has been requested and the pool awaits zero active objects
        /// (the pool is still usable in this state).</para>
        /// </summary>
        public bool IsReleasePending => _releaseRequested && !_disposed;

        /// <summary>
        /// 当前占用该池的消费者数量。
        /// <para>Number of consumers currently retaining this pool.</para>
        /// </summary>
        public int RetainCount => _retainCount;

        /// <summary>
        /// 池的原始预制体。
        /// <para>The original prefab of this pool.</para>
        /// </summary>
        public GameObject Prefab => _prefab;

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
        /// 池真正销毁时触发，参数为池自身，供管理器摘除注册表条目。
        /// <para>Raised when the pool is actually disposed; the argument is the pool itself,
        /// allowing the manager to drop its registry entry.</para>
        /// </summary>
        public event Action<GameObjectPool> Disposed;

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
        /// 设置共享根节点：池被第二个及以上消费者占用时，根节点会迁到该节点下，避免随任一消费者的层级被销毁。
        /// <para>Set the shared root: once a second consumer retains the pool, the root node is moved under it
        /// so it no longer dies with any single consumer's hierarchy.</para>
        /// </summary>
        /// <param name="sharedRoot">全局共享根节点
        /// <para>Global shared root</para></param>
        public void SetSharedRoot(Transform sharedRoot)
        {
            _sharedRoot = sharedRoot;

            if (_retainCount > 1)
                MoveRootToSharedRoot();
        }

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
                if (_stack.Count >= _maxSize)
                    return;

                var obj = Create();
                if (obj == null)
                    return;

                _stack.Push(obj);
#if POOL_DEBUG
                _pooledSet.Add(obj);
#endif
            }
        }

        /// <summary>
        /// 补足空闲对象到指定数量（不足则创建，已足够则不动）。
        /// <para>Top up idle objects to the target count (creates the gap, no-op when sufficient).</para>
        /// </summary>
        /// <param name="target">目标空闲数量
        /// <para>Target idle count</para></param>
        public void PrewarmTo(int target)
        {
            if (_disposed || target <= 0)
                return;

            int gap = Mathf.Min(target, _maxSize) - _stack.Count;
            if (gap > 0)
                Prewarm(gap);
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

            // 跳过已被外部销毁的残留对象，避免访问已销毁的 GameObject
            PooledObject m;
            while (_stack.TryPop(out m))
            {
                if (m == null)
                {
                    _aliveCount = Mathf.Max(0, _aliveCount - 1);
                    continue;
                }

#if POOL_DEBUG
                _pooledSet.Remove(m);
#endif
                Activate(m);
                return m.gameObject;
            }

            m = Create();
            if (m == null)
            {
                Error($"[Pool] Create failed: {_prefab.name}");
                return null;
            }

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

            // 已处于池中说明是重复归还：再走一遍会重复递减活跃数，并把同一实例二次压栈
            if (m.IsInPool)
            {
                if (OpenDebug)
                    Warning($"[Pool] Double return ignored: {m.name}");
                return;
            }

            RecycleInternal(m);
            CheckPendingDispose();
        }

        // 回收主体：保留原有全部提前返回分支，销毁判定统一放在 Recycle 收尾
        private void RecycleInternal(PooledObject m)
        {
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

            // 根节点已失效（宿主层级被卸载）：对象无处安放，直接销毁
            if (_root == null)
            {
                DestroyInternal(m);
                return;
            }

            // 防止误归还到错误池：转发给真正的归属池，否则会减错本池的活跃计数
            if (!ReferenceEquals(m.OwnerPool, this))
            {
                if (OpenDebug)
                    Warning($"[Pool] Wrong pool return: {m.name}");

                if (m.OwnerPool is GameObjectPool ownerPool)
                {
                    ownerPool.Recycle(m);
                    return;
                }

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
        /// 清空池：只销毁空闲对象，不影响已取出的对象，池保持可用。
        /// <para>Clear the pool: destroys idle objects only, leaves retrieved objects untouched,
        /// and keeps the pool usable.</para>
        /// </summary>
        public void Clear()
        {
            if (_disposed)
                return;

            while (_stack.Count > 0)
            {
                var m = _stack.Pop();
                if (m != null) DestroyInternal(m);
            }

#if POOL_DEBUG
            // 注意：不清除 _active，让激活对象仍然可以被泄漏检测
            _pooledSet.Clear();
#endif
        }

        /// <summary>
        /// 占用池：计数加一，并撤销待销毁标记。
        /// <para>Retain the pool: increments the counter and cancels any pending disposal.</para>
        /// </summary>
        public void Retain()
        {
            if (_disposed)
                return;

            _retainCount++;
            _releaseRequested = false;

            // 出现第二个占用者后，池根不能再依附任何单个消费者的层级
            if (_retainCount > 1)
                MoveRootToSharedRoot();
        }

        /// <summary>
        /// 释放占用：计数减一；减到 0 且无活跃对象时立即销毁，否则等最后一个对象归还时销毁。
        /// <para>Release the hold: decrements the counter and disposes immediately when it reaches zero
        /// with no active object, otherwise when the last active object is recycled.</para>
        /// </summary>
        public void Release()
        {
            if (_disposed)
                return;

            // 没有可释放的占用（重复释放或从未占用）：直接忽略，避免误杀别人的池
            if (_retainCount <= 0)
                return;

            _retainCount--;

            if (_retainCount > 0)
                return;                     // 还有别的消费者，池必须继续可用

            _releaseRequested = true;

            // 已无人在用：连空闲缓存一起销毁；仍有活跃对象则保留缓存，等最后一个归还再统一销毁
            if (_activeCount == 0)
                Dispose();
        }

        /// <summary>
        /// 彻底销毁池：清空空闲对象、销毁根节点并标记为不可用；活跃对象不主动销毁，归还时直接销毁。
        /// <para>Dispose the pool: clears idle objects, destroys the root node and marks the pool unusable;
        /// active objects are not destroyed here and are destroyed when returned.</para>
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            Clear();                        // 必须先于 _disposed 置位：Clear 内部有 disposed 守卫
            _disposed = true;
            _releaseRequested = false;
            _retainCount = 0;

            DestroyRoot();

            Disposed?.Invoke(this);
        }

        #endregion

        #region 内部函数

        /// <summary>
        /// 内部预热辅助：创建一个新对象并直接压入空闲栈（不激活）。
        /// <para>Internal preheating helper: create a new object and push it directly to the idle stack (not activated).</para>
        /// </summary>
        internal void CreateOneAndPush()
        {
            if (_disposed || _stack.Count >= _maxSize)
                return;
            
            var obj = Create();
            if (obj == null)
                return;

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

        // 池被多个消费者共享后，把根节点迁到管理器提供的全局根，避免随任一消费者的层级一起被销毁
        private void MoveRootToSharedRoot()
        {
            if (_sharedRoot == null || _root == null)
                return;
            if (_root.parent == _sharedRoot)
                return;

            _root.SetParent(_sharedRoot, false);
        }

        // 销毁池根节点：仍在根下的活跃对象先脱到场景根，避免被连带销毁
        private void DestroyRoot()
        {
            if (_root == null)
                return;

            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var child = _root.GetChild(i);
                var pooled = child.GetComponent<PooledObject>();
                if (pooled != null && !pooled.IsInPool)
                    child.SetParent(null, true);
            }

            Object.Destroy(_root.gameObject);
            _root = null;
        }

        // 占用者已全部释放且无活跃对象时执行销毁
        private void CheckPendingDispose()
        {
            if (!_releaseRequested || _disposed)
                return;
            if (_activeCount > 0)
                return;

            Dispose();
        }

        // 创建一个新实例（不激活），并标记 IsFromPool = true。
        private PooledObject Create()
        {
            // 根节点已被外部销毁（如宿主层级被卸载）：池已失去宿主，不再实例化
            if (_root == null)
                return null;

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
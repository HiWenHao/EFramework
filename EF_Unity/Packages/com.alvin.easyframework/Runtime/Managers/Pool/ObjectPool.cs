/*
 * ================================================
 * Describe:      This script is used to control the objects frequent creation and destruction, Thanks for LiuHaitao.
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-02 17:02:19
 * ModifyAuthor:  Alvin5100(Wang)
 * ModifyTime:    2025-12-02 17:02:19
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace EasyFramework.Managers.Pool
{
#if UNITY_EDITOR
    /// <summary>
    /// Control the objects frequent creation and destruction
    /// <para>控制对象的频繁创建和销毁</para>
    /// 编辑器模式下会检测对象是否已经在池中，防止不小心多次入池,方便查找问题
    /// </summary>
    public class ObjectPool
    {
        public bool IsUsed { get; private set; }
        public long MaxNum { get; private set; }

        private Stack<object> _poolStack;
        private readonly object _syncRoot = new object();

        public ObjectPool(long maxNum)
        {
            lock (_syncRoot)
            {
                if (IsUsed)
                    return;
                IsUsed = true;

                MaxNum = maxNum;
                _poolStack = new Stack<object>();
            }
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (!IsUsed)
                    return;
                IsUsed = false;

                MaxNum = 0;
                while (_poolStack.Count > 0)
                {
                    object item = _poolStack.Pop();
                    if (item is IDisposable disposable)
                        disposable.Dispose();
                }

                _poolStack.Clear();
                _poolStack = null;
            }
        }

        public T Get<T>() where T : class
        {
            lock (_syncRoot)
            {
                if (!IsUsed)
                    throw new InvalidOperationException("Object pool is not initialized");

                T item;
                if (_poolStack.Count > 0)
                {
                    item = (T)_poolStack.Pop();
                }
                else
                    item = Activator.CreateInstance<T>();
                
                return item;
            }
        }

        public void Recycle(object item)
        {
            if (item == null)
                return;

            lock (_syncRoot)
            {
                if (!IsUsed)
                    throw new InvalidOperationException("Object pool is not initialized");

                if (item is IDisposable disposable)
                    disposable.Dispose();

                if (_poolStack.Count >= MaxNum)
                    return;

                if (_poolStack.Contains(item))
                    throw new Exception("object already in pool: " + item.GetType().FullName);

                _poolStack.Push(item);
            }
        }
    }
#else
    /// <summary>
    /// Thread-safe lock-free object pool, Control the objects frequent creation and destruction
    /// <para>线程安全的无锁对象池, 控制对象的频繁创建和销毁</para>
    /// </summary>
    public class ObjectPool : IDisposable
    {
        public bool IsUsed { get; private set; }
        public long MaxNum { get; private set; }

        private int _canUseCount;   //可用数量
        private int _createdCount;  //已创建总量
        private object _fastItem;   //快速使用对象
        private ConcurrentQueue<object> _poolQueue;

        public ObjectPool(long maxNum)
        {
            if (IsUsed)
                return;
            IsUsed = true;

            MaxNum = maxNum;
            _poolQueue = new ConcurrentQueue<object>();
        }

        public void Dispose()
        {
            if (!IsUsed)
                return;
            IsUsed = false;

            _poolQueue.Clear();
            _poolQueue = null;
        }

        public T Get<T>() where T : class
        {
            object item = _fastItem;
            if (item != null && Interlocked.CompareExchange(ref _fastItem, null, item) == item)
                return (T)item;

            if (_poolQueue.TryDequeue(out object result))
            {
                Interlocked.Decrement(ref _canUseCount);
                return (T)result;
            }

            if (Interlocked.Increment(ref _createdCount) <= MaxNum)
                return Activator.CreateInstance<T>();

            Interlocked.Decrement(ref _createdCount);
            D.Error($"Maximum total objects ({MaxNum}) exceeded for {typeof(T).Name}");
            return null;
        }

        public void Recycle(object item)
        {
            if (item == null)
                return;

            if (Interlocked.CompareExchange(ref _fastItem, item, null) == null)
                return;

            int currentInPool;
            do
            {
                currentInPool = Volatile.Read(ref _canUseCount);
                if (currentInPool >= MaxNum)
                    return;
            } while (Interlocked.CompareExchange(ref _canUseCount, currentInPool + 1, currentInPool) != currentInPool);
            
            if (item is IDisposable disposable)
                disposable.Dispose();
            _poolQueue.Enqueue(item);
        }
    }
#endif
}

/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-04-30 14:22:14
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-30 14:22:14
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace EasyFramework.Managers.Pool
{
    public sealed class ObjectPool<T> : IPool<T>, IClearablePool where T : class
    {
        private readonly ConcurrentStack<T> _stack = new();
        private readonly Func<T> _factory;
        private readonly Action<T> _reset;
        private readonly int _maxSize;

        private int _count;

        public ObjectPool(int maxSize, Func<T> factory, Action<T> reset = null)
        {
            _maxSize = maxSize > 0 ? maxSize : int.MaxValue;
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _reset = reset;
        }

        public T Get(bool isFromPool = true)
        {
            if (!isFromPool || !_stack.TryPop(out var item))
                return _factory();
            
            Interlocked.Decrement(ref _count);
            if (item is IPoolable p)
            {
                p.IsFromPool = true;
            }
            return item;
        }

        public void Recycle(T item)
        {
            if (item == null) return;

            _reset?.Invoke(item);

            if (Interlocked.Increment(ref _count) <= _maxSize)
            {
                _stack.Push(item);
            }
            else
            {
                Interlocked.Decrement(ref _count);
            }
        }

        public void Clear()
        {
            _stack.Clear();
            _count = 0;
        }
    }
}
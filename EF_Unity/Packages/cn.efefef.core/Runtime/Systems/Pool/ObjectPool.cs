/*
 * ================================================
 * Describe:      通用对象池，支持泛型，线程安全，最大空闲数量限制。
 * Author:        Alvin5100
 * CreationTime:  2026-04-30 14:22:14
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-09 23:06:55
 * ScriptVersion:   0.1
 * ===============================================
 */

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace EasyFramework.Systems.Pool
{
    /// <summary>
    /// 通用对象池，可用于任何 class 类型。
    /// 内部使用 ConcurrentStack 实现，可在多线程环境使用。
    /// </summary>
    /// <typeparam name="T">池中对象类型，必须是引用类型</typeparam>
    public sealed class ObjectPool<T> : IPool<T>, IClearablePool where T : class
    {
        private readonly ConcurrentStack<T> _stack;    // 空闲对象栈
        private readonly Func<T> _factory;             // 创建新对象的方法
        private readonly Action<T> _reset;             // 归还时的重置方法（可选）
        private readonly int _maxSize;                 // 最大空闲对象数量

        private int _count;                            // 当前空闲对象数量（近似值）

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="maxSize">最大空闲对象数量，小于等于0表示无上限</param>
        /// <param name="factory">创建新实例的委托</param>
        /// <param name="reset">归还时调用的重置逻辑（例如清空数据）</param>
        public ObjectPool(int maxSize, Func<T> factory, Action<T> reset = null)
        {
            _maxSize = maxSize > 0 ? maxSize : int.MaxValue;
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _reset = reset;
            _stack = new ConcurrentStack<T>();
        }

        /// <summary>
        /// 从池中获取一个对象。如果池中有空闲，则弹出；否则调用 factory 新建。
        /// </summary>
        public T Get(bool isFromPool = true)
        {
            if (_stack.TryPop(out var item))
            {
                Interlocked.Decrement(ref _count);
                return item;
            }
            return _factory();
        }

        /// <summary>
        /// 将对象归还池中。如果当前空闲数量未达上限，则推入栈；否则直接丢弃。
        /// </summary>
        /// <param name="item">要归还的对象</param>
        public void Recycle(T item)
        {
            if (item == null) return;

            _reset?.Invoke(item);

            // 尝试增加空闲计数，如果不超过最大容量则入栈
            if (Interlocked.Increment(ref _count) <= _maxSize)
                _stack.Push(item);
            else
                Interlocked.Decrement(ref _count);
        }

        /// <summary>
        /// 清空池：移除所有空闲对象，重置计数。
        /// </summary>
        public void Clear()
        {
            _stack.Clear();
            _count = 0;
        }
    }
}

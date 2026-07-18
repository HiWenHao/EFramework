/*
 * ================================================
 * Describe:        通用对象池，支持泛型，线程安全，最大空闲数量限制。
 * Author:          Alvin5100
 * CreationTime:    2026-04-30 14:22:14
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-09 23:06:55
 * ScriptVersion:   0.1
 * ===============================================
 */

using System;
using System.Collections.Concurrent;

namespace EasyFramework.Managers.Pool
{
    /// <summary>
    /// 通用对象池，可用于任何 class 类型。内部使用 ConcurrentStack 实现，可在多线程环境使用。
    /// <para>The generic object pool can be used for any class type.
    /// It is implemented internally using ConcurrentStack and can be used in a multithreaded environment.</para>
    /// </summary>
    /// <typeparam name="T">池中对象类型，必须是引用类型<para>The object type in the pool must be a reference type.</para></typeparam>
    public sealed class ObjectPool<T> : IPool<T>, IClearablePool where T : class
    {
        private readonly int _maxSize;                 // 最大空闲对象数量
        private readonly Func<T> _factory;             // 创建新对象的方法
        private readonly Action<T> _reset;             // 归还时的重置方法（可选）
        private readonly ConcurrentStack<T> _stack;    // 空闲对象栈

        /// <summary>
        /// 通用对象池，可用于任何 class 类型。
        /// <para>The generic object pool can be used for any class type.</para>
        /// </summary>
        /// <param name="maxSize">最大空闲对象数量，小于等于0表示无上限
        /// <para>The maximum number of idle objects is less than or equal to 0, which indicates no limit.</para></param>
        /// <param name="factory">创建新实例的委托
        /// <para>The delegate for creating a new instance</para></param>
        /// <param name="reset">归还时调用的重置逻辑（例如清空数据）
        /// <para>The reset logic (such as clearing data) that is invoked upon return</para></param>
        public ObjectPool(int maxSize, Func<T> factory, Action<T> reset = null)
        {
            _maxSize = maxSize > 0 ? maxSize : int.MaxValue;
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _reset = reset;
            _stack = new ConcurrentStack<T>();
        }

        /// <summary>
        /// 从池中获取一个对象。如果池中有空闲，则弹出；否则调用 factory 新建。
        /// <para>Obtain an object from the pool. If there is an available one in the pool, pop it out; otherwise, call the factory to create a new one.</para>
        /// </summary>
        public T Get()
        {
            return _stack.TryPop(out var item) ? item : _factory();
        }

        /// <summary>
        /// 将对象归还池中。如果当前空闲数量未达上限，则推入栈；否则直接丢弃。
        /// <para>Return the object to the pool. If the current number of available objects has not reached the limit, push it onto the stack; otherwise, discard it directly.</para>
        /// </summary>
        /// <param name="item">要归还的对象
        /// <para>The object to be returned</para></param>
        public void Recycle(T item)
        {
            if (item == null)
                return;

            _reset?.Invoke(item);

            // 如果当前空闲数量未达上限，则入栈
            if (_stack.Count < _maxSize)
                _stack.Push(item);
        }

        /// <summary>
        /// 清空池：移除所有空闲对象。
        /// <para>Clear pool: Remove all idle objects.</para>
        /// </summary>
        public void Clear()
        {
            _stack.Clear();
        }
    }
}
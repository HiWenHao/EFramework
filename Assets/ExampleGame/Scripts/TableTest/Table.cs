/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-02-01 17:39:32
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-02-01 17:39:32
 * ScriptVersion: 0.1
 * ===============================================
*/
using System;
using System.Collections;
using System.Collections.Generic;

namespace EFExample
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    public abstract class Table<T> : IEnumerable<T>, IDisposable
    {
		public abstract void Add(T item);
        public abstract IEnumerator<T> Get();
        public abstract void Remove(T item);
        public abstract void Clear();
        public abstract void OnDispose();

        public abstract IEnumerator<T> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Get();

        public void Dispose()
        {
            OnDispose();
        }
    }
}

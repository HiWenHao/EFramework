/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-01-31 16:59:58
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-01-31 16:59:58
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;

namespace EasyFramework
{
    /// <summary>
    /// The event trigger with on value changed.
    /// <para>值改变触发器</para>
    /// </summary>
    public class BindableProperty<T> where T : IEquatable<T>
    {
        private T _value = default;

        /// <summary>
        /// 当前值
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                // value 可能为 null（T 为引用类型时），直接 value.Equals 会 NRE
                if (EqualityComparer<T>.Default.Equals(value, _value))
                    return;
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }

        /// <summary>
        /// 当值发生变化时
        /// </summary>
        public event Action<T> OnValueChanged;
    }
}
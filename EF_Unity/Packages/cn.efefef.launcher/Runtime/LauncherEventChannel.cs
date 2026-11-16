/*
 * ================================================
 * Describe:        Base class for launcher ScriptableObject event channels.
 * Author:          Alvin8412
 * CreationTime:    2026-11-16 11:20:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-16 11:20:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using UnityEngine;

namespace EasyFramework.Launcher
{
    /// <summary>
    /// 启动事件通道基类：订阅只存在于运行时，通道里不序列化任何场景对象引用。
    /// <para>Base class for launcher event channels. Subscriptions are runtime-only; no scene object references are serialized.</para>
    /// </summary>
    /// <typeparam name="TPayload">事件负载类型<para>Event payload type.</para></typeparam>
    public abstract class LauncherEventChannel<TPayload> : ScriptableObject
    {
        private event Action<TPayload> Raised;

        /// <summary>
        /// 广播一次事件。
        /// <para>Raise the event once.</para>
        /// </summary>
        /// <param name="payload">事件负载<para>Event payload.</para></param>
        public void Raise(TPayload payload) => Raised?.Invoke(payload);

        /// <summary>
        /// 订阅事件，务必在 OnDisable 里配对调用 <see cref="Unsubscribe"/>。
        /// <para>Subscribe; always pair with <see cref="Unsubscribe"/> in OnDisable.</para>
        /// </summary>
        /// <param name="listener">订阅回调<para>Listener callback.</para></param>
        public void Subscribe(Action<TPayload> listener)
        {
            if (listener != null)
                Raised += listener;
        }

        /// <summary>
        /// 取消订阅。
        /// <para>Unsubscribe.</para>
        /// </summary>
        /// <param name="listener">订阅回调<para>Listener callback.</para></param>
        public void Unsubscribe(Action<TPayload> listener)
        {
            if (listener != null)
                Raised -= listener;
        }

        private void OnDisable() => Raised = null;
    }
}

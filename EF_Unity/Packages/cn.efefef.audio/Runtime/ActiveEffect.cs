/*
 * ================================================
 * Describe:      记录一条正在播放的音效及其生命周期信息
 * Author:        Alvin8412
 * CreationTime:  2026-05-09 23:55:19
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-09 23:55:19
 * ScriptVersion: 1.0
 * ===============================================
 */

using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Managers.Audio
{
    /// <summary>
    /// 内部类：记录一条正在播放的音效及其生命周期信息。用于在 Update 中检测播放结束、响应外部取消、以及回收到对象池。
    /// <para>Internal class: Records information about a currently playing sound effect and its lifecycle.
    /// Used to detect playback completion in the Update function, respond to external cancellation, and recycle the AudioSource back to the object pool.</para>
    /// </summary>
    internal sealed class ActiveEffect
    {
        /// <summary>
        /// 正在播放的音频ID号
        /// <para>The currently playing audio ID</para>
        /// </summary>
        public uint ID;

        /// <summary>
        /// 当前音频暂停
        /// <para>Current audio is paused.</para>
        /// </summary>
        public bool IsPaused;

        /// <summary>
        /// 当前音效已经被释放
        /// <para>The current sound effect has been released.</para>
        /// </summary>
        public bool IsReleased;

        /// <summary>
        /// 正在播放的音效组件
        /// <para>The currently playing sound effect component</para>
        /// </summary>
        public AudioSource Source;

        /// <summary>
        /// 非循环音效的完成信号。当音效自然播放结束时 TrySetResult，外部 await 可获知。
        /// <para>Completion signal for non-cyclic sound effects. When the sound effect naturally ends,
        /// Function TrySetResult will be triggered, and at this point, the external await can obtain the result.</para>
        /// </summary>
        public UniTaskCompletionSource CompletionSource;

        /// <summary>
        /// 用于注册外部 CancellationToken 取消回调。当外部取消时，停止音效并立即回收 AudioSource。
        /// <para>Used to register the external CancellationToken for the cancellation callback.
        /// When the external cancellation occurs, stop the sound effect and immediately reclaim the AudioSource.</para>
        /// </summary>
        public CancellationTokenRegistration Cancellation;
    }
}
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
    /// 记录一条正在播放的音效及其生命周期状态
    /// <para>Records one playing effect and its lifecycle state</para>
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
        /// 非循环音效的播放完成信号
        /// <para>Completion signal for non-looping effects</para>
        /// </summary>
        public UniTaskCompletionSource CompletionSource;

        /// <summary>
        /// 外部取消令牌的注册回调
        /// <para>Registration of the external cancellation callback</para>
        /// </summary>
        public CancellationTokenRegistration Cancellation;
    }
}
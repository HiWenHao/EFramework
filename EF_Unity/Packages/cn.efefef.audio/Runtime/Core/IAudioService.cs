/*
 * ================================================
 * Describe:      音频服务生命周期契约。四个策略接口统一继承本接口，由 AudioManager 门面驱动。
 * Author:        Alvin8412
 * CreationTime:  2026-11-13 11:20
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-13 11:20
 * ScriptVersion: 1.0
 * ================================================
 */

namespace EasyFramework.Managers.Audio
{
    /// <summary>
    /// 音频服务生命周期接口
    /// <para>Lifecycle contract for audio services</para>
    /// </summary>
    public interface IAudioService
    {
        /// <summary>
        /// 初始化并绑定共享上下文。门面在 ISingleton.Init 内调用。
        /// <para>Initialize with the shared context, called by the facade during Init.</para>
        /// </summary>
        /// <param name="context">共享运行时上下文 <para>Shared runtime context.</para></param>
        void Initialize(AudioContext context);

        /// <summary>
        /// 释放运行时资源并解绑上下文。门面在 ISingleton.Quit 内逆序调用。
        /// <para>Release runtime resources, called by the facade during Quit in reverse order.</para>
        /// </summary>
        void Dispose();
    }

    /// <summary>
    /// 可选逐帧驱动接口
    /// <para>Optional per-frame tick contract</para>
    /// </summary>
    public interface IAudioTickable
    {
        /// <summary>
        /// 逐帧驱动，由门面 IUpdate.Update 转发。
        /// <para>Per-frame tick forwarded from the facade's IUpdate.Update.</para>
        /// </summary>
        /// <param name="elapse">缩放后的帧间隔 <para>Scaled delta time.</para></param>
        /// <param name="realElapse">真实帧间隔 <para>Unscaled delta time.</para></param>
        void Tick(float elapse, float realElapse);
    }
}
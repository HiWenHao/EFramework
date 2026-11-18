/*
 * ================================================
 * Describe:      进入游戏前的确认闸门契约（可选）
 * Author:        Alvin8412
 * CreationTime:  2026-11-18 21:30:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-18 21:30:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Launcher
{
    /// <summary>
    /// 进入游戏前的确认闸门。<b>可选</b>：实现方挂在与 <see cref="Launcher"/> 同一物体或其子物体上即生效，
    /// 未挂载时启动流程跳过等待，行为与原来完全一致。
    /// <para>Optional confirmation gate before the hotfix entry is invoked. Attach an implementation to the
    /// Launcher GameObject (or a child); without one the pipeline proceeds as before.</para>
    /// </summary>
    public interface ILauncherEnterConfirm
    {
        /// <summary>
        /// 已就绪、等待用户确认。<b>用户确认后必须调用 onComplete</b>，否则启动流程会一直停在此处
        /// <para>Called when everything is loaded. onComplete MUST be invoked on user confirmation,
        /// otherwise the pipeline waits forever.</para>
        /// </summary>
        /// <param name="onComplete">确认完成回调</param>
        void OnReadyToEnter(Action onComplete);
    }
}
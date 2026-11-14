/*
 * ================================================
 * Describe:        This script is used to define the project boot hook.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 15:58:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-14 15:58:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;

namespace EasyFramework.Launcher
{
    /// <summary>
    /// 项目侧插入点：隐私协议、渠道 SDK 初始化、登录、埋点等「必须早于资源系统」的逻辑
    /// <para>Project hook for privacy dialog, channel SDK init, login — anything that must run before the resource system.</para>
    /// </summary>
    public interface ILauncherHook
    {
        /// <summary>
        /// 在 YooAsset 初始化之前调用。<b>完成后必须调用 onComplete</b>，否则启动流程会一直等待
        /// <para>Runs before YooAsset initializes. onComplete MUST be invoked, otherwise boot stalls forever.</para>
        /// </summary>
        /// <param name="onComplete">完成回调</param>
        void OnBoot(Action onComplete);
    }
}
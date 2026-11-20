/*
 * ================================================
 * Describe:      Shared context for red dot subsystems - decouples nodes from the facade singleton.
 * Author:        Alvin5100, AI
 * CreationTime:  2026-11-20 14:10:00
 * ModifyAuthor:  Alvin5100, AI
 * ModifyTime:    2026-11-20
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Systems.RedDot
{
    /// <summary>
    /// 红点系统共享上下文：暴露节点与渲染器所需的子系统能力
    /// <para>Red dot shared context: exposes subsystem capabilities needed by nodes and renderers.</para>
    /// <para>门面 RedDotSystem 实现此接口；测试时可注入 fake 实现，无需启动 Unity 单例。</para>
    /// <para>The facade implements this; tests can inject a fake without booting the Unity singleton.</para>
    /// </summary>
    public interface IRedDotContext
    {
        /// <summary>脏节点系统 <para>Dirty node system.</para></summary>
        RedDotDirtySystem DirtySystem { get; }

        /// <summary>事件系统 <para>Event system.</para></summary>
        RedDotEventSystem EventSystem { get; }

        /// <summary>精灵异步加载委托 <para>Async sprite loading delegate.</para></summary>
        Func<string, UniTask<Sprite>> LoadSpriteAsync { get; }

        /// <summary>按键查询节点 <para>Get a node by key.</para></summary>
        RedDotNode GetNode(string key);
    }
}

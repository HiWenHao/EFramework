/*
 * ================================================
 * Describe:      This script is used to publishes node change events.
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 15:12:37
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-13 15:12:37
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Systems.RedDot
{
    /// <summary>
    /// 红点事件系统：发布节点变更事件
    /// <para>Red dot event system - publishes node change events</para>
    /// </summary>
    public class RedDotEventSystem
    {
        /// <summary>
        /// 节点变更事件
        /// <para>Node changed event</para>
        /// </summary>
        public event Action<RedDotNode> OnNodeChanged;

        // 发送节点变更通知
        internal void SendNodeChanged(RedDotNode node)
        {
            OnNodeChanged?.Invoke(node);
        }
    }
}
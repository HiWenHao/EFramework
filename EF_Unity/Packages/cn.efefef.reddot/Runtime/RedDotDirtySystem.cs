/*
 * ================================================
 * Describe:      This script is used to manages nodes that need refresh.
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 15:12:02
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-13 15:12:02
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;

namespace EasyFramework.Systems.RedDot
{
    /// <summary>
    /// 脏节点系统：管理需要刷新的节点集合
    /// <para>Dirty system - manages nodes that need refresh</para>
    /// </summary>
    public class RedDotDirtySystem
    {
        private readonly HashSet<RedDotNode> _dirtyNodes = new(); // 脏节点集合

        /// <summary>
        /// 将节点标记为脏（需要刷新）
        /// <para>Mark node as dirty (needs refresh)</para>
        /// </summary>
        public void MarkDirty(RedDotNode node)
        {
            if (node == null)
                return;
            _dirtyNodes.Add(node);
        }

        /// <summary>
        /// 刷新所有脏节点
        /// <para>Flush all dirty nodes</para>
        /// </summary>
        public void Flush()
        {
            if (_dirtyNodes.Count == 0)
                return;

            var processing = new List<RedDotNode>(_dirtyNodes);
            _dirtyNodes.Clear();

            processing.Sort((a, b) => b.Depth.CompareTo(a.Depth));

            for (int i = 0; i < processing.Count; i++)
            {
                processing[i].Refresh();
            }
        }
    }
}
/*
 * ================================================
 * Describe:      This script is used to manages nodes that need refresh.
 * Author:        Alvin5100, AI
 * CreationTime:  2026-05-13 15:12:02
 * ModifyAuthor:  Alvin5100, AI
 * ModifyTime:    2026-11-20
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
        private readonly List<RedDotNode> _processingBuffer = new(); // 复用缓冲，避免每帧 GC

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
        /// 清空所有脏节点（生命周期退出时调用，避免残留已释放节点的引用）
        /// <para>Clear all dirty nodes (called on teardown to avoid retaining disposed nodes).</para>
        /// </summary>
        public void Clear()
        {
            _dirtyNodes.Clear();
            _processingBuffer.Clear();
        }

        /// <summary>
        /// 刷新所有脏节点
        /// <para>Flush all dirty nodes</para>
        /// </summary>
        public void Flush()
        {
            if (_dirtyNodes.Count == 0)
                return;

            // 复用 buffer，避免每帧分配新 List
            _processingBuffer.Clear();
            foreach (var node in _dirtyNodes)
                _processingBuffer.Add(node);
            _dirtyNodes.Clear();

            _processingBuffer.Sort((a, b) => b.Depth.CompareTo(a.Depth));

            for (int i = 0; i < _processingBuffer.Count; i++)
            {
                _processingBuffer[i].Refresh();
            }

            _processingBuffer.Clear();
        }
    }
}
/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 11:35:35
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-13 11:35:35
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Managers.RedDot
{
    /// <summary>
    /// 红点管理器（单例）
    /// <para>English: Red dot manager (singleton)</para>
    /// </summary>
    public class RedDotManager : MonoBehaviour
    {
        public static RedDotManager Instance;       // 单例实例

        private Dictionary<string, RedDotNode> _nodes;  // 节点字典（key -> node）

        public RedDotDirtySystem DirtySystem { get; private set; }   // 脏系统
        public RedDotBatchSystem BatchSystem { get; private set; }   // 批处理系统
        public RedDotEventSystem EventSystem { get; private set; }   // 事件系统
        public IResourceProvider ResourceProvider { get; private set; } // 资源提供者

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _nodes = new Dictionary<string, RedDotNode>();
            DirtySystem = new RedDotDirtySystem();
            BatchSystem = new RedDotBatchSystem();
            EventSystem = new RedDotEventSystem();
            ResourceProvider = new DefaultResourceProvider();
        }

        private void LateUpdate()
        {
            if (!BatchSystem.IsBatching)
            {
                DirtySystem.Flush();
            }
        }

        /// <summary>
        /// 手动刷新所有脏节点
        /// <para>English: Manually flush all dirty nodes</para>
        /// </summary>
        public void Flush() => DirtySystem.Flush();

        /// <summary>
        /// 注册红点节点
        /// <para>English: Register a red dot node</para>
        /// </summary>
        public RedDotNode RegisterNode(
            string key,
            string parentKey = null,
            RedDotDisplayType displayType = RedDotDisplayType.Dot,
            string imagePath = null)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            if (_nodes.TryGetValue(key, out var exist))
                return exist;

            if (!string.IsNullOrEmpty(parentKey) && !_nodes.ContainsKey(parentKey))
            {
                Debug.LogError($"Parent Node Not Found : {parentKey}");
                return null;
            }

            var node = new RedDotNode(key, displayType, imagePath);
            _nodes.Add(key, node);

            if (!string.IsNullOrEmpty(parentKey))
            {
                node.SetParent(_nodes[parentKey]);
            }

            return node;
        }

        /// <summary>
        /// 获取红点节点
        /// <para>English: Get red dot node by key</para>
        /// </summary>
        public RedDotNode GetNode(string key)
        {
            _nodes.TryGetValue(key, out var node);
            return node;
        }

        /// <summary>
        /// 尝试获取红点节点
        /// <para>English: Try to get red dot node by key</para>
        /// </summary>
        public bool TryGetNode(string key, out RedDotNode node)
        {
            return _nodes.TryGetValue(key, out node);
        }

        /// <summary>
        /// 注销红点节点（及其所有子节点）
        /// <para>English: Unregister red dot node and all its children</para>
        /// </summary>
        public void UnregisterNode(string key)
        {
            if (!_nodes.TryGetValue(key, out var node))
                return;

            var children = new List<RedDotNode>(node.Children);
            for (int i = 0; i < children.Count; i++)
            {
                UnregisterNode(children[i].Key);
            }

            node.Dispose();
            _nodes.Remove(key);
        }

        private void OnDestroy()
        {
            foreach (var node in _nodes.Values)
            {
                node.Dispose();
            }
            _nodes.Clear();
        }
    }
}

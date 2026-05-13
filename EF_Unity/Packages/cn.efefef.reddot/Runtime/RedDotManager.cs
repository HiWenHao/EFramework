/*
 * ================================================
 * Describe:      This script is used to manage red dot nodes, including registration, unregistration, dirty flushing, and batching.
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 11:35:35
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-13 11:35:35
 * ScriptVersion: 0.1
 * ================================================
 */

using System.Collections.Generic;

namespace EasyFramework.Managers.RedDot
{
    /// <summary>
    /// 红点管理器（单例）
    /// <para>Red dot manager (singleton)</para>
    /// </summary>
    public class RedDotManager : MonoSingleton<RedDotManager>, IManager
    {
        /// <summary>
        /// 脏系统，负责收集和刷新被标记为脏的红点节点。
        /// <para>Dirty system, responsible for collecting and flushing red dot nodes marked as dirty.</para>
        /// </summary>
        public RedDotDirtySystem DirtySystem { get; private set; }

        /// <summary>
        /// 批处理系统，用于控制是否在批处理模式下延迟刷新。
        /// <para>Batch system, used to control whether to delay flushing while in batching mode.</para>
        /// </summary>
        public RedDotBatchSystem BatchSystem { get; private set; }

        /// <summary>
        /// 事件系统，负责红点变更事件的发布和订阅。
        /// <para>Event system, responsible for publishing and subscribing to red dot change events.</para>
        /// </summary>
        public RedDotEventSystem EventSystem { get; private set; }

        /// <summary>
        /// 资源提供者，用于加载红点图标等资源。
        /// <para>Resource provider, used to load red dot icons and other resources.</para>
        /// </summary>
        public IResourceProvider ResourceProvider { get; private set; }

        /// <summary>
        /// 节点字典，存储所有已注册的红点节点（key -> node）。
        /// <para>Node dictionary, stores all registered red dot nodes (key -> node).</para>
        /// </summary>
        private Dictionary<string, RedDotNode> _nodes;

        public void Init()
        {
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

        public void Quit()
        {
            foreach (var node in _nodes.Values)
            {
                node.Dispose();
            }
            _nodes.Clear();
        }

        /// <summary>
        /// 注册红点节点。
        /// <para>Registers a red dot node.</para>
        /// </summary>
        /// <param name="key">节点的唯一标识键 <para>Unique key of the node</para></param>
        /// <param name="parentKey">父节点的键（可选） <para>Key of the parent node (optional)</para></param>
        /// <param name="displayType">红点显示类型 <para>Red dot display type</para></param>
        /// <param name="imagePath">红点图标资源路径（可选） <para>Resource path of the red dot icon (optional)</para></param>
        /// <returns>注册成功返回节点实例，失败返回null <para>Returns the node instance on success, null on failure</para></returns>
        public RedDotNode RegisterNode(string key, string parentKey = null,
            RedDotDisplayType displayType = RedDotDisplayType.Dot, string imagePath = null)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            if (_nodes.TryGetValue(key, out var exist))
                return exist;

            if (!string.IsNullOrEmpty(parentKey) && !_nodes.ContainsKey(parentKey))
            {
                D.Error($"Parent Node Not Found : {parentKey}");
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
        /// 根据键获取红点节点。
        /// <para>Gets the red dot node by key.</para>
        /// </summary>
        /// <param name="key">节点的唯一标识键 <para>Unique key of the node</para></param>
        /// <returns>节点实例，如果不存在则返回null <para>Node instance, or null if not found</para></returns>
        public RedDotNode GetNode(string key)
        {
            _nodes.TryGetValue(key, out var node);
            return node;
        }

        /// <summary>
        /// 尝试根据键获取红点节点。
        /// <para>Tries to get the red dot node by key.</para>
        /// </summary>
        /// <param name="key">节点的唯一标识键 <para>Unique key of the node</para></param>
        /// <param name="node">输出节点实例 <para>Output node instance</para></param>
        /// <returns>是否成功获取节点 <para>Whether the node was successfully obtained</para></returns>
        public bool TryGetNode(string key, out RedDotNode node)
        {
            return _nodes.TryGetValue(key, out node);
        }

        /// <summary>
        /// 注销红点节点及其所有子节点。
        /// <para>Unregisters the red dot node and all its children.</para>
        /// </summary>
        /// <param name="key">节点的唯一标识键 <para>Unique key of the node</para></param>
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

        /// <summary>
        /// 开始批处理
        /// <para>Begin batch processing</para>
        /// </summary>
        public void BeginBatch()
        {
            BatchSystem.Begin();
        }

        /// <summary>
        /// 结束批处理，若深度归零则触发刷新
        /// <para>End batch processing, trigger flush when depth reaches zero</para>
        /// </summary>
        public void EndBatch()
        {
            BatchSystem.End();
        }
        
        /// <summary>
        /// 手动刷新所有脏节点（忽略批处理状态）。
        /// <para>Manually flushes all dirty nodes (ignores batch status).</para>
        /// </summary>
        public void Flush() => DirtySystem.Flush();
    }
}
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

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Systems.RedDot
{
    /// <summary>
    /// 红点管理器（单例）
    /// <para>Red dot manager (singleton)</para>
    /// </summary>
    public class RedDotSystem : MonoSingleton<RedDotSystem>, ISingleton
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
        /// 异步加载精灵图片的委托，用户可替换为自定义加载逻辑（如 Addressables、AssetBundle 等）。
        /// <para>Delegate for asynchronous sprite loading. User can replace with custom logic (e.g. Addressables, AssetBundle).</para>
        /// </summary>
        public Func<string, UniTask<Sprite>> LoadSpriteAsync { get; private set; }

        /// <summary>
        /// 节点字典，存储所有已注册的红点节点（key -> node）。
        /// <para>Node dictionary, stores all registered red dot nodes (key -> node).</para>
        /// </summary>
        private Dictionary<string, RedDotNode> _nodes;

        /// <summary>
        /// 获取所有已注册的红点节点（只读）。未初始化时返回 null。
        /// <para>Gets all registered red dot nodes (read-only). Returns null before initialization.</para>
        /// </summary>
        public IReadOnlyCollection<RedDotNode> Nodes => _nodes?.Values;

        private bool _subsystemsSet; //  判断是否已经注册子系统
        private readonly Dictionary<string, Sprite> _spriteCache = new(); // 简单缓存

        void ISingleton.Init()
        {
            _nodes = new Dictionary<string, RedDotNode>();
            LoadSpriteAsync = DefaultLoadSpriteAsync;
        }

        private void LateUpdate()
        {
            if (_subsystemsSet && !BatchSystem.IsBatching)
            {
                DirtySystem.Flush();
            }
        }

        void ISingleton.Quit()
        {
            foreach (var node in _nodes.Values)
            {
                node.Dispose();
            }

            _nodes.Clear();
            _spriteCache.Clear();
        }

        // 确保子系统准备就绪
        private bool EnsureSubsystemsReady()
        {
            if (_subsystemsSet) return true;
            D.Error("[ RedDotManager ] Subsystems not set. Call SetupNewSystem before using red dot features. Operation ignored.");
            return false;
        }

        // 默认Resources加载
        private async UniTask<Sprite> DefaultLoadSpriteAsync(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (_spriteCache.TryGetValue(path, out var cached) && cached != null)
                return cached;

            var request = Resources.LoadAsync<Sprite>(path);
            await request;
            var sprite = request.asset as Sprite;
            if (sprite != null)
                _spriteCache[path] = sprite;
            return sprite;
        }

        #region 系统设置

        /// <summary>
        /// 初始化子系统（脏系统、批处理系统、事件系统）。参数为 null 时将自动创建默认实例。
        /// <para>Initialize the subsystems (dirty, batch, event). If a parameter is null, a default instance will be created automatically.</para>
        /// </summary>
        /// <param name="dirtySystem">新的脏系统（为 null 则创建默认）<para>New dirty system (creates default if null)</para></param>
        /// <param name="batchSystem">新的批处理系统（为 null 则创建默认）<para>New batch system (creates default if null)</para></param>
        /// <param name="eventSystem">新的事件系统（为 null 则创建默认）<para>New event system (creates default if null)</para></param>
        public void InitSubsystem(RedDotDirtySystem dirtySystem = null, RedDotBatchSystem batchSystem = null,
            RedDotEventSystem eventSystem = null)
        {
            if (_subsystemsSet)
            {
                D.Error("[RedDotManager] Subsystems have already been set. Re-setting may cause state loss. Skipped.");
                return;
            }

            _subsystemsSet = true;

            DirtySystem = dirtySystem ?? new RedDotDirtySystem();
            BatchSystem = batchSystem ?? new RedDotBatchSystem();
            EventSystem = eventSystem ?? new RedDotEventSystem();
        }

        /// <summary>
        /// 设置资源加载代理器
        /// <para>Set up resource loading proxy</para>
        /// </summary>
        /// <param name="spriteLoader">资源加载代理器<para>Resource proxy</para></param>
        public void SetResourceProvider(Func<string, UniTask<Sprite>> spriteLoader)
        {
            LoadSpriteAsync = spriteLoader;
        }

        #endregion

        /// <summary>
        /// 清理默认加载的全部图片缓存, 如果你通过“SetResourceProvider”函数重新设置了加载器，那么此函数将不会对该图像进行清理操作。
        /// <para>Clear all the cached images that are loaded by default.
        /// If you newly set a loader through the SetResourceProvider function, the image will not be cleaned up by this function.</para>
        /// </summary>
        public void ClearSpriteCache()
        {
            _spriteCache.Clear();
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
            if (!EnsureSubsystemsReady() || string.IsNullOrEmpty(key))
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
            if (!EnsureSubsystemsReady()) return null;
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
            if (EnsureSubsystemsReady())
                return _nodes.TryGetValue(key, out node);

            node = null;
            return false;
        }

        /// <summary>
        /// 注销红点节点及其所有子节点。
        /// <para>Unregisters the red dot node and all its children.</para>
        /// </summary>
        /// <param name="key">节点的唯一标识键 <para>Unique key of the node</para></param>
        public void UnregisterNode(string key)
        {
            if (!EnsureSubsystemsReady()) return;
            if (!_nodes.TryGetValue(key, out var node))
                return;

            // 迭代式收集所有子孙节点（含自身），避免递归每层分配 List
            var stack = new Stack<RedDotNode>();
            stack.Push(node);
            var collected = new List<RedDotNode>();
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                collected.Add(current);
                for (int i = 0; i < current.Children.Count; i++)
                {
                    stack.Push(current.Children[i]);
                }
            }

            // 逆序销毁：子节点先于父节点（DFS 先序收集的逆序即为后序）
            for (int i = collected.Count - 1; i >= 0; i--)
            {
                collected[i].Dispose();
                _nodes.Remove(collected[i].Key);
            }
        }

        /// <summary>
        /// 开始批处理
        /// <para>Begin batch processing</para>
        /// </summary>
        public void BeginBatch()
        {
            if (!EnsureSubsystemsReady()) return;
            BatchSystem.Begin();
        }

        /// <summary>
        /// 结束批处理，若深度归零则触发刷新
        /// <para>End batch processing, trigger flush when depth reaches zero</para>
        /// </summary>
        public void EndBatch()
        {
            if (!EnsureSubsystemsReady()) return;
            BatchSystem.End();
        }

        /// <summary>
        /// 手动刷新所有脏节点（忽略批处理状态）。
        /// <para>Manually flushes all dirty nodes (ignores batch status).</para>
        /// </summary>
        public void Flush()
        {
            if (!EnsureSubsystemsReady()) return;
            DirtySystem.Flush();
        }
    }
}
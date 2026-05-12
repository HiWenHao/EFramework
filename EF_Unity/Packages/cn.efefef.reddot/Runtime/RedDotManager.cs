/*
 * ================================================
 * Describe:      空点系统管理器
 * Author:        Alvin5100
 * CreationTime:  2026-05-12 17:38:43
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-12 17:38:43
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Managers.RedDot
{
    /// <summary>
    /// 红点系统管理器
    /// <para>Red dot system manager</para>
    /// </summary>
    public class RedDotManager : MonoSingleton<RedDotManager>, ISingleton
    {
        private Dictionary<string, RedDotNode> _allNodes;

        void ISingleton.Init()
        {
            _allNodes = new Dictionary<string, RedDotNode>();
        }

        void ISingleton.Quit()
        {
            _allNodes.Clear();
            _allNodes = null;
        }

        /// <summary>
        /// 注册一个节点
        /// </summary>
        /// <param name="key">当前红点: 键 - 名字<para>The current red dot: key - name</para></param>
        /// <param name="parentKey">当前红点父节点<para>The parent node of current red dot</para></param>
        /// <param name="type">红点展示类型 <para>Red dot display type</para></param>
        /// <param name="imagePath">当前节点要展示的图片地址 <para>The address of the image to be displayed at the current node</para></param>
        /// <returns>节点 - Node</returns>
        public RedDotNode RegisterNode(string key, string parentKey = null,
            RedDotDisplayType type = RedDotDisplayType.Dot, string imagePath = null)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            if (_allNodes.TryGetValue(key, out var registerNode))
                return registerNode;

            var node = new RedDotNode(key, type, imagePath);
            _allNodes[key] = node;

            if (!string.IsNullOrEmpty(parentKey))
                SetParent(key, parentKey);

            return node;
        }

        /// <summary>
        /// 设置父节点
        /// <para>Set patent node.</para>
        /// </summary>
        /// <param name="childKey">子节点名</param>
        /// <param name="parentKey">父节点名</param>
        public void SetParent(string childKey, string parentKey)
        {
            if (!_allNodes.TryGetValue(childKey, out var child))
            {
                D.Error(
                    $"[ RedDotManager ]: Current child node [ {childKey} ] is not exist, you should first create a new one.");
                return;
            }

            if (!_allNodes.TryGetValue(parentKey, out var parent))
            {
                D.Error($"[ RedDotManager ]: The parent node [ {parentKey} ] is not exist.");
                return;
            }

            child.SetParent(parent);
        }

        /// <summary>
        /// 节点从父节点中注销自己
        /// <para>The node logs itself out from the parent node.</para>
        /// </summary>
        /// <param name="key">节点键 - 名字<para>Node key - Name</para></param>
        public void UnregisterNode(string key)
        {
            if (!_allNodes.TryGetValue(key, out var node)) return;
            node.RemoveFromParent();
            _allNodes.Remove(key);
        }

        /// <summary>
        /// 一键已读全部节点（将所有节点Number置0）
        /// <para>One-click mark all nodes as read (set the Number of all nodes to 0)</para>
        /// </summary>
        public void ReadAll()
        {
            foreach (var node in _allNodes.Values)
            {
                if (node.Number == 0) continue;
                node.SetNumber(0);
            }
        }

        /// <summary>
        /// 获取节点
        /// <para>Get a node by key</para>
        /// </summary>
        /// <param name="key">节点键 - 名字<para>Node key - Name</para></param>
        /// <returns></returns>
        public RedDotNode GetNode(string key)
        {
            _allNodes.TryGetValue(key, out var node);
            return node;
        }
        
        /// <summary>
        /// 获取节点
        /// <para>Get a node by key</para>
        /// </summary>
        /// <param name="key">节点键 - 名字<para>Node key - Name</para></param>
        /// <param name="node">节点 - Node</param>
        public bool TryGetNode(string key, out RedDotNode node)
        {
            return _allNodes.TryGetValue(key, out node);
        }

        // 调试：打印树状结构
        public void DumpTree(string rootKey = null)
        {
            if (string.IsNullOrEmpty(rootKey))
            {
                // 找到所有根节点
                foreach (var node in _allNodes.Values)
                    if (node.Parent == null)
                        DumpNode(node, 0);
            }
            else if (_allNodes.TryGetValue(rootKey, out var root))
            {
                DumpNode(root, 0);
            }
        }

        private void DumpNode(RedDotNode node, int depth)
        {
            Debug.Log(new string('-', depth) + node.Key + $"({node.Number}, {node.DisplayType})");
            foreach (var child in node.Children) DumpNode(child, depth + 1);
        }
    }
}
/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 15:11:19
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-13 15:11:19
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;

namespace EasyFramework.Managers.RedDot
{
    /// <summary>
    /// 红点树节点
    /// <para>English: Red dot tree node</para>
    /// </summary>
    public class RedDotNode : IDisposable
    {
        public string Key { get; private set; }                     // 节点唯一标识
        public int Number { get; private set; }                     // 当前数值（已应用）
        public string ImagePath { get; private set; }               // 图片路径（仅 Image/ImageNumber 使用）
        public RedDotDisplayType DisplayType { get; private set; }  // 显示类型
        public RedDotNode Parent { get; private set; }              // 父节点
        
        private List<RedDotNode> _children = new();                 // 子节点列表
        public IReadOnlyList<RedDotNode> Children => _children;     // 子节点只读访问

        public event Action<RedDotNode> OnValueChanged;             // 数值变更事件

        private bool _disposed;          // 是否已释放
        private int _pendingNumber;       // 暂存的待应用数值
        private bool _hasPendingNumber;   // 是否有待应用的数值

        /// <summary>
        /// 节点深度（根节点深度为0）
        /// <para>English: Node depth (root depth = 0)</para>
        /// </summary>
        public int Depth
        {
            get
            {
                int depth = 0;
                var current = Parent;
                while (current != null)
                {
                    depth++;
                    current = current.Parent;
                }
                return depth;
            }
        }

        /// <summary>
        /// 构造函数
        /// <para>English: Constructor</para>
        /// </summary>
        public RedDotNode(string key, RedDotDisplayType displayType, string imagePath = null)
        {
            Key = key;
            DisplayType = displayType;
            ImagePath = imagePath;
            _children = new List<RedDotNode>();
        }

        /// <summary>
        /// 设置节点数值（实际生效在本帧的 LateUpdate 或手动 Flush 时）
        /// <para>English: Set node number (will be applied in LateUpdate or manual Flush)</para>
        /// </summary>
        public void SetNumber(int number)
        {
            if (DisplayType == RedDotDisplayType.Dot)
                number = number > 0 ? 1 : 0;
            if (Number == number) return;
    
            _pendingNumber = number;
            _hasPendingNumber = true;
            RedDotManager.Instance.DirtySystem.MarkDirty(this);
        }

        // 刷新节点：应用 pending 数值 或 聚合子节点数值
        internal void Refresh()
        {
            // 先应用暂存的数值（优先于子节点聚合）
            if (_hasPendingNumber)
            {
                Number = _pendingNumber;
                _hasPendingNumber = false;
                NotifyValueChanged();
            }
            else if (_children.Count > 0)
            {
                int total = 0;
                foreach (var child in _children)
                {
                    if (child.DisplayType == RedDotDisplayType.Dot)
                        total += child.Number > 0 ? 1 : 0;
                    else
                        total += child.Number;
                }
                int newNumber = total;
                if (DisplayType == RedDotDisplayType.Dot)
                    newNumber = newNumber > 0 ? 1 : 0;
                if (Number != newNumber)
                {
                    Number = newNumber;
                    NotifyValueChanged();
                }
            }
    
            if (Parent != null)
                RedDotManager.Instance.DirtySystem.MarkDirty(Parent);
        }

        /// <summary>
        /// 设置父节点
        /// <para>English: Set parent node</para>
        /// </summary>
        public void SetParent(RedDotNode parent)
        {
            if (Parent == parent)
                return;

            if (parent != null && parent.IsChildOf(this))
            {
                UnityEngine.Debug.LogError($"RedDot Cycle Detected : {Key}");
                return;
            }

            Parent?._children.Remove(this);
            Parent = parent;
            if (parent != null && !parent._children.Contains(this))
            {
                parent._children.Add(this);
            }
        }

        // 检查当前节点是否是 target 的子孙节点（防止循环引用）
        private bool IsChildOf(RedDotNode target)
        {
            var current = Parent;
            while (current != null)
            {
                if (current == target)
                    return true;
                current = current.Parent;
            }
            return false;
        }

        /// <summary>
        /// 从父节点移除自身
        /// <para>English: Remove itself from parent</para>
        /// </summary>
        public void RemoveFromParent()
        {
            Parent?._children.Remove(this);
            Parent = null;
        }

        // 通知数值变更
        private void NotifyValueChanged()
        {
            OnValueChanged?.Invoke(this);
            RedDotManager.Instance.EventSystem.SendNodeChanged(this);
        }

        /// <summary>
        /// 释放节点资源
        /// <para>English: Dispose node resources</para>
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            RemoveFromParent();
            if (_children != null)
            {
                var childrenCopy = new List<RedDotNode>(_children);
                foreach (var child in childrenCopy)
                {
                    child.RemoveFromParent();
                }
                _children.Clear();
            }
            Parent = null;
            OnValueChanged = null;
            _pendingNumber = 0;
            _hasPendingNumber = false;
        }
    }
}
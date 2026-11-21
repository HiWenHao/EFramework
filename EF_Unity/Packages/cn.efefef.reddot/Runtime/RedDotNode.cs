/*
 * ================================================
 * Describe:      This script is used to define the red dot tree node.
 * Author:        Alvin5100, AI
 * CreationTime:  2026-05-13 15:11:19
 * ModifyAuthor:  Alvin5100, AI
 * ModifyTime:    2026-11-20
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Systems.RedDot
{
    /// <summary>
    /// 红点树节点
    /// <para>Red dot tree node</para>
    /// </summary>
    public class RedDotNode : IDisposable
    {
        /// <summary>
        /// 节点唯一标识
        /// <para>Unique key of the node</para>
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// 当前数值（已应用）
        /// <para>Current number (already applied)</para>
        /// </summary>
        public int Number { get; private set; }

        /// <summary>
        /// 图片路径（仅 Image/ImageNumber 使用）
        /// <para>Image path (used only for Image/ImageNumber display type)</para>
        /// </summary>
        public string ImagePath { get; private set; }

        /// <summary>
        /// 显示类型
        /// <para>Display type</para>
        /// </summary>
        public RedDotDisplayType DisplayType { get; private set; }

        /// <summary>
        /// 父节点
        /// <para>Parent node</para>
        /// </summary>
        public RedDotNode Parent { get; private set; }

        /// <summary>
        /// 子节点只读列表
        /// <para>Read-only list of child nodes</para>
        /// </summary>
        public IReadOnlyList<RedDotNode> Children => _children;

        /// <summary>
        /// 数值变更事件
        /// <para>Event triggered when the number changes</para>
        /// </summary>
        public event Action<RedDotNode> OnValueChanged;

        /// <summary>
        /// 节点深度（根节点深度为0）
        /// <para>Node depth (root depth = 0)</para>
        /// </summary>
        public int Depth { get; private set; }

        internal bool IsDisposed { get; private set; }  // 是否已释放 供脏系统在 Flush 时跳过已释放节点
        private int _pendingNumber;                     // 暂存的待应用数值
        private bool _hasPendingNumber;                 // 是否有待应用的数值
        private bool _hasOverride;                      // 是否被显式设值（压制子节点聚合）
        private readonly List<RedDotNode> _children;    // 子节点列表
        private readonly IRedDotContext _context;       // 子系统上下文（可注入，默认回退门面单例）

        /// <summary>
        /// 红点树节点
        /// <para>Red dot tree node</para>
        /// </summary>
        /// <param name="key">节点唯一标识 <para>Unique key of the node</para></param>
        /// <param name="displayType">显示类型 <para>Display type</para></param>
        /// <param name="imagePath">图片路径（可选，仅 Image/ImageNumber 使用） <para>Image path (optional, used only for Image/ImageNumber display type)</para></param>
        /// <param name="context">子系统上下文（null 则回退门面单例，便于测试注入） <para>Subsystem context (null falls back to the facade singleton, for test injection)</para></param>
        public RedDotNode(string key, RedDotDisplayType displayType, string imagePath = null,
            IRedDotContext context = null)
        {
            Key = key;
            DisplayType = displayType;
            ImagePath = imagePath;
            _children = new List<RedDotNode>();
            _context = context;
        }

        // 延迟解析上下文：构造时未注入则首次访问时取门面单例
        private IRedDotContext Context => _context ?? RedDotSystem.Instance;

        /// <summary>
        /// 精灵异步加载委托（透传自上下文，供渲染器使用）
        /// <para>Async sprite loader forwarded from context, for renderers.</para>
        /// </summary>
        public Func<string, UniTask<Sprite>> LoadSpriteAsync => Context.LoadSpriteAsync;

        /// <summary>
        /// 设置节点数值（实际生效在本帧的 LateUpdate 或手动 Flush 时）
        /// <para><c>警告：所以在执行完这个函数后，立即调用 RedDotNode.Number，那么你得到的结果可能会是错误的。</c></para>
        /// <para>Set node number (will be applied in LateUpdate or manual Flush)</para>
        /// <para><c>Warning: So,immediately call RedDotNode.Number after this function, and the result you get might be incorrect.</c></para>
        /// </summary>
        /// <param name="number">要设置的数值 <para>Number to set</para></param>
        public void SetNumber(int number)
        {
            if (DisplayType == RedDotDisplayType.Dot)
                number = number > 0 ? 1 : 0;

            // 优先与 pending 值比较，避免同帧多次调用时丢失更新
            int compareValue = _hasPendingNumber ? _pendingNumber : Number;
            if (compareValue == number) return;

            _pendingNumber = number;
            _hasPendingNumber = true;
            Context.DirtySystem.MarkDirty(this);
        }

        /// <summary>
        /// 强制设置数值并压制子节点聚合（用于表达"父节点强制态"，如强制关闭/强制常亮）
        /// <para>Force a number and suppress child aggregation (for a forced parent state, e.g. force-off/force-on).</para>
        /// <para>与 SetNumber 不同：此值不会被子节点变化覆盖，直到调用 ClearOverride。</para>
        /// <para>Unlike SetNumber, this value is not overwritten by child changes until ClearOverride is called.</para>
        /// </summary>
        /// <param name="number">要强制设置的数值 <para>Number to force</para></param>
        public void SetOverride(int number)
        {
            SetNumber(number);
            _hasOverride = true;
        }

        /// <summary>
        /// 清除显式设值覆盖，使节点恢复为聚合子节点数值
        /// <para>Clear the explicit-value override so the node resumes aggregating its children.</para>
        /// </summary>
        public void ClearOverride()
        {
            if (!_hasOverride)
                return;

            _hasOverride = false;
            // 用当前聚合目标重算：标记自身为脏，下一帧 Flush 时走聚合分支
            Context.DirtySystem.MarkDirty(this);
        }

        // 刷新节点：应用 pending 数值 或 聚合子节点数值
        internal void Refresh()
        {
            bool changed = false;

            // 先应用暂存的数值（优先于子节点聚合）
            if (_hasPendingNumber)
            {
                Number = _pendingNumber;
                _hasPendingNumber = false;
                changed = true;
                NotifyValueChanged();
            }
            else if (!_hasOverride && _children.Count > 0)
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
                    changed = true;
                    NotifyValueChanged();
                }
            }

            // 只有值真正变化时才标记父节点为脏，避免不必要的级联刷新
            if (changed && Parent != null)
                Context.DirtySystem.MarkDirty(Parent);
        }

        /// <summary>
        /// 设置父节点
        /// <para>Set parent node</para>
        /// </summary>
        /// <param name="parent">新的父节点 <para>New parent node</para></param>
        public void SetParent(RedDotNode parent)
        {
            if (Parent == parent)
                return;

            if (parent == this || (parent != null && parent.IsChildOf(this)))
            {
                D.Error($"RedDot Cycle Detected : {Key}");
                return;
            }

            var oldParent = Parent;
            oldParent?._children.Remove(this);
            Parent = parent;
            if (parent != null && !parent._children.Contains(this))
            {
                parent._children.Add(this);
            }

            // 更新自身及所有子节点的深度缓存
            int newDepth = parent != null ? parent.Depth + 1 : 0;
            UpdateDepth(newDepth);

            // 标记旧父和新父为脏，使下次 Flush 时重新聚合子节点数值
            if (oldParent != null)
                Context.DirtySystem.MarkDirty(oldParent);
            if (parent != null)
                Context.DirtySystem.MarkDirty(parent);
        }

        // 递归更新自身及所有子节点的深度缓存（仅在 SetParent 时调用）
        private void UpdateDepth(int newDepth)
        {
            int delta = newDepth - Depth;
            if (delta == 0) return;

            Depth = newDepth;
            for (int i = 0; i < _children.Count; i++)
            {
                _children[i].UpdateDepth(_children[i].Depth + delta);
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
        /// <para>Remove itself from parent</para>
        /// </summary>
        public void RemoveFromParent()
        {
            Parent?._children.Remove(this);
            Parent = null;
            UpdateDepth(0);
        }

        // 通知数值变更
        private void NotifyValueChanged()
        {
            OnValueChanged?.Invoke(this);
            Context.EventSystem.SendNodeChanged(this);
        }

        /// <summary>
        /// 释放节点资源
        /// <para>Dispose node resources</para>
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            // 从脏集合移除自身：否则已释放节点会残留在 DirtySystem 中，下次 Flush 仍持有其引用（即使 Flush 有 IsDisposed 防御，
            // 也不应长期持有）。Quit 流程中 Context 仍指向有效门面，安全。
            var dirty = Context?.DirtySystem;
            if (dirty != null)
                dirty.UnmarkDirty(this);

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
            _hasOverride = false;
        }
    }
}
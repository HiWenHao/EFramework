/*
 * ================================================
 * Describe:      红点系统单个节点
 * Author:        Alvin5100
 * CreationTime:  2026-05-12 17:57:26
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-12 17:57:26
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;

namespace EasyFramework.Managers.RedDot
{
    /// <summary>
    /// 红点系统单个节点
    /// <para>Red dot system single node</para>
    /// </summary>
    public class RedDotNode
    {
        /// <summary>
        /// 当前红点: 键 - 名字
        /// <para>The current red dot: key - name</para>
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// 当前红点展示类型
        /// <para>Current red dot display type</para>
        /// </summary>
        public RedDotDisplayType DisplayType { get; private set; }

        /// <summary>
        /// 红点计数值（0表示无红点）
        /// <para>The count of red dots (0 indicates no red dots)</para>
        /// </summary>
        public int Number { get; private set; }

        /// <summary>
        /// 当前节点要展示的图片地址
        /// <para>The address of the image to be displayed at the current node</para>
        /// </summary>
        public string ImagePath { get; set; }

        /// <summary>
        /// 启用动画播放
        /// <para>Enable animation playback</para>
        /// </summary>
        public bool EnableAnimation { get; set; } = false;

        /// <summary>
        /// 当节点的Number发生变化时触发
        /// <para>It triggers when the Number of the node changes</para>
        /// </summary>
        public event Action<RedDotNode> OnValueChanged;

        /// <summary>
        /// 当前节点的父节点
        /// <para>The parent node of the current node</para>
        /// </summary>
        public RedDotNode Parent { get; private set; }

        /// <summary>
        /// 当前节点的全部子节点
        /// <para>All the child nodes of the current node</para>
        /// </summary>
        public IReadOnlyList<RedDotNode> Children => _childrenNode;

        private readonly List<RedDotNode> _childrenNode = new(); // 全部子节点

        /// <summary>
        /// 创建一个节点
        /// </summary>
        internal RedDotNode(string key, RedDotDisplayType type = RedDotDisplayType.Dot, string imagePath = null)
        {
            Key = key;
            DisplayType = type;
            ImagePath = imagePath;
            Number = 0;
        }

        //设置父节点
        internal void SetParent(RedDotNode parent)
        {
            if (Parent == parent || null == parent) return;
            RemoveFromParent();
            Parent = parent;
            parent._childrenNode.Add(this);
            parent.RefreshNumberFromChildren();
        }

        #region 公开函数
        
        /// <summary>
        /// 从父节点中移除自身
        /// <para>Remove oneself from the parent node</para>
        /// </summary>
        public void RemoveFromParent()
        {
            if (Parent == null) return;
            Parent._childrenNode.Remove(this);
            Parent.RefreshNumberFromChildren();
            Parent = null;
        }

        /// <summary>
        /// 设置红点数值
        /// <para>Set the red dot value</para>
        /// </summary>
        /// <param name="value">具体的数量<para>Specific number</para></param>
        public void SetNumber(int value)
        {
            if (Number == value) return;
            Number = Math.Max(0, value);
            OnValueChanged?.Invoke(this);

            // 向上通知父节点重新计算
            Parent?.RefreshNumberFromChildren();
        }

        /// <summary>
        /// 增加红点数值（data可为负数）
        /// <para>Increase the value of the red dot (the value can be a negative number)</para>
        /// </summary>
        /// <param name="value">要增加或减少的数量<para>The quantity to be increased or decreased</para></param>
        public void AddNumber(int value)
        {
            SetNumber(Number + value);
        }

        #endregion

        // 根据子节点数据刷新自身Number（求和模式）
        private void RefreshNumberFromChildren()
        {
            int newNumber = 0;
            foreach (var child in _childrenNode)
            {
                // 若子节点显示类型为Dot，将其Number>0视为1参与求和；否则直接加Number
                if (child.DisplayType == RedDotDisplayType.Dot)
                    newNumber += child.Number > 0 ? 1 : 0;
                else
                    newNumber += child.Number;
            }

            // 若自身显示类型为Dot，求和结果仅用于判断是否>0（保持Boolean语义）
            if (DisplayType == RedDotDisplayType.Dot)
                newNumber = newNumber > 0 ? 1 : 0;

            if (Number != newNumber)
            {
                Number = newNumber;
                OnValueChanged?.Invoke(this);
                Parent?.RefreshNumberFromChildren();
            }
        }
    }
}
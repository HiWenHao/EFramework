/*
 * ================================================
 * Describe:      红点渲染器抽象基类，所有渲染器继承此类
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 15:10:18
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-23 23:16:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Systems.RedDot
{
    /// <summary>
    /// 红点渲染器基类：所有渲染器继承此类
    /// <para>Base class for all red dot renderers</para>
    /// <para>RedDotView 会调用列表中所有渲染器的 Render，每个渲染器内部根据 node.Number 自行决定显隐</para>
    /// </summary>
    public abstract class RedDotRendererBase : MonoBehaviour
    {
        /// <summary>
        /// 渲染红点节点（每个渲染器内部根据 node.Number 决定显示或隐藏）
        /// <para>Render the red dot node (each renderer decides visibility based on node.Number)</para>
        /// </summary>
        public abstract UniTask Render(RedDotNode node);

        /// <summary>
        /// 隐藏渲染器
        /// <para>Hide the renderer</para>
        /// </summary>
        public abstract void Hide();
    }
}

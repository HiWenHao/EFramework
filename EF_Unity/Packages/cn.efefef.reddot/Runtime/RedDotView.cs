/*
 * ================================================
 * Describe:      This script is used to attach to UI element to bind and render red dot node.
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 15:14:18
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-23 23:16:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework.Edit;
using UnityEngine;

namespace EasyFramework.Systems.RedDot
{
    /// <summary>
    /// 红点视图组件：挂载到UI元素上，自动绑定红点节点并渲染
    /// <para>Red dot view component - attach to UI element to bind and render red dot node</para>
    /// </summary>
    public class RedDotView : MonoBehaviour
    {
        [HeaderPro("红点节点 key", "Red dot node key")]
        [SerializeField] private string key;

        [HeaderPro("渲染器列表", "Renderer list (all renderers are called on value change)")]
        [SerializeField] private List<RedDotRendererBase> renderers;

        private RedDotNode _node; // 绑定的红点节点
        private readonly List<RedDotRendererBase> _activeRenderers = new(); // 运行时生效的渲染器列表

        private void Start()
        {
            Initialize().Forget();
        }

        private void OnDestroy()
        {
            if (_node != null)
            {
                _node.OnValueChanged -= Refresh;
            }
        }

        // 异步初始化
        private async UniTaskVoid Initialize()
        {
            await UniTask.Yield();
            _node = RedDotSystem.Instance.GetNode(key);
            if (_node == null)
            {
                D.Error($"RedDot Node Not Found : {key}");
                return;
            }

            // 将序列化列表中的渲染器加入运行时列表（过滤空值和重复）
            if (renderers != null)
            {
                foreach (var r in renderers)
                {
                    if (r != null && !_activeRenderers.Contains(r))
                        _activeRenderers.Add(r);
                }
            }

            _node.OnValueChanged += Refresh;
            await RefreshAsync(_node);
        }

        // 刷新事件回调
        private void Refresh(RedDotNode node)
        {
            RefreshAsync(node).Forget();
        }

        // 异步刷新所有渲染器（每个渲染器内部根据 node.Number 自行决定显隐）
        private async UniTask RefreshAsync(RedDotNode node)
        {
            foreach (var render in _activeRenderers)
            {
                try { await render.Render(node); }
                catch (System.Exception e) { D.Error($"[RedDotView] {render} Render failed: {e.Message}"); }
            }
        }

        /// <summary>
        /// 设置红点节点的键
        /// <para>Set the key of the red dot node</para>
        /// <para>若已初始化（渲染器已注册），会自动解绑旧节点并绑定新节点</para>
        /// <para>If already initialized, automatically unbinds old node and binds new node</para>
        /// </summary>
        /// <param name="newKey">新的节点键 <para>New node key</para></param>
        public void SetKey(string newKey)
        {
            if (key == newKey) return;

            // 若已绑定节点，先取消旧订阅
            if (_node != null)
            {
                _node.OnValueChanged -= Refresh;
                _node = null;
            }

            key = newKey;

            if (_activeRenderers.Count <= 0) return;
            _node = RedDotSystem.Instance.GetNode(key);
            if (_node == null)
            {
                D.Error($"RedDot Node Not Found : {key}");
                return;
            }

            _node.OnValueChanged += Refresh;
            RefreshAsync(_node).Forget();
        }

        /// <summary>
        /// 手动添加渲染器
        /// <para>Manually add a renderer</para>
        /// </summary>
        /// <param name="render">要添加的渲染器<para>Renderer to add</para>
        /// </param>
        public void AddRenderer(RedDotRendererBase render)
        {
            if (render == null || _activeRenderers.Contains(render)) return;
            _activeRenderers.Add(render);
        }

        /// <summary>
        /// 手动移除渲染器
        /// <para>Manually remove a renderer</para>
        /// </summary>
        /// <param name="render">要移除的渲染器 <para>Renderer to remove</para></param>
        public void RemoveRenderer(RedDotRendererBase render)
        {
            if (render == null) return;
            _activeRenderers.Remove(render);
        }
    }
}

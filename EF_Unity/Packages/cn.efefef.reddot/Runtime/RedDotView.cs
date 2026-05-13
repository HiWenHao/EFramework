/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 15:14:18
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-13 15:14:18
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Managers.RedDot
{
    /// <summary>
    /// 红点视图组件：挂载到UI元素上，自动绑定红点节点并渲染
    /// <para>English: Red dot view component - attach to UI element to bind and render red dot node</para>
    /// </summary>
    public class RedDotView : MonoBehaviour
    {
        [SerializeField] private string key;                    // 红点节点 key
        [SerializeField] private MonoBehaviour dotRenderer;     // Dot 渲染器组件
        [SerializeField] private MonoBehaviour numberRenderer;  // Number 渲染器组件
        [SerializeField] private MonoBehaviour imageRenderer;   // Image 渲染器组件

        private readonly List<IRedDotRenderer> _allRenderers = new();                     // 所有渲染器
        private readonly Dictionary<RedDotDisplayType, List<IRedDotRenderer>> _rendererMap = new();  // 按类型分组
        private RedDotNode _node;    // 绑定的红点节点

        private void Start()
        {
            Initialize().Forget();
        }

        // 异步初始化
        private async UniTaskVoid Initialize()
        {
            _node = RedDotManager.Instance.GetNode(key);
            if (_node == null)
            {
                Debug.LogError($"RedDot Node Not Found : {key}");
                return;
            }

            // 注册单一类型的渲染器
            RegisterRenderer(RedDotDisplayType.Dot, dotRenderer);
            RegisterRenderer(RedDotDisplayType.Number, numberRenderer);
            RegisterRenderer(RedDotDisplayType.Image, imageRenderer);

            // 为 ImageNumber 类型注册组合渲染器（图片 + 数字）
            RegisterRenderer(RedDotDisplayType.ImageNumber, imageRenderer);
            RegisterRenderer(RedDotDisplayType.ImageNumber, numberRenderer);

            _node.OnValueChanged += Refresh;
            await RefreshAsync(_node);
        }

        // 注册渲染器到类型映射
        private void RegisterRenderer(RedDotDisplayType type, MonoBehaviour mono)
        {
            if (mono == null) return;
            if (mono is not IRedDotRenderer render)
            {
                Debug.LogError($"{mono.name} does not implement IRedDotRenderer");
                return;
            }

            if (!_rendererMap.TryGetValue(type, out var list))
            {
                list = new List<IRedDotRenderer>();
                _rendererMap[type] = list;
            }
            if (!list.Contains(render))
                list.Add(render);

            if (!_allRenderers.Contains(render))
                _allRenderers.Add(render);
        }

        // 刷新事件回调
        private void Refresh(RedDotNode node)
        {
            RefreshAsync(node).Forget();
        }

        // 异步刷新所有渲染器
        private async UniTask RefreshAsync(RedDotNode node)
        {
            // 先隐藏所有可能用到的渲染器
            foreach (var render in _allRenderers)
            {
                render.Hide();
            }

            // 找到当前类型对应的所有渲染器，依次渲染
            if (_rendererMap.TryGetValue(node.DisplayType, out var renderers))
            {
                foreach (var render in renderers)
                {
                    await render.Render(node);
                }
            }
        }

        private void OnDestroy()
        {
            if (_node != null)
            {
                _node.OnValueChanged -= Refresh;
            }
        }
    }
}
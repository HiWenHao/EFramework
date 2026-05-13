/*
 * ================================================
 * Describe:      This script is used to attach to UI element to bind and render red dot node.
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 15:14:18
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-13 15:14:18
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework.Edit;
using UnityEngine;

namespace EasyFramework.Managers.RedDot
{
    /// <summary>
    /// 红点视图组件：挂载到UI元素上，自动绑定红点节点并渲染
    /// <para>Red dot view component - attach to UI element to bind and render red dot node</para>
    /// </summary>
    public class RedDotView : MonoBehaviour
    {
        [HeaderPro("红点节点 key", "Red dot node key")]
        [SerializeField] private string key;

        [HeaderPro("点红点渲染器组件", "Dot render component")]
        [SerializeField] private MonoBehaviour dotRenderer;

        [HeaderPro("数字红点渲染器组件", "Number render component")]
        [SerializeField] private MonoBehaviour numberRenderer;

        [HeaderPro("图片红点渲染器组件", "Image render component")]
        [SerializeField] private MonoBehaviour imageRenderer;

        private RedDotNode _node; // 绑定的红点节点
        private readonly List<IRedDotRenderer> _allRenderers = new(); // 所有渲染器
        private readonly Dictionary<RedDotDisplayType, List<IRedDotRenderer>> _rendererMap = new(); // 按类型分组

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
            _node = RedDotManager.Instance.GetNode(key);
            if (_node == null)
            {
                D.Error($"RedDot Node Not Found : {key}");
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
                D.Error($"{mono.name} does not implement IRedDotRenderer");
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
        
        /// <summary>
        /// 设置红点节点的键
        /// <para>Set the key of the red dot node</para>
        /// </summary>
        /// <param name="newKey">新的节点键 <para>New node key</para></param>
        public void SetKey(string newKey) => key = newKey;

        /// <summary>
        /// 设置点红点渲染器（Dot类型）
        /// <para>Set dot red dot render (Dot type)</para>
        /// </summary>
        /// <param name="render">实现了 IRedDotRenderer 的 MonoBehavior <para>MonoBehavior implementing IRedDotRenderer</para></param>
        public void SetDotRenderer(IRedDotRenderer render)
        {
            if (render is not MonoBehaviour mono)
            {
                D.Error("The red dot renderer you want to set up is not MonoBehaviour");
                return;
            }
            dotRenderer = mono;
        }

        /// <summary>
        /// 设置数字红点渲染器（Number类型）
        /// <para>Set number red dot render (Number type)</para>
        /// </summary>
        /// <param name="render">实现了 IRedDotRenderer 的 MonoBehavior <para>MonoBehavior implementing IRedDotRenderer</para></param>
        public void SetNumberRenderer(IRedDotRenderer render)
        {
            if (render is not MonoBehaviour mono)
            {
                D.Error("The red dot renderer you want to set up is not MonoBehaviour");
                return;
            }
            numberRenderer = mono;
        }

        /// <summary>
        /// 设置图片红点渲染器（Image类型或ImageNumber类型共用图片部分）
        /// <para>Set image red dot render (for Image or ImageNumber display type)</para>
        /// </summary>
        /// <param name="render">实现了 IRedDotRenderer 的 MonoBehavior <para>MonoBehavior implementing IRedDotRenderer</para></param>
        public void SetImageRenderer(IRedDotRenderer render)
        {
            if (render is not MonoBehaviour mono)
            {
                D.Error("The red dot renderer you want to set up is not MonoBehaviour");
                return;
            }
            imageRenderer = mono;
        }
    }
}
/*
 * ================================================
 * Describe:      Red dot number renderer using TextMeshProUGUI.
 * Author:        Alvin5100, AI
 * CreationTime:  2026-11-20 14:20:00
 * ModifyAuthor:  Alvin5100, AI
 * ModifyTime:    2026-11-20
 * ScriptVersion: 0.1
 * ===============================================
 */

using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace EasyFramework.RedDot
{
    /// <summary>
    /// 红点数字渲染器（TextMeshPro 版），超过 maxDisplay 显示 "max+"
    /// <para>Number renderer using TextMeshPro; shows "max+" beyond maxDisplay.</para>
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TMPNumberRenderer : RedDotRendererBase
    {
        // 数字上限，超过显示 "99+"
        private const int MaxDisplay = 99;

        [HeaderPro("显示数字的文本组件", "Text component displaying numbers")]
        [SerializeField] private TextMeshProUGUI text;

#if UNITY_EDITOR
        private void Reset()
        {
            text = GetComponent<TextMeshProUGUI>();
        }
#endif

        /// <summary>
        /// 渲染数字节点
        /// <para>Render the number node.</para>
        /// </summary>
        /// <param name="node">红点节点 <para>Red dot node</para></param>
        public override UniTask Render(RedDotNode node)
        {
            bool active = node.Number > 0;
            text.gameObject.SetActive(active);
            if (active)
                text.text = node.Number > MaxDisplay ? MaxDisplay + "+" : node.Number.ToString();

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 隐藏数字显示
        /// <para>Hide the number display.</para>
        /// </summary>
        public override void Hide() => text.gameObject.SetActive(false);
    }
}

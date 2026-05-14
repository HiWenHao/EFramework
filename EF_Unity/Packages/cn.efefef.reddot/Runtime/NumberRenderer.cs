/*
 * ================================================
 * Describe:      This script is used to renderer for Number type.
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 15:13:30
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-13 15:13:30
 * ScriptVersion: 0.1
 * ===============================================
 */

using Cysharp.Threading.Tasks;
using EasyFramework.Edit;
using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.Systems.RedDot
{
    /// <summary>
    /// 红点渲染器：显示数字（Number类型）
    /// <para>Red dot renderer for Number type</para>
    /// </summary>
    [RequireComponent(typeof(Text))]
    [RequireComponent(typeof(RedDotView))]
    public class NumberRenderer : MonoBehaviour, IRedDotRenderer
    {
        [HeaderPro("显示数字的文本组件","Text component displaying numbers")]
        [SerializeField] private Text text;

#if UNITY_EDITOR
        private void Reset()
        {
            text = GetComponent<Text>();
        }
#endif

        /// <summary>
        /// 渲染数字节点
        /// <para>Render number node</para>
        /// </summary>
        public UniTask Render(RedDotNode node)
        {
            bool active = node.Number > 0;
            text.gameObject.SetActive(active);
            if (active)
            {
                text.text = node.Number > 99 ? "99+" : node.Number.ToString();
            }

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 隐藏数字显示
        /// <para>Hide number display</para>
        /// </summary>
        public void Hide()
        {
            text.gameObject.SetActive(false);
        }
    }
}
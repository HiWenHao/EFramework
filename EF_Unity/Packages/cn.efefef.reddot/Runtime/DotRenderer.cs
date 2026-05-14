/*
 * ================================================
 * Describe:      This script is used to renderer for Dot type (show/hide).
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 15:12:59
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-13 15:12:59
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
    /// 红点渲染器：显示/隐藏红点（Dot类型）
    /// <para>Red dot renderer for Dot type (show/hide)</para>
    /// </summary>
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(RedDotView))]
    public class DotRenderer : MonoBehaviour, IRedDotRenderer
    {
        [HeaderPro("显示红点的目标物体", "The target object with a red dot displayed")]
        [SerializeField] private GameObject target;

#if UNITY_EDITOR
        private void Reset()
        {
            target = GetComponent<Image>().gameObject;
        }
#endif

        /// <summary>
        /// 渲染红点节点
        /// <para>Render red dot node</para>
        /// </summary>
        public UniTask Render(RedDotNode node)
        {
            target.SetActive(node.Number > 0);
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 隐藏红点
        /// <para>Hide red dot</para>
        /// </summary>
        public void Hide()
        {
            target.SetActive(false);
        }
    }
}
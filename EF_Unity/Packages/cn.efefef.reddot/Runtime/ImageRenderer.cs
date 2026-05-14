/*
 * ================================================
 * Describe:      This script is used to renderer for Image type.
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 15:13:47
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-13 15:13:47
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
    /// 红点渲染器：显示图片（Image类型）
    /// <para>Red dot renderer for Image type</para>
    /// </summary>
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(RedDotView))]
    public class ImageRenderer : MonoBehaviour, IRedDotRenderer
    {
        [HeaderPro("显示图片的组件", "Component for displaying images")]
        [SerializeField] private Image image;

        private int _renderVersion = 0; // 渲染版本号（防止异步错位）

#if UNITY_EDITOR
        private void Reset()
        {
            image = GetComponent<Image>();
        }
#endif

        /// <summary>
        /// 渲染图片节点
        /// <para>Render image node</para>
        /// </summary>
        public async UniTask Render(RedDotNode node)
        {
            int currentVersion = ++_renderVersion;
            bool active = node.Number > 0;
            image.gameObject.SetActive(active);
            if (!active) return;
            if (string.IsNullOrEmpty(node.ImagePath)) return;

            var sprite = await RedDotSystem.Instance.LoadSpriteAsync(node.ImagePath);
            if (currentVersion != _renderVersion) return; // 旧的请求被丢弃
            image.sprite = sprite;
        }

        /// <summary>
        /// 隐藏图片显示
        /// <para>Hide image display</para>
        /// </summary>
        public void Hide() => image.gameObject.SetActive(false);
    }
}
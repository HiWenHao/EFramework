/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 15:13:47
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-13 15:13:47
 * ScriptVersion: 0.1
 * ===============================================
 */

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.Managers.RedDot
{
	/// <summary>
	/// 红点渲染器：显示图片（Image类型）
	/// <para>English: Red dot renderer for Image type</para>
	/// </summary>
	public class ImageRenderer : MonoBehaviour, IRedDotRenderer
	{
		[SerializeField] private Image image;           // 显示图片的组件
		private int _renderVersion = 0;                 // 渲染版本号（防止异步错位）

		/// <summary>
		/// 渲染图片节点
		/// <para>English: Render image node</para>
		/// </summary>
		public async UniTask Render(RedDotNode node)
		{
			int currentVersion = ++_renderVersion;
			bool active = node.Number > 0;
			image.gameObject.SetActive(active);
			if (!active) return;
			if (string.IsNullOrEmpty(node.ImagePath)) return;

			var sprite = await RedDotManager.Instance.ResourceProvider.LoadSpriteAsync(node.ImagePath);
			if (currentVersion != _renderVersion) return; // 旧的请求被丢弃
			image.sprite = sprite;
		}

		/// <summary>
		/// 隐藏图片显示
		/// <para>English: Hide image display</para>
		/// </summary>
		public void Hide() => image.gameObject.SetActive(false);
	}
}
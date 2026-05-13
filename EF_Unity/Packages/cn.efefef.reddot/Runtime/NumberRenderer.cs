/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 15:13:30
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-13 15:13:30
 * ScriptVersion: 0.1
 * ===============================================
 */

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.Managers.RedDot
{
	/// <summary>
	/// 红点渲染器：显示数字（Number类型）
	/// <para>English: Red dot renderer for Number type</para>
	/// </summary>
	public class NumberRenderer : MonoBehaviour, IRedDotRenderer
	{
		[SerializeField] private Text text;  // 显示数字的文本组件

		/// <summary>
		/// 渲染数字节点
		/// <para>English: Render number node</para>
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
		/// <para>English: Hide number display</para>
		/// </summary>
		public void Hide()
		{
			text.gameObject.SetActive(false);
		}
	}
}
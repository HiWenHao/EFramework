/*
 * ================================================
 * Describe:      红点系统渲染接口
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 15:10:18
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-13 15:10:18
 * ScriptVersion: 0.1
 * ===============================================
 */

using Cysharp.Threading.Tasks;

namespace EasyFramework.Managers.RedDot
{
	/// <summary>
	/// 红点渲染器接口
	/// <para>English: Interface for red dot renderer</para>
	/// </summary>
	public interface IRedDotRenderer
	{
		/// <summary>
		/// 渲染红点节点
		/// <para>English: Render the red dot node</para>
		/// </summary>
		UniTask Render(RedDotNode node);

		/// <summary>
		/// 隐藏渲染器
		/// <para>English: Hide the renderer</para>
		/// </summary>
		void Hide();
	}
}

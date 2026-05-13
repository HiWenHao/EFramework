/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 15:10:36
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-13 15:10:36
 * ScriptVersion: 0.1
 * ===============================================
 */

using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Managers.RedDot
{
	/// <summary>
	/// 资源提供者接口，用于异步加载图片等资源
	/// <para>English: Resource provider interface, used to load assets asynchronously</para>
	/// </summary>
	public interface IResourceProvider
	{
		/// <summary>
		/// 异步加载精灵图片
		/// <para>English: Load sprite asynchronously</para>
		/// </summary>
		UniTask<Sprite> LoadSpriteAsync(string path);
	}
}
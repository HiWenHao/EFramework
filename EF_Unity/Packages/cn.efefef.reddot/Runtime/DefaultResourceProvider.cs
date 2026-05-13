/*
 * ================================================
 * Describe:      This script is used to Resources.Load and caching.
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 15:10:58
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-13 15:10:58
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Managers.RedDot
{
    /// <summary>
    /// 默认资源提供者，使用 Resources 加载并缓存
    /// <para>Default resource provider using Resources.Load and caching</para>
    /// </summary>
    public class DefaultResourceProvider : IResourceProvider
    {
        private readonly Dictionary<string, Sprite> _cache = new(); // 图片缓存

        /// <summary>
        /// 异步加载精灵图片
        /// <para>Load sprite asynchronously</para>
        /// <param name="path">图片路径<para>Image path</para></param>
        /// </summary>
        public async UniTask<Sprite> LoadSpriteAsync(string path)
        {
            if (_cache.TryGetValue(path, out var cached) && cached != null)
                return cached;
            var request = Resources.LoadAsync<Sprite>(path);
            await request;
            var sprite = request.asset as Sprite;
            if (sprite != null) _cache[path] = sprite;
            return sprite;
        }
    }
}
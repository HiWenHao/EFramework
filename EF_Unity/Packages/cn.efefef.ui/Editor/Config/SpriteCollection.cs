/*
 * ================================================
 * Describe:      This script is used to collect the sprite.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-03-31 15:17:32
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-03-31 15:17:32
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

namespace EasyFramework.Edit.SpriteTools
{
    /// <summary>
    /// Collect the sprite and control it.
    /// </summary>
    public class SpriteCollection : ScriptableObject
    {
        [SerializeField] [HideInInspector] string _atlasFolder;
        public string AtlasFolder => _atlasFolder;

        [SerializeField] [HideInInspector] List<Object> _objects = new List<Object>();
        public List<Object> TargetObjects => _objects;

        [SerializeField] [HideInInspector] List<SpriteAtlas> _atlas = new List<SpriteAtlas>();

        public List<SpriteAtlas> Atlas => _atlas;
    }
}
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
    [CreateAssetMenu(fileName = "SpriteCollection", menuName = "EF/SpriteCollection", order = 50)]
    public class SpriteCollection : ScriptableObject
    {
        [SerializeField]
        [HideInInspector]
        string _atlasFolder;
        public string AtlasFolder => _atlasFolder;

        [SerializeField]
        [HideInInspector]
        List<Object> _objects = new List<Object>();
        public List<Object> TargetObjects => _objects;

        [SerializeField]
        [HideInInspector]
        List<SpriteAtlas> _atlas = new List<SpriteAtlas>();
        public List<SpriteAtlas> Atlas { get { return _atlas; } set { _atlas = value; } }

        public List<Dictionary<string, Sprite>> Sprites { get; set; } = new List<Dictionary<string, Sprite>>();

        [SerializeField]
        [HideInInspector]
        List<SpriteInfo> _spriteInfos = new List<SpriteInfo>();
        public List<SpriteInfo> SpriteInfos { get { return _spriteInfos; } set { _spriteInfos = value; } }

        [System.Serializable]
        public class SpriteInfo
        {
            [SerializeField]
            public string FolderName;

            [SerializeField]
            public List<string> PathList = new List<string>();

            [SerializeField]
            public List<Sprite> SpriteList = new List<Sprite>();
        }
    }
}

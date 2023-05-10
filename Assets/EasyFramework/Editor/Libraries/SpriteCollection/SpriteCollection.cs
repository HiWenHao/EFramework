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
        string m_AtlasFolder;
        public string AtlasFolder => m_AtlasFolder;

        [SerializeField]
        [HideInInspector]
        List<Object> m_Objects = new List<Object>();
        public List<Object> TargetObjects => m_Objects;

        [SerializeField]
        [HideInInspector]
        List<SpriteAtlas> m_Atlas = new List<SpriteAtlas>();
        public List<SpriteAtlas> Atlas { get { return m_Atlas; } set { m_Atlas = value; } }

        public List<Dictionary<string, Sprite>> Sprites { get; set; } = new List<Dictionary<string, Sprite>>();

        [SerializeField]
        [HideInInspector]
        List<SpriteInfo> m_SpriteInfos = new List<SpriteInfo>();
        public List<SpriteInfo> SpriteInfos { get { return m_SpriteInfos; } set { m_SpriteInfos = value; } }

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

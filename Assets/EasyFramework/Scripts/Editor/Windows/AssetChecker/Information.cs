/*
 * ================================================
 * Describe:      This script is used to show the resource detection overview. Let's thank LiangZG!!!!!
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-06-06 15:29:28
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-06-06 15:29:28
 * ScriptVersion: 0.1
 * ===============================================
*/

namespace EasyFramework.Windows.AssetChecker
{
    /// <summary>
    /// 纹理信息
    /// </summary>
    public class TextureInformation
    {
        public string Name;
        public string AssetDesc;
        public string FilePath;

        public int Width;
        public int Height;

        public bool MipMaps;
        public int MaxSize;
        public string Format;

        public float MemorySize;
        public string MemoryText;
    }

    /// <summary>
    /// 模型信息
    /// </summary>
    public class ModelInformation
    {
        public string Name;
        public string FilePath;
        public string AssetDesc;

        public int VertexCount;
        public int TriangleCount;
        public int BondCount;

        public string TextureName;
        public UnityEngine.Vector2 TextureSize;

        public float VetexScore;
        public float TriangleScore;
        public float BondScore;
        public float Score;
    }

    /// <summary>
    /// 特效信息
    /// </summary>
    public class EffectInformation
    {
        public string Name;
        public string FilePath;
        public string AssetDesc;

        public int DrawCallCount;
        public int TextureCount;
        public int ParticelCount;

        public float DrawCallScore;
        public float ParticeScore;
        public float Score;
    }
}

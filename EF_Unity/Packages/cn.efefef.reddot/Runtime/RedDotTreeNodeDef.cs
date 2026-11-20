/*
 * ================================================
 * Describe:      Data definition for declarative red dot tree registration.
 * Author:        Alvin5100, AI
 * CreationTime:  2026-11-20 14:15:00
 * ModifyAuthor:  Alvin5100, AI
 * ModifyTime:    2026-11-20
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework.Systems.RedDot
{
    /// <summary>
    /// 红点树节点定义（数据驱动注册用）
    /// <para>Red dot tree node definition (for data-driven registration).</para>
    /// </summary>
    public sealed class RedDotTreeNodeDef
    {
        /// <summary>节点唯一键 <para>Unique node key</para></summary>
        public readonly string Key;

        /// <summary>父节点键（可为空） <para>Parent node key (may be null)</para></summary>
        public readonly string ParentKey;

        /// <summary>显示类型 <para>Display type</para></summary>
        public readonly RedDotDisplayType DisplayType;

        /// <summary>图片路径（可选） <para>Image path (optional)</para></summary>
        public readonly string ImagePath;

        /// <summary>
        /// 构造节点定义
        /// <para>Construct a node definition.</para>
        /// </summary>
        /// <param name="key">节点键 <para>Node key</para></param>
        /// <param name="parentKey">父节点键（可为空） <para>Parent key (may be null)</para></param>
        /// <param name="displayType">显示类型 <para>Display type</para></param>
        /// <param name="imagePath">图片路径（可选） <para>Image path (optional)</para></param>
        public RedDotTreeNodeDef(string key, string parentKey = null,
            RedDotDisplayType displayType = RedDotDisplayType.Dot, string imagePath = null)
        {
            Key = key;
            ParentKey = parentKey;
            DisplayType = displayType;
            ImagePath = imagePath;
        }
    }
}

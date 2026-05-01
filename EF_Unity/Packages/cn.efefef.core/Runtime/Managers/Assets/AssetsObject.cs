/*
 * ================================================
 * Describe:      单个资产
 * Author:        Alvin8412
 * CreationTime:  2026-05-01 21:44:50
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-01 21:44:50
 * ScriptVersion: 0.1
 * ===============================================
 */

using UnityEngine;

namespace EasyFramework.Managers.Assets
{
    /// <summary>
    /// 单个资产
    /// </summary>
    public class AssetsObject : IAssetsObject
    {
        public int RefCount { get; set; }
        public bool IsReleased { get; set; }
        public Object Asset { get; set; }
    }
}
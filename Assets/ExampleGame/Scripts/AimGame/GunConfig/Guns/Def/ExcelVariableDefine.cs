/* 
 * ================================================
 * Describe:      This is the code for the excel variable define table. 
 * Author:        Xiaohei.Wang(Wenaho)
 * CreationTime:  2024-01-23 17:56:12
 * ModifyAuthor:  Xiaohei.Wang(Wenaho)
 * ModifyTime:    2024-01-23 17:56:12
 * Version:       0.50
 * ================================================
*/
#pragma warning disable
using EasyFramework.ExcelTool;

namespace AimGame{
    public static class EVD_GunInfos
    {
        /// <summary> [Int] ID </summary>
        public const int id = 0;
        /// <summary> [String] 枪械名称 </summary>
        public const int Name = 65540;
        /// <summary> [Int] 类型 </summary>
        public const int GunsType = 131080;
        /// <summary> [String] 介绍 </summary>
        public const int Description = 196620;
        /// <summary> [Int] 开火方式 </summary>
        public const int FireType = 262160;
        /// <summary> [Int] 射速 </summary>
        public const int FiringRate = 327700;
        /// <summary> [Int] 总弹药数量 </summary>
        public const int TotalAmmo = 393240;
        /// <summary> [Int] 弹夹数量 </summary>
        public const int Magazine = 458780;
        /// <summary> [Int] 头部伤害 </summary>
        public const int InjuryHead = 524320;
        /// <summary> [Int] 身体伤害 </summary>
        public const int InjuryBody = 589860;
        /// <summary> [Int] 四肢伤害 </summary>
        public const int InjuryLimbs = 655400;
    }
    public enum ExcelName
    {
        ///<summary>主列: id [Int]</summary>
        GunInfos = 0,
    }
}
#pragma warning disable

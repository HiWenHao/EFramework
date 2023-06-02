/* 
 * ================================================
 * Describe:      This is the code for the excel variable define table. 
 * Author:        Xiaohei.Wang(Wenaho)
 * CreationTime:  2023-05-30 14:21:12
 * ModifyAuthor:  Xiaohei.Wang(Wenaho)
 * ModifyTime:    2023-05-30 14:21:12
 * Version:       0.4
 * ================================================
*/
#pragma warning disable
using EasyFramework.ExcelTool;

namespace EasyFramework.Edit{
    public static class EVD_LC
    {
        /// <summary> [Int] ID </summary>
        public const int id = 0;
        /// <summary> [String] 英文注释 </summary>
        public const int Lc = 65540;
        /// <summary> [String] 测试数据 </summary>
        public const int Lc1 = 131080;
    }
    public enum ExcelName
    {
        ///<summary>主列: id [Int]</summary>
        LC = 0,
    }
}
#pragma warning disable

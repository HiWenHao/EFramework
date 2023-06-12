/* 
 * ================================================
 * Describe:      This is the code for the excel variable define table. 
 * Author:        Xiaohei.Wang(Wenaho)
 * CreationTime:  2023-06-12 17:29:21
 * ModifyAuthor:  Xiaohei.Wang(Wenaho)
 * ModifyTime:    2023-06-12 17:29:21
 * Version:       1.3000002
 * ================================================
*/
#pragma warning disable
using EasyFramework.ExcelTool;

namespace EasyFramework.Edit{
    public static class EVD_Config
    {
        /// <summary> [String] ID </summary>
        public const int ID = 0;
        /// <summary> [List&lt;String&gt;] 展示名字 </summary>
        public const int ShowName = 65540;
    }
    public enum ExcelName
    {
        ///<summary>主列: ID [String]</summary>
        Config = 0,
    }
}
#pragma warning disable

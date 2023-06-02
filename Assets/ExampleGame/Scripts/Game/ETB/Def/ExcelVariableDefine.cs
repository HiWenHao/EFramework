/* 
 * ================================================
 * Describe:      This is the code for the excel variable define table. 
 * Author:        Xiaohei.Wang(Wenaho)
 * CreationTime:  2023-05-30 11:29:41
 * ModifyAuthor:  Xiaohei.Wang(Wenaho)
 * ModifyTime:    2023-05-30 11:29:41
 * Version:       0.1
 * ================================================
*/
#pragma warning disable
using EasyFramework.ExcelTool;

namespace ETB{
    public static class EVD_Example
    {
        /// <summary> [Int] ID </summary>
        public const int id = 0;
        /// <summary> [String] 表格名字 </summary>
        public const int name = 65540;
        /// <summary> [List&lt;Int&gt;] int列表 </summary>
        public const int lsi = 131080;
        /// <summary> [List&lt;String&gt;] string列表 </summary>
        public const int lss = 196620;
        /// <summary> [Vector2] 二维向量 </summary>
        public const int v2 = 262160;
        /// <summary> [Vector3Int] 三维向量 </summary>
        public const int v3i = 327704;
        /// <summary> [Vector4] 四维向量 </summary>
        public const int v4 = 393252;
        /// <summary> [Dict&lt;Int ,Int&gt;] 字典双int </summary>
        public const int dic_Int = 458804;
        /// <summary> [Dict&lt;String ,String&gt;] 字典双string </summary>
        public const int ds = 524344;
        /// <summary> [Bool] 布尔值 </summary>
        public const int boolType = 589884;
        /// <summary> [Sbyte] 1 </summary>
        public const int sbyteType = 655421;
        /// <summary> [Byte] 2 </summary>
        public const int byteType = 720958;
        /// <summary> [UShort] 3 </summary>
        public const int ushortType = 786495;
        /// <summary> [Short] 4 </summary>
        public const int shortType = 852033;
        /// <summary> [UInt] 5 </summary>
        public const int uintType = 917571;
        /// <summary> [Int] 6 </summary>
        public const int intType = 983111;
        /// <summary> [ULong] 7 </summary>
        public const int ulongTYpe = 1048651;
        /// <summary> [Long] 8 </summary>
        public const int longTYpe = 1114195;
        /// <summary> [Float] 9 </summary>
        public const int floatType = 1179739;
        /// <summary> [String] 10 </summary>
        public const int StrType = 1245279;
        /// <summary> [Double] 11 </summary>
        public const int douType = 1310819;
    }
    public enum ExcelName
    {
        ///<summary>主列: id [Int]</summary>
        Example = 0,
    }
}
#pragma warning disable

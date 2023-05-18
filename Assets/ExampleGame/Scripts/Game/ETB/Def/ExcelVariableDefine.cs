/* 
 * ================================================
 * Describe:      This is the code for the excel variable define table. 
 * Author:        Xiaohei.Wang(Wenaho)
 * CreationTime:  2023-05-18 14:46:42
 * ModifyAuthor:  Xiaohei.Wang(Wenaho)
 * ModifyTime:    2023-05-18 14:46:42
 * Version:       0.3
 * ================================================
*/
#pragma warning disable
using EasyFramework.ExcelTool;

namespace ETB{
    public static class EVD_XH_Sheet1
    {
        /// <summary> [Int] ID </summary>
        public const int id = 0;
        /// <summary> [List&lt;Int&gt;] int列表 </summary>
        public const int lsi = 65540;
        /// <summary> [List&lt;String&gt;] string列表 </summary>
        public const int lss = 131080;
        /// <summary> [Vector2] 二维向量 </summary>
        public const int v2 = 196620;
        /// <summary> [Vector3Int] 三维向量 </summary>
        public const int v3i = 262164;
        /// <summary> [Vector4] 四维向量 </summary>
        public const int v4 = 327712;
        /// <summary> [Dict&lt;Int ,Int&gt;] 字典双int </summary>
        public const int dic_Int = 393264;
        /// <summary> [Dict&lt;String ,String&gt;] 字典双string </summary>
        public const int ds = 458804;
        /// <summary> [Bool] 布尔值 </summary>
        public const int boolType = 524344;
        /// <summary> [Sbyte] 1 </summary>
        public const int sbyteType = 589881;
        /// <summary> [Byte] 2 </summary>
        public const int byteType = 655418;
        /// <summary> [UShort] 3 </summary>
        public const int ushortType = 720955;
        /// <summary> [Short] 4 </summary>
        public const int shortType = 786493;
        /// <summary> [UInt] 5 </summary>
        public const int uintType = 852031;
        /// <summary> [Int] 6 </summary>
        public const int intType = 917571;
        /// <summary> [ULong] 7 </summary>
        public const int ulongTYpe = 983111;
        /// <summary> [Long] 8 </summary>
        public const int longTYpe = 1048655;
        /// <summary> [Float] 9 </summary>
        public const int floatType = 1114199;
        /// <summary> [String] 10 </summary>
        public const int StrType = 1179739;
        /// <summary> [Double] 11 </summary>
        public const int douType = 1245279;
    }
    public static class EVD_XH_Sheet2
    {
        /// <summary> [Int] 序列ID </summary>
        public const int id = 0;
        /// <summary> [String] 拼音 </summary>
        public const int Spelling = 65540;
        /// <summary> [List&lt;Float&gt;] Msg.消息 </summary>
        public const int OpenData = 131080;
        /// <summary> [List&lt;Float&gt;]  </summary>
        public const int ShutData = 196620;
    }
    public enum ExcelName
    {
        ///<summary>主列: id [Int]</summary>
        XH_Sheet1 = 0,
        ///<summary>主列: id [Int]</summary>
        XH_Sheet2 = 1,
    }
}
#pragma warning disable

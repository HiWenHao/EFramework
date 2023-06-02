/* 
 * ================================================
 * Describe:      This is the code for the excel struct define table. 
 * Author:        Xiaohei.Wang(Wenaho)
 * CreationTime:  2023-05-30 11:29:41
 * ModifyAuthor:  Xiaohei.Wang(Wenaho)
 * ModifyTime:    2023-05-30 11:29:41
 * Version:       0.1
 * ================================================
*/
using System.Collections.Generic;
using UnityEngine;
using EasyFramework.ExcelTool;

#pragma warning disable
namespace ETB
{
    public struct ESD_Example
    {
        int primaryColVal;
        readonly ByteFileInfo<int> byteFileInfo;
        public ESD_Example(int val)
        {
            this.primaryColVal = val;
            this.byteFileInfo = ExcelDataManager.GetByteFileInfo<int>((short)ExcelName.Example);
        }
        public void SetPrimary(int id) { this.primaryColVal = id; } 
        /// <summary> ID </summary>
        public int id => byteFileInfo.Get<int>(primaryColVal, 0);
        /// <summary> 表格名字 </summary>
        public string name => byteFileInfo.Get<string>(primaryColVal, 65540);
        /// <summary> int列表 </summary>
        public List<int> lsi => byteFileInfo.Get<List<int>>(primaryColVal, 131080);
        /// <summary> string列表 </summary>
        public List<string> lss => byteFileInfo.Get<List<string>>(primaryColVal, 196620);
        /// <summary> 二维向量 </summary>
        public Vector2 v2 => byteFileInfo.Get<Vector2>(primaryColVal, 262160);
        /// <summary> 三维向量 </summary>
        public Vector3Int v3i => byteFileInfo.Get<Vector3Int>(primaryColVal, 327704);
        /// <summary> 四维向量 </summary>
        public Vector4 v4 => byteFileInfo.Get<Vector4>(primaryColVal, 393252);
        /// <summary> 字典双int </summary>
        public Dictionary<int, int> dic_Int => byteFileInfo.GetDict<int, int>(primaryColVal, 458804);
        /// <summary> 字典双string </summary>
        public Dictionary<string, string> ds => byteFileInfo.GetDict<string, string>(primaryColVal, 524344);
        /// <summary> 布尔值 </summary>
        public bool boolType => byteFileInfo.Get<bool>(primaryColVal, 589884);
        /// <summary> 1 </summary>
        public sbyte sbyteType => byteFileInfo.Get<sbyte>(primaryColVal, 655421);
        /// <summary> 2 </summary>
        public byte byteType => byteFileInfo.Get<byte>(primaryColVal, 720958);
        /// <summary> 3 </summary>
        public ushort ushortType => byteFileInfo.Get<ushort>(primaryColVal, 786495);
        /// <summary> 4 </summary>
        public short shortType => byteFileInfo.Get<short>(primaryColVal, 852033);
        /// <summary> 5 </summary>
        public uint uintType => byteFileInfo.Get<uint>(primaryColVal, 917571);
        /// <summary> 6 </summary>
        public int intType => byteFileInfo.Get<int>(primaryColVal, 983111);
        /// <summary> 7 </summary>
        public ulong ulongTYpe => byteFileInfo.Get<ulong>(primaryColVal, 1048651);
        /// <summary> 8 </summary>
        public long longTYpe => byteFileInfo.Get<long>(primaryColVal, 1114195);
        /// <summary> 9 </summary>
        public float floatType => byteFileInfo.Get<float>(primaryColVal, 1179739);
        /// <summary> 10 </summary>
        public string StrType => byteFileInfo.Get<string>(primaryColVal, 1245279);
        /// <summary> 11 </summary>
        public double douType => byteFileInfo.Get<double>(primaryColVal, 1310819);
    }

}
#pragma warning disable

/* 
 * ================================================
 * Describe:      This is the code for the excel struct define table. 
 * Author:        Xiaohei.Wang(Wenaho)
 * CreationTime:  2023-05-14 18:21:21
 * ModifyAuthor:  Xiaohei.Wang(Wenaho)
 * ModifyTime:    2023-05-14 18:21:21
 * Version:       0.2
 * ================================================
*/
using System.Collections.Generic;
using UnityEngine;
using EasyFramework.ExcelTool;

#pragma warning disable
namespace ETB
{
    public struct ESD_XH_Sheet1
    {
        int primaryColVal;
        readonly ByteFileInfo<int> byteFileInfo;
        public ESD_XH_Sheet1(int val)
        {
            this.primaryColVal = val;
            this.byteFileInfo = ExcelDataManager.GetByteFileInfo<int>((short)ExcelName.XH_Sheet1);
        }
        public void SetPrimary(int id) { this.primaryColVal = id; } 
        /// <summary> ID </summary>
        public int id => byteFileInfo.Get<int>(primaryColVal, 0);
        /// <summary> int列表 </summary>
        public List<int> lsi => byteFileInfo.Get<List<int>>(primaryColVal, 65540);
        /// <summary> string列表 </summary>
        public List<string> lss => byteFileInfo.Get<List<string>>(primaryColVal, 131080);
        /// <summary> 二维向量 </summary>
        public Vector2 v2 => byteFileInfo.Get<Vector2>(primaryColVal, 196620);
        /// <summary> 三维向量 </summary>
        public Vector3Int v3i => byteFileInfo.Get<Vector3Int>(primaryColVal, 262164);
        /// <summary> 四维向量 </summary>
        public Vector4 v4 => byteFileInfo.Get<Vector4>(primaryColVal, 327712);
        /// <summary> 字典双int </summary>
        public Dictionary<int, int> di => byteFileInfo.GetDict<int, int>(primaryColVal, 393264);
        /// <summary> 字典双string </summary>
        public Dictionary<string, string> ds => byteFileInfo.GetDict<string, string>(primaryColVal, 458804);
        /// <summary> 布尔值 </summary>
        public bool boolType => byteFileInfo.Get<bool>(primaryColVal, 524344);
        /// <summary> 1 </summary>
        public sbyte sbyteType => byteFileInfo.Get<sbyte>(primaryColVal, 589881);
        /// <summary> 2 </summary>
        public byte byteType => byteFileInfo.Get<byte>(primaryColVal, 655418);
        /// <summary> 3 </summary>
        public ushort ushortType => byteFileInfo.Get<ushort>(primaryColVal, 720955);
        /// <summary> 4 </summary>
        public short shortType => byteFileInfo.Get<short>(primaryColVal, 786493);
        /// <summary> 5 </summary>
        public uint uintType => byteFileInfo.Get<uint>(primaryColVal, 852031);
        /// <summary> 6 </summary>
        public int intType => byteFileInfo.Get<int>(primaryColVal, 917571);
        /// <summary> 7 </summary>
        public ulong ulongTYpe => byteFileInfo.Get<ulong>(primaryColVal, 983111);
        /// <summary> 8 </summary>
        public long longTYpe => byteFileInfo.Get<long>(primaryColVal, 1048655);
        /// <summary> 9 </summary>
        public float floatType => byteFileInfo.Get<float>(primaryColVal, 1114199);
        /// <summary> 10 </summary>
        public string StrType => byteFileInfo.Get<string>(primaryColVal, 1179739);
        /// <summary> 11 </summary>
        public double douType => byteFileInfo.Get<double>(primaryColVal, 1245279);
    }
    public struct ESD_XH_Sheet2
    {
        int primaryColVal;
        readonly ByteFileInfo<int> byteFileInfo;
        public ESD_XH_Sheet2(int val)
        {
            this.primaryColVal = val;
            this.byteFileInfo = ExcelDataManager.GetByteFileInfo<int>((short)ExcelName.XH_Sheet2);
        }
        public void SetPrimary(int id) { this.primaryColVal = id; } 
        /// <summary> 序列ID </summary>
        public int id => byteFileInfo.Get<int>(primaryColVal, 0);
        /// <summary> 拼音 </summary>
        public string Spelling => byteFileInfo.Get<string>(primaryColVal, 65540);
        /// <summary> Msg.消息 </summary>
        public List<float> OpenData => byteFileInfo.Get<List<float>>(primaryColVal, 131080);
        /// <summary>  </summary>
        public List<float> ShutData => byteFileInfo.Get<List<float>>(primaryColVal, 196620);
    }

}
#pragma warning disable

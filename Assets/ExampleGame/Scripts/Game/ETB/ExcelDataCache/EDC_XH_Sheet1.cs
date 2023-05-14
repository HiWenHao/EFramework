/* 
 * ================================================
 * Describe:      This is the code for the XH_Sheet1 table. 
 * Author:        Xiaohei.Wang(Wenaho)
 * CreationTime:  2023-05-14 14:44:45
 * ModifyAuthor:  Xiaohei.Wang(Wenaho)
 * ModifyTime:    2023-05-14 14:44:45
 * Version:       0.1
 * ===============================================
*/
using System.Collections.Generic;
using UnityEngine;
using XHTools;
using XHTools.ExcelTool;

#pragma warning disable
namespace ETB
{
    public class EDC_XH_Sheet1
    {
        public static int[] Ids => byteFileInfo.Ids;
        static bool cached = false;
        static ByteFileInfo<int> byteFileInfo;
        static Dictionary<int, EDC_XH_Sheet1> cacheDict = new Dictionary<int, EDC_XH_Sheet1>();

        /// <summary> ID </summary>
        public int id { get; }
        /// <summary> int列表 </summary>
        public List<int> lsi { get; }
        /// <summary> string列表 </summary>
        public List<string> lss { get; }
        /// <summary> 二维向量 </summary>
        public Vector2 v2 { get; }
        /// <summary> 三维向量 </summary>
        public Vector3Int v3i { get; }
        /// <summary> 四维向量 </summary>
        public Vector4 v4 { get; }
        /// <summary> 字典双int </summary>
        public Dictionary<int, int> di { get; }
        /// <summary> 字典双string </summary>
        public Dictionary<string, string> ds { get; }
        /// <summary> 布尔值 </summary>
        public bool boolType { get; }
        /// <summary> 1 </summary>
        public sbyte sbyteType { get; }
        /// <summary> 2 </summary>
        public byte byteType { get; }
        /// <summary> 3 </summary>
        public ushort ushortType { get; }
        /// <summary> 4 </summary>
        public short shortType { get; }
        /// <summary> 5 </summary>
        public uint uintType { get; }
        /// <summary> 6 </summary>
        public int intType { get; }
        /// <summary> 7 </summary>
        public ulong ulongTYpe { get; }
        /// <summary> 8 </summary>
        public long longTYpe { get; }
        /// <summary> 9 </summary>
        public float floatType { get; }
        /// <summary> 10 </summary>
        public string StrType { get; }
        /// <summary> 11 </summary>
        public double douType { get; }

        public EDC_XH_Sheet1(int id)
        {
            this.id = id;
            ByteFileReader.SkipOne();
            this.lsi = ByteFileReader.Get<List<int>>();
            this.lss = ByteFileReader.Get<List<string>>();
            this.v2 = ByteFileReader.Get<Vector2>();
            this.v3i = ByteFileReader.Get<Vector3Int>();
            this.v4 = ByteFileReader.Get<Vector4>();
            this.di = ByteFileReader.GetDict<int, int>();
            this.ds = ByteFileReader.GetDict<string, string>();
            this.boolType = ByteFileReader.Get<bool>();
            this.sbyteType = ByteFileReader.Get<sbyte>();
            this.byteType = ByteFileReader.Get<byte>();
            this.ushortType = ByteFileReader.Get<ushort>();
            this.shortType = ByteFileReader.Get<short>();
            this.uintType = ByteFileReader.Get<uint>();
            this.intType = ByteFileReader.Get<int>();
            this.ulongTYpe = ByteFileReader.Get<ulong>();
            this.longTYpe = ByteFileReader.Get<long>();
            this.floatType = ByteFileReader.Get<float>();
            this.StrType = ByteFileReader.Get<string>();
            this.douType = ByteFileReader.Get<double>();

        }

        public static void CacheData()
        {
            if (cached) return;
            if (byteFileInfo == null)
            {
                byteFileInfo = ExcelDataManager.GetByteFileInfo<int>((short)ExcelName.XH_Sheet1);
            }
            if (!byteFileInfo.ByteDataLoaded) byteFileInfo.LoadByteData();
            byteFileInfo.ResetByteFileReader();
            for (int i = 0; i < byteFileInfo.RowCount; i++)
            {
                int id = byteFileInfo.GetKey(i);
                EDC_XH_Sheet1 cache = new EDC_XH_Sheet1(id);
                cacheDict.Add(id, cache);
            }
        }

        public static EDC_XH_Sheet1 Get(int id)
        {
            if (cacheDict.TryGetValue(id, out var cache)) return cache;
            D.Error($"{typeof(EDC_XH_Sheet1).Name}不存在主列值{id.ToString()}");
            return null;
        }
    }
}
#pragma warning disable

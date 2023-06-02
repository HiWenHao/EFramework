/* 
 * ================================================
 * Describe:      This is the code for the LC table. 
 * Author:        Xiaohei.Wang(Wenaho)
 * CreationTime:  2023-05-30 14:21:12
 * ModifyAuthor:  Xiaohei.Wang(Wenaho)
 * ModifyTime:    2023-05-30 14:21:12
 * Version:       0.4
 * ===============================================
*/
using System.Collections.Generic;
using UnityEngine;
using EasyFramework;
using EasyFramework.ExcelTool;

#pragma warning disable
namespace EasyFramework.Edit
{
    public class EDC_LC
    {
        public static int[] Ids => byteFileInfo.Ids;
        static bool cached = false;
        static ByteFileInfo<int> byteFileInfo;
        static Dictionary<int, EDC_LC> cacheDict = new Dictionary<int, EDC_LC>();

        /// <summary> ID </summary>
        public int id { get; }
        /// <summary> 英文注释 </summary>
        public string Lc { get; }
        /// <summary> 测试数据 </summary>
        public string Lc1 { get; }

        public EDC_LC(int id)
        {
            this.id = id;
            ByteFileReader.SkipOne();
            this.Lc = ByteFileReader.Get<string>();
            this.Lc1 = ByteFileReader.Get<string>();

        }

        public static void CacheData()
        {
            if (cached) return;
            if (byteFileInfo == null)
            {
                byteFileInfo = ExcelDataManager.GetByteFileInfo<int>((short)ExcelName.LC);
            }
            if (!byteFileInfo.ByteDataLoaded) byteFileInfo.LoadByteData();
            byteFileInfo.ResetByteFileReader();
            for (int i = 0; i < byteFileInfo.RowCount; i++)
            {
                int id = byteFileInfo.GetKey(i);
                EDC_LC cache = new EDC_LC(id);
                cacheDict.Add(id, cache);
            }
        }

        public static EDC_LC Get(int id)
        {
            if (cacheDict.TryGetValue(id, out var cache)) return cache;
            D.Error($"{typeof(EDC_LC).Name}不存在主列值{id.ToString()}");
            return null;
        }
    }
}
#pragma warning disable

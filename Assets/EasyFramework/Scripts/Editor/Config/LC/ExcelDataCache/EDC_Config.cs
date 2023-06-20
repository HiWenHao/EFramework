/* 
 * ================================================
 * Describe:      This is the code for the Config table. 
 * Author:        Xiaohei.Wang(Wenaho)
 * CreationTime:  2023-06-20 17:54:35
 * ModifyAuthor:  Xiaohei.Wang(Wenaho)
 * ModifyTime:    2023-06-20 17:54:35
 * Version:       1.80
 * ===============================================
*/
using System.Collections.Generic;
using UnityEngine;
using EasyFramework;
using EasyFramework.ExcelTool;

#pragma warning disable
namespace EasyFramework.Edit
{
    public class EDC_Config
    {
        public static string[] Ids => byteFileInfo.Ids;
        static bool cached = false;
        static ByteFileInfo<string> byteFileInfo;
        static Dictionary<string, EDC_Config> cacheDict = new Dictionary<string, EDC_Config>();

        /// <summary> ID </summary>
        public string ID { get; }
        /// <summary> 展示名字 </summary>
        public List<string> ShowName { get; }

        public EDC_Config(string id)
        {
            this.ID = id;
            ByteFileReader.SkipOne();
            this.ShowName = ByteFileReader.Get<List<string>>();

        }

        public static void CacheData()
        {
            if (cached) return;
            if (byteFileInfo == null)
            {
                byteFileInfo = ExcelDataManager.GetByteFileInfo<string>((short)ExcelName.Config);
            }
            if (!byteFileInfo.ByteDataLoaded) byteFileInfo.LoadByteData();
            byteFileInfo.ResetByteFileReader();
            for (int i = 0; i < byteFileInfo.RowCount; i++)
            {
                string id = byteFileInfo.GetKey(i);
                EDC_Config cache = new EDC_Config(id);
                cacheDict.Add(id, cache);
            }
        }

        public static EDC_Config Get(string id)
        {
            if (cacheDict.TryGetValue(id, out var cache)) return cache;
            D.Error($"{typeof(EDC_Config).Name}不存在主列值{id.ToString()}");
            return null;
        }
    }
}
#pragma warning disable

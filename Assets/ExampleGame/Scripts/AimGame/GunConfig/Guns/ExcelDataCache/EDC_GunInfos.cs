/* 
 * ================================================
 * Describe:      This is the code for the GunInfos table. 
 * Author:        Xiaohei.Wang(Wenaho)
 * CreationTime:  2024-01-23 17:56:12
 * ModifyAuthor:  Xiaohei.Wang(Wenaho)
 * ModifyTime:    2024-01-23 17:56:12
 * Version:       0.50
 * ===============================================
*/
using System.Collections.Generic;
using UnityEngine;
using EasyFramework;
using EasyFramework.ExcelTool;

#pragma warning disable
namespace AimGame
{
    public class EDC_GunInfos
    {
        public static int[] Ids => byteFileInfo.Ids;
        static bool cached = false;
        static ByteFileInfo<int> byteFileInfo;
        static Dictionary<int, EDC_GunInfos> cacheDict = new Dictionary<int, EDC_GunInfos>();

        /// <summary> ID </summary>
        public int id { get; }
        /// <summary> 枪械名称 </summary>
        public string Name { get; }
        /// <summary> 类型 </summary>
        public int GunsType { get; }
        /// <summary> 介绍 </summary>
        public string Description { get; }
        /// <summary> 开火方式 </summary>
        public int FireType { get; }
        /// <summary> 射速 </summary>
        public int FiringRate { get; }
        /// <summary> 总弹药数量 </summary>
        public int TotalAmmo { get; }
        /// <summary> 弹夹数量 </summary>
        public int Magazine { get; }
        /// <summary> 头部伤害 </summary>
        public int InjuryHead { get; }
        /// <summary> 身体伤害 </summary>
        public int InjuryBody { get; }
        /// <summary> 四肢伤害 </summary>
        public int InjuryLimbs { get; }

        public EDC_GunInfos(int id)
        {
            this.id = id;
            ByteFileReader.SkipOne();
            this.Name = ByteFileReader.Get<string>();
            this.GunsType = ByteFileReader.Get<int>();
            this.Description = ByteFileReader.Get<string>();
            this.FireType = ByteFileReader.Get<int>();
            this.FiringRate = ByteFileReader.Get<int>();
            this.TotalAmmo = ByteFileReader.Get<int>();
            this.Magazine = ByteFileReader.Get<int>();
            this.InjuryHead = ByteFileReader.Get<int>();
            this.InjuryBody = ByteFileReader.Get<int>();
            this.InjuryLimbs = ByteFileReader.Get<int>();

        }

        public static void CacheData()
        {
            if (cached) return;
            if (byteFileInfo == null)
            {
                byteFileInfo = ExcelDataManager.GetByteFileInfo<int>((short)ExcelName.GunInfos);
            }
            if (!byteFileInfo.ByteDataLoaded) byteFileInfo.LoadByteData();
            byteFileInfo.ResetByteFileReader();
            for (int i = 0; i < byteFileInfo.RowCount; i++)
            {
                int id = byteFileInfo.GetKey(i);
                EDC_GunInfos cache = new EDC_GunInfos(id);
                cacheDict.Add(id, cache);
            }
        }

        public static EDC_GunInfos Get(int id)
        {
            if (cacheDict.TryGetValue(id, out var cache)) return cache;
            D.Error($"{typeof(EDC_GunInfos).Name}不存在主列值{id.ToString()}");
            return null;
        }
    }
}
#pragma warning disable

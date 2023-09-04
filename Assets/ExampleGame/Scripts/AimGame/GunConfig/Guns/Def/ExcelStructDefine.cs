/* 
 * ================================================
 * Describe:      This is the code for the excel struct define table. 
 * Author:        Xiaohei.Wang(Wenaho)
 * CreationTime:  2023-08-28 15:10:38
 * ModifyAuthor:  Xiaohei.Wang(Wenaho)
 * ModifyTime:    2023-08-28 15:10:38
 * Version:       0.40
 * ================================================
*/
using System.Collections.Generic;
using UnityEngine;
using EasyFramework.ExcelTool;

#pragma warning disable
namespace AimGame
{
    public struct ESD_GunInfos
    {
        int primaryColVal;
        readonly ByteFileInfo<int> byteFileInfo;
        public ESD_GunInfos(int val)
        {
            this.primaryColVal = val;
            this.byteFileInfo = ExcelDataManager.GetByteFileInfo<int>((short)ExcelName.GunInfos);
        }
        public void SetPrimary(int id) { this.primaryColVal = id; } 
        /// <summary> ID </summary>
        public int id => byteFileInfo.Get<int>(primaryColVal, 0);
        /// <summary> 枪械名称 </summary>
        public string Name => byteFileInfo.Get<string>(primaryColVal, 65540);
        /// <summary> 类型 </summary>
        public int GunsType => byteFileInfo.Get<int>(primaryColVal, 131080);
        /// <summary> 介绍 </summary>
        public string Description => byteFileInfo.Get<string>(primaryColVal, 196620);
        /// <summary> 开火方式 </summary>
        public int FireType => byteFileInfo.Get<int>(primaryColVal, 262160);
        /// <summary> 射速 </summary>
        public int FiringRate => byteFileInfo.Get<int>(primaryColVal, 327700);
        /// <summary> 总弹药数量 </summary>
        public int TotalAmmo => byteFileInfo.Get<int>(primaryColVal, 393240);
        /// <summary> 弹夹数量 </summary>
        public int Magazine => byteFileInfo.Get<int>(primaryColVal, 458780);
        /// <summary> 头部伤害 </summary>
        public int InjuryHead => byteFileInfo.Get<int>(primaryColVal, 524320);
        /// <summary> 身体伤害 </summary>
        public int InjuryBody => byteFileInfo.Get<int>(primaryColVal, 589860);
        /// <summary> 四肢伤害 </summary>
        public int InjuryLimbs => byteFileInfo.Get<int>(primaryColVal, 655400);
    }

}
#pragma warning disable

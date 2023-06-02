/* 
 * ================================================
 * Describe:      This is the code for the excel struct define table. 
 * Author:        Xiaohei.Wang(Wenaho)
 * CreationTime:  2023-05-30 14:21:12
 * ModifyAuthor:  Xiaohei.Wang(Wenaho)
 * ModifyTime:    2023-05-30 14:21:12
 * Version:       0.4
 * ================================================
*/
using System.Collections.Generic;
using UnityEngine;
using EasyFramework.ExcelTool;

#pragma warning disable
namespace EasyFramework.Edit
{
    public struct ESD_LC
    {
        int primaryColVal;
        readonly ByteFileInfo<int> byteFileInfo;
        public ESD_LC(int val)
        {
            this.primaryColVal = val;
            this.byteFileInfo = ExcelDataManager.GetByteFileInfo<int>((short)ExcelName.LC);
        }
        public void SetPrimary(int id) { this.primaryColVal = id; } 
        /// <summary> ID </summary>
        public int id => byteFileInfo.Get<int>(primaryColVal, 0);
        /// <summary> 英文注释 </summary>
        public string Lc => byteFileInfo.Get<string>(primaryColVal, 65540);
        /// <summary> 测试数据 </summary>
        public string Lc1 => byteFileInfo.Get<string>(primaryColVal, 131080);
    }

}
#pragma warning disable

/* 
 * ================================================
 * Describe:      This is the code for the excel struct define table. 
 * Author:        Xiaohei.Wang(Wenaho)
 * CreationTime:  2023-06-12 17:29:21
 * ModifyAuthor:  Xiaohei.Wang(Wenaho)
 * ModifyTime:    2023-06-12 17:29:21
 * Version:       1.3000002
 * ================================================
*/
using System.Collections.Generic;
using UnityEngine;
using EasyFramework.ExcelTool;

#pragma warning disable
namespace EasyFramework.Edit
{
    public struct ESD_Config
    {
        string primaryColVal;
        readonly ByteFileInfo<string> byteFileInfo;
        public ESD_Config(string val)
        {
            this.primaryColVal = val;
            this.byteFileInfo = ExcelDataManager.GetByteFileInfo<string>((short)ExcelName.Config);
        }
        public void SetPrimary(string id) { this.primaryColVal = id; } 
        /// <summary> ID </summary>
        public string ID => byteFileInfo.Get<string>(primaryColVal, 0);
        /// <summary> 展示名字 </summary>
        public List<string> ShowName => byteFileInfo.Get<List<string>>(primaryColVal, 65540);
    }

}
#pragma warning disable

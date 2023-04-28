/* 
 * ================================================
 * Describe:      This is the code for the XH_Sheet2 table. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-28 18:56:05
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-04-28 18:56:05
 * Version:       1.0
 * ===============================================
*/
using System.Collections.Generic;
using UnityEngine;
using XHTools;
using XHTools.ExcelTool;

#pragma warning disable

namespace ETB
{
	public class EDC_XH_Sheet2
	{
		public static int[] Ids => byteFileInfo.Ids;
		static bool cached = false;
		static ByteFileInfo<int> byteFileInfo;
		static Dictionary<int, EDC_XH_Sheet2> cacheDict = new Dictionary<int, EDC_XH_Sheet2>();

		/// <summary> 序列ID </summary>
		public int id { get; }
		/// <summary> 拼音 </summary>
		public string Spelling { get; }
		/// <summary> Msg.消息 </summary>
		public List<float> OpenData { get; }
		/// <summary>  </summary>
		public List<float> ShutData { get; }

		public EDC_XH_Sheet2(int id)
		{
			this.id = id;
			ByteFileReader.SkipOne();
			this.Spelling = ByteFileReader.Get<string>();
			this.OpenData = ByteFileReader.Get<List<float>>();
			this.ShutData = ByteFileReader.Get<List<float>>();

		}

		public static void CacheData()
		{
			if (cached) return;
			if (byteFileInfo == null)
			{
				byteFileInfo = ExcelDataManager.GetByteFileInfo<int>((short)ExcelName.XH_Sheet2);
			}
			if (!byteFileInfo.ByteDataLoaded) byteFileInfo.LoadByteData();
			byteFileInfo.ResetByteFileReader();
			for (int i = 0; i < byteFileInfo.RowCount; i++)
			{
				int id = byteFileInfo.GetKey(i);
				EDC_XH_Sheet2 cache = new EDC_XH_Sheet2(id);
				cacheDict.Add(id, cache);
			}
		}

		public static EDC_XH_Sheet2 Get(int id)
		{
			if (cacheDict.TryGetValue(id, out var cache)) return cache;
			D.Error($"{typeof(EDC_XH_Sheet2).Name}不存在主列值{id.ToString()}");
			return null;
		}
	}
}

/* 
 * ================================================
 * Describe:      This is the code for the cache the excel data manager. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-28 18:56:05
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-04-28 18:56:05
 * Version:       1.0
 * ===============================================
*/
#pragma warning disable

using EasyFramework;

namespace ETB
{
	public class ExcelDataCacheManager
	{
		public static void CacheAllData()
		{
			EDC_XH_Sheet1.CacheData();
			EDC_XH_Sheet2.CacheData();
		}
	}
}

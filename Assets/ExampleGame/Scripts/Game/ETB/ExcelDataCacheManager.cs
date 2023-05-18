/* 
 * ================================================
 * Describe:      This is the code for the cache the excel data manager. 
 * Author:        Xiaohei.Wang(Wenaho)
 * CreationTime:  2023-05-18 14:46:42
 * ModifyAuthor:  Xiaohei.Wang(Wenaho)
 * ModifyTime:    2023-05-18 14:46:42
 * Version:       0.3
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
#pragma warning disable

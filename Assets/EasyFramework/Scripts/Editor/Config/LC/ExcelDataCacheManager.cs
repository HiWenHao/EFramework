/* 
 * ================================================
 * Describe:      This is the code for the cache the excel data manager. 
 * Author:        Xiaohei.Wang(Wenaho)
 * CreationTime:  2023-05-30 14:21:12
 * ModifyAuthor:  Xiaohei.Wang(Wenaho)
 * ModifyTime:    2023-05-30 14:21:12
 * Version:       0.4
 * ===============================================
*/
#pragma warning disable

using EasyFramework;

namespace EasyFramework.Edit
{
    public class ExcelDataCacheManager
    {
        public static void CacheAllData()
        {
            EDC_LC.CacheData();
        }
    }
}
#pragma warning disable

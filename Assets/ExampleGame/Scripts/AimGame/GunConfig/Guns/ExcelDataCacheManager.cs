/* 
 * ================================================
 * Describe:      This is the code for the cache the excel data manager. 
 * Author:        Xiaohei.Wang(Wenaho)
 * CreationTime:  2024-01-23 17:56:12
 * ModifyAuthor:  Xiaohei.Wang(Wenaho)
 * ModifyTime:    2024-01-23 17:56:12
 * Version:       0.50
 * ===============================================
*/
#pragma warning disable

using EasyFramework;

namespace AimGame
{
    public class ExcelDataCacheManager
    {
        public static void CacheAllData()
        {
            EDC_GunInfos.CacheData();
        }
    }
}
#pragma warning disable

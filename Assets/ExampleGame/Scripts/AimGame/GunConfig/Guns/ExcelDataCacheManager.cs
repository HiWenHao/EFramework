/* 
 * ================================================
 * Describe:      This is the code for the cache the excel data manager. 
 * Author:        Xiaohei.Wang(Wenaho)
 * CreationTime:  2023-08-28 15:10:38
 * ModifyAuthor:  Xiaohei.Wang(Wenaho)
 * ModifyTime:    2023-08-28 15:10:38
 * Version:       0.40
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

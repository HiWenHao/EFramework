/* 
 * ================================================
 * Describe:      This script is used to setting project contents. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-15 15:11:29
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-01 14:59:28
 * ScriptVersion: 0.1
 * ===============================================
*/

using System;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 规则前缀
    /// </summary>
    [Serializable]
    public struct RulePrefixes
    {
        /// <summary>
        /// 前缀
        /// </summary>
        public string Prefixe;

        /// <summary>
        /// 全名称
        /// </summary>
        public string FullName;

        /// <summary>
        /// 规则前缀
        /// </summary>
        /// <param name="prefixes">前缀</param>
        /// <param name="fullName">全内容</param>
        public RulePrefixes(string prefixes, string fullName)
        {
            Prefixe = prefixes;
            FullName = fullName;
        }
    }
}

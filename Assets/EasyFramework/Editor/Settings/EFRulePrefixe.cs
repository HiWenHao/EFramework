/* 
 * ================================================
 * Describe:      This script is used to setting project contents. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-15 15:11:29
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-15 15:11:29
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
    public class EFRulePrefixe
    {
        /// <summary>
        /// 前缀
        /// </summary>
        public string Prefixe;

        /// <summary>
        /// 全名称
        /// </summary>
        public string FullContent;

        /// <summary>
        /// 规则前缀
        /// </summary>
        /// <param name="prefixe">前缀</param>
        /// <param name="fullContent">全内容</param>
        public EFRulePrefixe(string prefixe, string fullContent)
        {
            Prefixe = prefixe;
            FullContent = fullContent;
        }
    }
}

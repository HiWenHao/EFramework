/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-04-13 15:10:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-13 15:10:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Edit
{
    /// <summary>
    /// EF配置面板特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EFConfigPanelAttribute : Attribute
    {
        /// <summary>
        /// 面板排序优先级， 数值越小排名越靠前, 默认为9999
        /// <para>Panel sorting priority: The smaller the value, the top of the ranking. The default value is 9999.</para>
        /// </summary>
        public int Priority { get; set; } = 9999;
    }
}
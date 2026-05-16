/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-05-16 00:52:53
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-16 00:52:53
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Managers
{
    /// <summary>
    /// 标记一个类为管理器（可排序）
    /// <para>Mark a class as a manager (sortable)</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ManagerAttribute : BaseAttribute
    {
        /// <summary>
        /// 更新顺序，值越小越先执行; 默认为 -1
        /// <para>Update order: The smaller the value, the earlier it is executed. Default is -1</para>
        /// </summary>
        public int Order { get; set; } = -1;
    }
}

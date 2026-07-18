/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-05-16 19:44:49
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-16 19:44:49
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework
{
    /// <summary>
    /// 标记一个单例类的的优先级 - 数值越大更新越靠前，退出越靠后， 默认为 0
    /// <para><c> 10000 以上是EF框架中带有的单例管理器，其余内容最大不应高于 10000 </c></para>
    /// Marks the priority of a singleton class - the higher the number, the earlier it is updated and the later it is exited. The default value is 0.
    /// <para><c>Values above 10000 are the singleton managers included in the EF framework. The rest should not exceed 10000.</c></para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public abstract class SingletonPriorityAttribute : BaseAttribute
    {
        /// <summary>
        /// 数值越大更新越靠前，退出越靠后， 默认为 0
        /// <para><c> 10000 以上是EF框架中带有的单例管理器，其余内容最大不应高于 10000 </c></para>
        /// The higher the number, the earlier it is updated and the later it is exited. The default value is 0.
        /// <para><c>Values above 10000 are the singleton managers included in the EF framework. The rest should not exceed 10000.</c></para>
        /// </summary>
        public int Order { get; set; }
    }
}

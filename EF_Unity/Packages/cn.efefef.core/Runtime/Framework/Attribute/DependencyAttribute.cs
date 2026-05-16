/*
 * ================================================
 * Describe:      声明当前单例依赖其他单例
 * Author:        Alvin8412
 * CreationTime:  2026-05-16 16:40:39
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-16 16:40:39
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework
{
    /// <summary>
    /// 声明当前单例依赖其他单例
    /// <para>Declare that the current singleton depends on another singleton.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DependencyAttribute : BaseAttribute
    {
        /// <summary>
        /// 依赖类型
        /// </summary>
        public Type DependencyType { get; }
        
        /// <summary>
        /// 声明当前单例依赖其他单例
        /// <para>Declare that the current singleton depends on another singleton.</para>
        /// </summary>
        /// <param name="dependencyType">依赖类型</param>
        public DependencyAttribute(Type dependencyType) => DependencyType = dependencyType;
    }
}

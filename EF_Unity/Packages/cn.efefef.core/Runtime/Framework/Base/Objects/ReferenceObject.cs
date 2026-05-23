/*
 * ================================================
 * Describe:      引用对象基类
 * Author:        Alvin8412
 * CreationTime:  2026-05-23 16:47:16
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-23 16:47:16
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework
{
    /// <summary>
    /// 引用对象
    /// </summary>
    public class ReferenceObject : EFObject
    {
        /// <summary>
        /// 对象目前还存在引用
        /// <para>The object still has a reference at present.</para>
        /// </summary>
        public bool HasReference => ReferenceCount <= 0;

        /// <summary>
        /// 当前被引用数量
        /// <para>Current citation count</para>
        /// </summary>
        public int ReferenceCount { get; private set; }

        /// <summary>
        /// 增加引用次数
        /// <para>Increase the number of citations</para>
        /// </summary>
        /// <param name="count">引用数量</param>
        public virtual void AddCitation(int count = 1)
        {
            ReferenceCount += count;
        }

        /// <summary>
        /// 减去引用次数
        /// <para>Subtract the number of citations</para>
        /// </summary>
        /// <param name="count">引用数量</param>
        public virtual void SubtractCitation(int count = 1)
        {
            ReferenceCount -= count;
            if (ReferenceCount < 0)
                ReferenceCount = 0;
        }
    }

    /// <summary>
    /// 引用对象
    /// </summary>
    /// <typeparam name="T">引用目标类型</typeparam>
    public class ReferenceObject<T> : ReferenceObject where T : class
    {
        /// <summary>
        /// 引用目标
        /// </summary>
        public T ReferenceTarget { get; set; }
    }
}
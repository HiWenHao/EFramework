/*
 * ================================================
 * Describe:        This is singleton interface.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-14:42:36
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2022-05-14:42:36
 * Version:         1.0
 * ===============================================
 */
namespace EasyFramework
{
    /// <summary>
    /// A singleton interface in project
    /// <para>项目中的单例接口</para>
    /// </summary>
    public interface ISingleton
    {
        /// <summary>
        /// Initialize
        /// <para>初始化</para>
        /// </summary>
        public void Init();
        /// <summary>
        /// Quit
        /// <para>退出</para>
        /// </summary>
        public void Quit();
    }
}

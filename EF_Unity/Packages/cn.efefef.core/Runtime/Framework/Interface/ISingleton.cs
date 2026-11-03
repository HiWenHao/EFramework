/*
 * ================================================
 * Describe:        框架单例接口
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-14 10:42:36
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-03 11:40:00
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
        #region Methods

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

        #endregion
    }
}
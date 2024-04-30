/*
 * ================================================
 * Describe:        This is all managers interface.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-05-13:53:45
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2022-05-05-13:53:45
 * Version:         1.0
 * ===============================================
 */
namespace EasyFramework
{
    /// <summary>
    /// Manager interface.
    /// <para>管理器接口</para>
    /// </summary>
    public interface IManager  : ISingleton
    {
        /// <summary>
        /// Level of the m_managerLevel. The smaller the number, the update function performs faster, the quit function is also later .
        /// <para>管理器的级别，数越小更新越先执行，同时退出也越后</para>
        /// </summary>
        internal int ManagerLevel { get; }
    }
}

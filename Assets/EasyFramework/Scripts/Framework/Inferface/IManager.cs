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
    /// </summary>
    public interface IManager  : ISingleton
    {
        /// <summary>
        /// Level of the manager. The smaller the number, the sooner it is executed.管理器的级别，数越小越先执行
        /// </summary>
        internal int ManagerLevel { get; }
    }
}

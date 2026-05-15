/*
 * ================================================
 * Describe:      系统拥有者接口，主要用来规范系统的归属
 * Author:        Alvin5100
 * CreationTime:  2026-05-15 18:23:14
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-15 18:23:14
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework
{
    /// <summary>
    /// 系统拥有者接口，主要用来规范系统的归属
    /// <para>System owner, mainly used to define the ownership of the system.</para>
    /// </summary>
    public interface ISystemOwner
    {
        /// <summary>
        /// 注册系统
        /// </summary>
        /// <param name="system">系统</param>
        void RegisterSystem(ISystem system);

        /// <summary>
        /// 卸载系统
        /// <para>Offloading system</para>
        /// </summary>
        /// <typeparam name="T">要被卸载的系统类型<para>The type of system to be uninstalled</para></typeparam>
        void UnregisterSystem<T>() where T : ISystem;

        /// <summary>
        /// 卸载系统
        /// <para>Offloading system</para>
        /// </summary>
        /// <param name="system">要被卸载的系统<para>The system to be uninstalled</para></param>
        void UnregisterSystem(ISystem system);

        /// <summary>
        /// 访问系统
        /// <para>Call system</para>
        /// </summary>
        /// <typeparam name="T">系统类型<para>The type of system</para></typeparam>
        bool GetSystem<T>(out T system) where T : ISystem;

        /// <summary>
        /// 替换系统
        /// <para>Change the system</para>
        /// </summary>
        /// <param name="system">要被替换的系统<para>The system to be replaced</para></param>
        void ChangeSystem<T>(T system) where T : ISystem;
    }
}
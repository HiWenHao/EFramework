/*
 * ================================================
 * Describe:      HTTP 服务生命周期契约。四个策略接口统一继承本接口，由 HttpManager 门面驱动。
 * Author:        Alvin8412
 * CreationTime:  2026-11-13 14:40
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-13 14:40
 * ScriptVersion: 1.0
 * ================================================
 */

namespace EasyFramework.Systems.Http
{
    /// <summary>
    /// HTTP 服务生命周期接口
    /// <para>Lifecycle contract for HTTP services</para>
    /// </summary>
    public interface IHttpService
    {
        /// <summary>
        /// 初始化并绑定共享上下文。门面在 <see cref="EasyFramework.ISingleton"/>.Init 内调用。
        /// <para>Initialize with the shared context, called by the facade during <see cref="EasyFramework.ISingleton"/>.Init.</para>
        /// </summary>
        /// <param name="context">共享运行时上下文 <para>Shared runtime context.</para></param>
        void Initialize(HttpContext context);

        /// <summary>
        /// 释放运行时资源并解绑上下文。门面在 <see cref="EasyFramework.ISingleton"/>.Quit 内逆序调用。
        /// <para>Release runtime resources, called by the facade during <see cref="EasyFramework.ISingleton"/>.Quit in reverse order.</para>
        /// </summary>
        void Dispose();
    }
}
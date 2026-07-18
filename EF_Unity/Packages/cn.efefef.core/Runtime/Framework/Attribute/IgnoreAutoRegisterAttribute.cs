/*
 * ================================================
 * Describe:      用来忽略单例的自动注册EF统一管理，有些单例并不是全局的，可能只存在某个场景下。
 * Author:        Alvin8412
 * CreationTime:  2026-05-16 16:10:25
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-16 16:10:25
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework
{
    /// <summary>
    /// 忽略自动注册到EF中, 仅仅使用单例Instance, 相关生命周期开发者自己维护
    /// <para>Ignore the automatic registration in EF, and only use the singleton Instance. The relevant lifecycle will be maintained by the developers themselves.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class IgnoreAutoRegisterAttribute : BaseAttribute { }
}

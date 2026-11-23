/*
 * ================================================
 * Describe:        The class singleton base.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-14:33:01
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-21
 * ScriptVersion:   1.1
 * Changelog:
 *   1.1  移除 static readonly Lazy，改以 EFC 注册表为唯一真源，
 *        解决禁用域重载 / QuitGames 后重入拿到已销毁实例的问题。
 * ===============================================
 */

namespace EasyFramework
{
    public abstract class Singleton<T> where T : class, ISingleton, new()
    {
        protected Singleton()
        {
        }

        /// <summary>
        /// Current type name
        /// <para>当前类型名字</para>
        /// </summary>
        public string Name => typeof(T).Name;

        /// <summary>
        /// 全局唯一实例。委托 EFC.GetOrCreate&lt;T&gt;()，以注册表为唯一真源。
        /// <para>Global instance. Delegates to EFC.GetOrCreate&lt;T&gt;(), registry is the single source of truth.</para>
        /// <remarks>
        /// QuitGames 清空注册表后，下次访问会自然重建，不再返回已 Quit 的旧实例。
        /// <para>After QuitGames clears the registry, the next access recreates the instance.</para>
        /// </remarks>
        /// </summary>
        public static T Instance => EFC.GetOrCreate<T>();
    }
}
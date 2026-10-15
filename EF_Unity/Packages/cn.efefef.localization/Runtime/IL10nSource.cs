/*
 * ================================================
 * Describe:        本地化数据源契约：包与具体翻译来源之间的唯一接缝
 * Author:          Alvin5100
 * CreationTime:    2026-10-14 17:10:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-14 17:10:00
 * ScriptVersion:   0.1
 * ================================================
 */

namespace EasyFramework.Localization
{
    /// <summary>
    /// 本地化数据源
    /// <para>翻译文案的唯一取数入口。实现方由使用者在自己的工程里提供
    /// （典型实现是读取 Luban 生成的 <c>Cfg.DatableMgr</c>），并在启动时
    /// 通过 <see cref="I18NManager.RegisterSource"/> 注册给 <see cref="I18NManager"/>。</para>
    ///
    /// <para><b>为什么需要这一层：</b>本包要通过 UPM 分发给不特定的工程，
    /// 而翻译数据的存储方案（配置表 / 远端下发 / 手写字典）因人而异，
    /// 且 Luban 生成代码位于每个使用者自己的 <c>Assets/</c> 下、不属于本包。
    /// 若包内直接 <c>using Cfg</c>，则包与使用者工程产生硬耦合，
    /// 别人克隆包后因缺少 <c>EF.Luban.Generated</c> 程序集而导致整包编译失败。
    /// 这里用接口把这条接缝反转过来，依赖方向变为
    /// <c>使用者代码 → 包</c>，包对具体存储方案零认知。</para>
    ///
    /// <para>The seam between this package and whatever actually stores translations.
    /// Implemented and registered by the consuming project, never by the package itself.</para>
    /// </summary>
    public interface IL10nSource
    {
        /// <summary>
        /// 按 key 与指定语言取译文
        /// <para>刻意做成<b>无状态</b>：语言由参数传入而不是由实现方保存，
        /// 这样实现方可以纯查表、不必跟着 <see cref="I18NManager"/> 的切换逻辑走，
        /// 多语言回落也才能集中在 <see cref="I18NManager.Get"/> 一处实现。</para>
        /// <para>Stateless by design: the language comes in as a parameter rather than
        /// being stored by the implementation, so fallback logic lives in one place.</para>
        /// </summary>
        /// <param name="key">本地化 key</param>
        /// <param name="language">
        /// 语言代码，取值见 <see cref="LanguageCode"/>；实现方需能处理不支持的语言
        /// （返回 <c>null</c> 而非抛异常，由 <see cref="I18NManager"/> 决定回落）
        /// </param>
        /// <returns>译文；该 key 在该语言下不存在时返回 <c>null</c> 或空串</returns>
        string Get(string key, string language);
    }
}
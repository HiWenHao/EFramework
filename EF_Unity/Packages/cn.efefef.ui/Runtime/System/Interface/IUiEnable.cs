/*
 * ================================================
 * Describe:         把UI视窗启用的函数独立成接口，增加UI视窗的使用灵活性
 * Author:           Alvin8412
 * CreationTime:     2026-07-01 18:05:32
 * ModifyAuthor:     Alvin8412
 * ModifyTime:       2026-07-02
 * ScriptVersion:    0.2
 * ================================================
 */

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// UI面板每次打开时
    /// <para>Every time the UI view is opened</para>
    /// </summary>
    public interface IUiEnable
    {
        /// <summary>
        /// 当页面被激活时
        /// <para>When the view is activated</para>
        /// </summary>
        void Enable();
    }

    /// <inheritdoc cref="IUiEnable"/>
    public interface IUiEnable<in TArgs1>
    {
        /// <inheritdoc cref="IUiEnable.Enable"/>
        void Enable(TArgs1 args1);
    }


    /// <inheritdoc cref="IUiEnable"/>
    public interface IUiEnable<in TArgs1, in TArgs2>
    {
        /// <inheritdoc cref="IUiEnable.Enable"/>
        void Enable(TArgs1 args1, TArgs2 args2);
    }

    /// <inheritdoc cref="IUiEnable"/>
    public interface IUiEnable<in TArgs1, in TArgs2, in TArgs3>
    {
        /// <inheritdoc cref="IUiEnable.Enable"/>
        void Enable(TArgs1 args1, TArgs2 args2, TArgs3 args3);
    }

    /// <inheritdoc cref="IUiEnable"/>
    public interface IUiEnable<in TArgs1, in TArgs2, in TArgs3, in TArgs4>
    {
        /// <inheritdoc cref="IUiEnable.Enable"/>
        void Enable(TArgs1 args1, TArgs2 args2, TArgs3 args3, TArgs4 args4);
    }

    /// <inheritdoc cref="IUiEnable"/>
    public interface IUiEnable<in TArgs1, in TArgs2, in TArgs3, in TArgs4, in TArgs5>
    {
        /// <inheritdoc cref="IUiEnable.Enable"/>
        void Enable(TArgs1 args1, TArgs2 args2, TArgs3 args3, TArgs4 args4, TArgs5 args5);
    }
}
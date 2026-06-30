/*
 * ================================================
 * Describe:      UI视窗参数基类，提供Direction传递方向控制和Payload携带
 * Author:        Alvin8412
 * CreationTime:  2026-06-30
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-06-30
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// UI视窗参数基类 — 提供参数传递方向控制
    /// <para>UI view arguments base class — controls which lifecycle callback receives the args</para>
    /// </summary>
    public abstract class UiViewArgs
    {
        /// <summary>
        /// 参数传递方向，默认 Both
        /// <para>Argument direction, default Both</para>
        /// </summary>
        public UiArgsDirection Direction { get; set; } = UiArgsDirection.Both;
    }

    /// <summary>
    /// UI视窗参数泛型基类
    /// <para>UI View Parameter Generic Base Class</para>
    /// </summary>
    public class UiViewArgs<T1> : UiViewArgs
    {
        public UiViewArgs()
        {
        }

        public UiViewArgs(T1 args1)
        {
            Args1 = args1;
        }

        public T1 Args1 { get; set; }
    }

    /// <summary>
    /// UI视窗参数泛型基类
    /// <para>UI View Parameter Generic Base Class</para>
    /// </summary>
    public class UiViewArgs<T1, T2> : UiViewArgs
    {
        public UiViewArgs()
        {
        }

        public UiViewArgs(T1 args1, T2 args2)
        {
            Args1 = args1;
            Args2 = args2;
        }

        public T1 Args1 { get; set; }
        public T2 Args2 { get; set; }
    }

    /// <summary>
    /// UI视窗参数泛型基类
    /// <para>UI View Parameter Generic Base Class</para>
    /// </summary>
    public class UiViewArgs<T1, T2, T3> : UiViewArgs
    {
        public UiViewArgs()
        {
        }

        public UiViewArgs(T1 args1, T2 args2, T3 args3)
        {
            Args1 = args1;
            Args2 = args2;
            Args3 = args3;
        }

        public T1 Args1 { get; set; }
        public T2 Args2 { get; set; }
        public T3 Args3 { get; set; }
    }

    /// <summary>
    /// UI视窗参数泛型基类
    /// <para>UI View Parameter Generic Base Class</para>
    /// </summary>
    public class UiViewArgs<T1, T2, T3, T4> : UiViewArgs
    {
        public UiViewArgs()
        {
        }

        public UiViewArgs(T1 args1, T2 args2, T3 args3, T4 args4)
        {
            Args1 = args1;
            Args2 = args2;
            Args3 = args3;
            Args4 = args4;
        }

        public T1 Args1 { get; set; }
        public T2 Args2 { get; set; }
        public T3 Args3 { get; set; }
        public T4 Args4 { get; set; }
    }

    /// <summary>
    /// UI视窗参数泛型基类
    /// <para>UI View Parameter Generic Base Class</para>
    /// </summary>
    public class UiViewArgs<T1, T2, T3, T4, T5> : UiViewArgs
    {
        public UiViewArgs()
        {
        }

        public UiViewArgs(T1 args1, T2 args2, T3 args3, T4 args4, T5 args5)
        {
            Args1 = args1;
            Args2 = args2;
            Args3 = args3;
            Args4 = args4;
            Args5 = args5;
        }

        public T1 Args1 { get; set; }
        public T2 Args2 { get; set; }
        public T3 Args3 { get; set; }
        public T4 Args4 { get; set; }
        public T5 Args5 { get; set; }
    }
}
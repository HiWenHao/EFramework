/* 
 * ================================================
 * Describe:      This script is used to define every function. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-10-25 11:26:05
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-10-25 11:26:05
 * ScriptVersion: 0.1
 * ===============================================
*/

namespace EasyFramework
{
    /// <summary>
    /// A function with no arguments and no return value. 一个无参无返回值的函数
    /// </summary>
    public delegate void EAction();

    #region A function with has arguments and no return value.有参无返回值的函数
    /// <summary>
    /// A function with one argument and no return value. 一个有参无返回值的函数
    /// </summary>
    /// <typeparam name="T">The arguments type.参数类型</typeparam>
    /// <param name="arg">The arguments.参数</param>
    public delegate void EAction<in T>(T arg);

    /// <summary>
    /// A function with arguments and no return value. 一个有参无返回值的函数
    /// </summary>
    /// <typeparam name="T1">The arguments type.参数类型</typeparam>
    /// <typeparam name="T2">The arguments type.参数类型</typeparam>
    /// <param name="arg1">The arguments.参数</param>
    /// <param name="arg2">The arguments.参数</param>
    public delegate void EAction<in T1, in T2>(T1 arg1, T2 arg2);

    /// <summary>
    /// A function with arguments and no return value. 一个有参无返回值的函数
    /// </summary>
    /// <typeparam name="T1">The arguments type.参数1类型</typeparam>
    /// <typeparam name="T2">The arguments type.参数2类型</typeparam>
    /// <typeparam name="T3">The arguments type.参数3类型</typeparam>
    /// <param name="arg1">The arguments.第1参数</param>
    /// <param name="arg2">The arguments.第2参数</param>
    /// <param name="arg3">The arguments.第3参数</param>
    public delegate void EAction<in T1, in T2, in T3>(T1 arg1, T2 arg2, T2 arg3);
    #endregion
}

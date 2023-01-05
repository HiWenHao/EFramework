/* 
 * ================================================
 * Describe:      This script is used to define every function. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-10-25 15:14:04
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-10-25 15:14:04
 * ScriptVersion: 0.1
 * ===============================================
*/

namespace EasyFramework
{
    /// <summary>
    /// A function with return value but no arguments. 一个无参但有指定返回值类型的函数
    /// </summary>
    /// <typeparam name="V">此委托封装的方法的返回值类型。</typeparam>
    /// <returns>此委托封装的方法的返回值。</returns>
    public delegate V EFunction<out V>();

    #region A function with has arguments and return value.有参有返回值的函数
    /// <summary>
    /// A function with has one argument and return value.有1个参数且有返回值的函数
    /// </summary>
    /// <typeparam name="V">The return value type.返回值类型</typeparam>
    /// <typeparam name="T">The argument type.参数类型</typeparam>
    /// <param name="arg">The argument.参数</param>
    /// <returns>The return value of this function.这个函数的返回值</returns>
    public delegate V EFunction<in T, out V>(T arg);

    /// <summary>
    /// A function with has arguments and return value.有2个参数且有返回值的函数
    /// </summary>
    /// <typeparam name="V">The return value type.返回值类型</typeparam>
    /// <typeparam name="T1">The argument type.参数1类型</typeparam>
    /// <typeparam name="T2">The argument type.参数2类型</typeparam>
    /// <param name="arg1">The argument.参数1</param>
    /// <param name="arg2">The argument.参数2</param>
    /// <returns>The return value of this function.这个函数的返回值</returns>
    public delegate V EFunction<in T1, in T2, out V>(T1 arg1, T1 arg2);

    /// <summary>
    /// A function with has arguments and return value.有3个参数且有返回值的函数
    /// </summary>
    /// <typeparam name="V">The return value type.返回值类型</typeparam>
    /// <typeparam name="T1">The argument type.参数1类型</typeparam>
    /// <typeparam name="T2">The argument type.参数2类型</typeparam>
    /// <typeparam name="T2">The argument type.参数3类型</typeparam>
    /// <param name="arg1">The argument.参数1</param>
    /// <param name="arg2">The argument.参数2</param>
    /// <param name="arg3">The argument.参数3</param>
    /// <returns>The return value of this function.这个函数的返回值</returns>
    public delegate V EFunction<in T1, in T2, in T3, out V>(T1 arg1, T1 arg2, T1 arg3);

    #endregion
}

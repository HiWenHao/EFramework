/*
 * ================================================
 * Describe:        The class singleton base.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-14:33:01
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2022-05-14:33:01
 * Version:         1.0
 * ===============================================
 */
using System;
using EasyFramework;

public abstract class Singleton<T> where T : class, ISingleton
{
    protected Singleton() { }
    /// <summary>
    /// Current type name
    /// <para>当前类型名字</para>
    /// </summary>
    public string Name = typeof(T).Name;
    public static T Instance => _instance;
    private static readonly T _instance = new Lazy<T>(delegate
    {
        T t = Activator.CreateInstance<T>();
        if (t is IManager)
            EF.Register(t as IManager);
        else
            EF.Register(t);
        t.Init();
        return t;
    }).Value;
}

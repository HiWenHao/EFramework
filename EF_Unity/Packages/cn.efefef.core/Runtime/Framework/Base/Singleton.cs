/*
 * ================================================
 * Describe:        The class singleton base.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-14:33:01
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-08 22:30:14
 * ScriptVersion:   0.2
 * ===============================================
 */

using System;
using EasyFramework;

public abstract class Singleton<T> where T : class, ISingleton, new()
{
    protected Singleton() { }
    
    /// <summary>
    /// Current type name
    /// <para>当前类型名字</para>
    /// </summary>
    public string Name => typeof(T).Name;
    
    public static T Instance => _instance.Value;

    private static readonly Lazy<T> _instance = new(() =>
    {
        T t = new T();
        if (t is IManager manager)
            EF.Register(manager);
        else
            EF.Register(t);
        t.Init();
        return t;
    });
}

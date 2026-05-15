/*
 * ================================================
 * Describe:        The class is monobehavior singleton base.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-14:33:01
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-08 22:42:46
 * ScriptVersion:   1.0
 * ===============================================
 */

using System;
using System.Collections.Generic;
using EasyFramework;
using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>, ISingleton
{
    public static T Instance => SelfLazy.Value;
    public string TypeName => typeof(T).Name;

    private static readonly HashSet<T> Registered = new();
    private static readonly Lazy<T> SelfLazy = new(CreateInstance);

    private static T CreateInstance()
    {
        var existing = FindObjectsByType<T>(FindObjectsSortMode.None);
        if (existing.Length > 0)
        {
            for (int i = 1; i < existing.Length; i++)
                Destroy(existing[i].gameObject);
            RegisterAndInit(existing[0]);
            return existing[0];
        }

        GameObject go = new($"[ {typeof(T).Name} ]");
        T instance = go.AddComponent<T>();
        RegisterAndInit(instance);
        return instance;
    }

    private static void RegisterAndInit(T instance)
    {
        if (!Registered.Add(instance)) return;
        if (instance is IManager manager)
        {
            instance.transform.SetParent(EF.Managers);
            EF.Register(manager);
        }
        else
        {
            instance.transform.SetParent(EF.Singleton);
            EF.Register(instance);
        }
        instance.Init();
    }

    protected virtual void OnDestroy()
    {
        Registered.Remove((T)this);
    }
}
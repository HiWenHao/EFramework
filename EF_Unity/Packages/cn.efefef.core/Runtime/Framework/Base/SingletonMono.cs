/*
 * ================================================
 * Describe:        The class is monobehavior singleton base.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-14:33:01
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-08 22:42:46
 * ScriptVersion:   0.2
 * ===============================================
 */

using System;
using EasyFramework;
using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>, ISingleton
{
    private static bool _isQuitting;
    private static bool _quitEventRegistered;
    private static readonly Lazy<T> _lazy = new(CreateInstance);

    public static T Instance => _isQuitting ? null : _lazy.Value;
    
    /// <summary>
    /// Current type name
    /// <para>当前类型名字</para>
    /// </summary>
    public string TypeName => typeof(T).Name;

    private static T CreateInstance()
    {
        if (!_quitEventRegistered)
        {
            Application.quitting += OnApplicationQuitting;
            _quitEventRegistered = true;
        }

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

    private static void OnApplicationQuitting()
    {
        _isQuitting = true;
        if (_lazy.IsValueCreated)
            Destroy(_lazy.Value.gameObject);
    }

    private static void RegisterAndInit(T instance)
    {
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
}

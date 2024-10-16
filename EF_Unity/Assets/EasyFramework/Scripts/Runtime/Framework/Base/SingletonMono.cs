﻿/*
 * ================================================
 * Describe:        The class is monobehavior singleton base.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-14:33:01
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2022-05-14:33:01
 * Version:         1.0
 * ===============================================
 */
using EasyFramework;
using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>, ISingleton, new()
{
    protected MonoSingleton() { }
    /// Current type name
    /// <para>当前类型名字</para>
    /// </summary>
    public string Name = typeof(T).Name;
    private static T _instance;
    public static T Instance {
        get{
            if (null == _instance)
            {
                _instance = FindObjectOfType<T>();
                if (_instance == null)
                {
                    string[] _names = typeof(T).ToString().Split('.');
                    _instance = new GameObject(_names[^1]).AddComponent<T>();
                }
                if (_instance is IManager)
                {
                    _instance.transform.SetParent(EF.Managers);
                    EF.Register(_instance as IManager);
                }
                else
                {
                    _instance.transform.SetParent(EF.Singleton);
                    EF.Register(_instance);
                }
                _instance.Init();
            }
            return _instance;
        }
    }

    private void OnApplicationQuit()
    {
        Destroy(_instance);
        Destroy(gameObject);
        _instance = null;
    }
}

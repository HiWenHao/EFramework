/*
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
using UnityEngine.Serialization;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>, ISingleton, new()
{
    protected MonoSingleton() { }
    
    /// <summary>
    /// Current type name
    /// <para>当前类型名字</para>
    /// </summary>
    public string TypeName = typeof(T).Name;
    
    private static T _instance;
    public static T Instance {
        get{
            if (null != _instance) 
                return _instance;
            
            _instance = FindObjectOfType<T>();
            if (_instance == null)
            {
                _instance = new GameObject(typeof(T).Name).AddComponent<T>();
            }
            if (_instance is IManager manager)
            {
                _instance.transform.SetParent(EF.Managers);
                EF.Register(manager);
            }
            else
            {
                _instance.transform.SetParent(EF.Singleton);
                EF.Register(_instance);
            }
            _instance.Init();
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

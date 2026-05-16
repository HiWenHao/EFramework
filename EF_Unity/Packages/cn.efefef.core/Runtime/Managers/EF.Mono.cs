/*
 * ================================================
 * Describe:      This script is used to control the all managers.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-06-17 16:31:29
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-16 00:09:01
 * Version:       1.0
 * ===============================================
 */

using EasyFramework;
using System;
using System.Collections.Generic;
using System.Reflection;
using EasyFramework.Managers;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class EF : MonoBehaviour
{
    private static EF _monoEF;

    private static bool _inRunning;

    private static List<ISingleton> _managers;
    private static List<ISingleton> _singletons;
    private static List<IUpdate> _updater;
    private static List<IFixedUpdate> _fixedUpdaters;
    private static List<ILateUpdate> _lateUpdaters;

    private static HashSet<Type> _resolvingSet;
    private static Dictionary<Type, int> _managerOrderCache;
    private static Dictionary<Type, ISingleton> _singletonMap;

    private static readonly object _lockObj = new object();

    private EF()
    {
    }

    #region Lifecycle / 生命周期

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void BeforeSplashScreen() => SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Initialize()
    {
        Managers = new GameObject("GM.Managers").transform;
        Singleton = new GameObject("GM.Singleton").transform;
        _monoEF = Managers.gameObject.AddComponent<EF>();

        DontDestroyOnLoad(Managers);
        DontDestroyOnLoad(Singleton);

        _updater = new List<IUpdate>();
        _managers = new List<ISingleton>();
        _singletons = new List<ISingleton>();
        _lateUpdaters = new List<ILateUpdate>();
        _fixedUpdaters = new List<IFixedUpdate>();

        _managerOrderCache = new Dictionary<Type, int>();
        _singletonMap = new Dictionary<Type, ISingleton>();
        _resolvingSet = new HashSet<Type>();

        Projects = Resources.Load<ProjectConfig>("Configs/ProjectConfig");

        _inRunning = true;
    }

    private void Update()
    {
        if (!_inRunning) return;
        lock (_lockObj)
        {
            for (int i = 0; i < _updater.Count; i++)
            {
                _updater[i].Update(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!_inRunning) return;
        lock (_lockObj)
        {
            for (int i = 0; i < _fixedUpdaters.Count; i++)
            {
                _fixedUpdaters[i].FixedUpdate(Time.fixedDeltaTime);
            }
        }
    }

    private void LateUpdate()
    {
        if (!_inRunning) return;
        lock (_lockObj)
        {
            for (int i = 0; i < _lateUpdaters.Count; i++)
            {
                _lateUpdaters[i].LateUpdate(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }
    }

    private void OnDestroy() => QuitGames();
    private void OnApplicationQuit() => QuitGames();

    private static void QuitGames()
    {
        if (!_inRunning) return;
        _inRunning = false;
    
        lock (_lockObj)
        {
            // 先注销普通单例
            for (int i = _singletons.Count - 1; i >= 0; i--)
                Unregister(_singletons[i], destroyGameObject: true);
            _singletons.Clear();
        
            // 再注销管理器
            for (int i = _managers.Count - 1; i >= 0; i--)
                Unregister(_managers[i], destroyGameObject: true);
            _managers.Clear();
        
            _updater.Clear();
            _fixedUpdaters.Clear();
            _lateUpdaters.Clear();
            _singletonMap.Clear();
        }
        _resolvingSet.Clear();
        _managerOrderCache.Clear();
        
        _updater = null;
        _managers = null;
        _singletons = null;
        _resolvingSet = null;
        _lateUpdaters = null;
        _singletonMap = null;
        _fixedUpdaters = null;
        _managerOrderCache = null;
    }

    #endregion

    // 获取顺序
    private static int GetOrder(ISingleton singleton)
    {
        Type type = singleton.GetType();
        if (_managerOrderCache.TryGetValue(type, out int cached))
            return cached;
        var attr = type.GetCustomAttribute<ManagerAttribute>();
        int order = attr?.Order ?? 0;
        _managerOrderCache[type] = order;
        return order;
    }
    
    // 按order升序插入到列表中
    private static void InsertByOrder<T>(List<T> list, T item, int order)
    {
        int index = 0;
        for (; index < list.Count; index++)
        {
            int existingOrder = GetOrder((ISingleton)list[index]);
            if (order < existingOrder)
                break;
        }
        list.Insert(index, item);
    }

    #region 依赖解析和自动注册

    /// <summary>
    /// 尝试获取已注册的单例.
    /// <para>Try to get a registered singleton</para>
    /// </summary>
    public static bool TryGetSingleton<T>(out T singleton) where T : ISingleton
    {
        Type t = typeof(T);
        lock (_lockObj)
        {
            if (_singletonMap.TryGetValue(t, out var inst))
            {
                singleton = (T)inst;
                return true;
            }
        }

        singleton = default;
        return false;
    }

    /// <summary>
    /// 强制获取或创建单例
    /// <para>Force get or create a singleton</para>
    /// </summary>
    private static T GetOrCreateSingleton<T>() where T : class, ISingleton, new()
    {
        if (TryGetSingleton<T>(out var existing))
            return existing;
        return Singleton<T>.Instance;
    }

    /// <summary>
    /// 通过类型获取或创建单例
    /// <para>Get or create a singleton by type </para>
    /// </summary>
    private static void GetOrCreateSingletonByType(Type type)
    {
        if (type.IsValueType)
        {
            D.Error(
                $"Type {type.Name} cannot be used as a singleton dependency: must be a reference type (class). / 类型 {type.Name} 不能作为单例依赖：必须是引用类型。");
            return;
        }

        if (!typeof(ISingleton).IsAssignableFrom(type))
        {
            D.Error($"Type {type.Name} does not implement ISingleton. / 类型 {type.Name} 未实现 ISingleton 接口。");
            return;
        }

        if (type.GetConstructor(Type.EmptyTypes) == null)
        {
            D.Error($"Type {type.Name} must have a parameterless constructor. / 类型 {type.Name} 必须有一个无参构造函数。");
            return;
        }

        var method = typeof(EF).GetMethod(nameof(GetOrCreateSingleton), BindingFlags.NonPublic | BindingFlags.Static)
            ?.MakeGenericMethod(type);
        method?.Invoke(null, null);
    }

    /// <summary>
    /// 递归解析依赖，确保所有依赖已注册，并检测循环依赖
    /// <para>Recursively resolve dependencies, ensure all dependencies are registered, and detect circular dependencies.</para>
    /// </summary>
    private static void EnsureDependencies(Type managerType)
    {
        if (!_resolvingSet.Add(managerType))
            D.Exception($"[EasyFramework] Circular dependency detected: {managerType.Name} is already in the resolution chain.");

        object[] attributes = managerType.GetCustomAttributes(typeof(DependencyAttribute), false);
        List<Type> dependencies = new List<Type>();
        for (int i = 0; i < attributes.Length; i++)
        {
            if (attributes[i] is DependencyAttribute depAttr &&
                typeof(ISingleton).IsAssignableFrom(depAttr.DependencyType))
                dependencies.Add(depAttr.DependencyType);
        }

        foreach (Type dep in dependencies)
        {
            if (dep == managerType) continue;
            EnsureDependencies(dep);
            GetOrCreateSingletonByType(dep);
        }

        _resolvingSet.Remove(managerType);
    }

    #endregion

    #region Public methods / 公开函数

    /// <summary>
    /// Register a singleton.
    /// <para>注册一个单例</para>
    /// </summary>
    public static void Register(ISingleton item)
    {
        Type type = item.GetType();
        bool isManager = Attribute.IsDefined(type, typeof(ManagerAttribute));
        int order = GetOrder(item);   // 使用 GetOrder 获取（普通单例 order=0）

        EnsureDependencies(type);

        lock (_lockObj)
        {
            if (_singletonMap.ContainsKey(type))
                return;

            // 1. 插入到管理器列表或普通单例列表（仅用于生命周期管理，不影响更新顺序）
            if (isManager)
            {
                // 按 Order 插入 _managers
                int index = 0;
                for (; index < _managers.Count; index++)
                {
                    int existingOrder = GetOrder(_managers[index]);
                    if (order < existingOrder) break;
                }
                _managers.Insert(index, item);
            }
            else
                _singletons.Add(item);

            if (item is IUpdate update) InsertByOrder(_updater, update, order);
            if (item is IFixedUpdate fixedUpdater) InsertByOrder(_fixedUpdaters, fixedUpdater, order);
            if (item is ILateUpdate lateUpdater) InsertByOrder(_lateUpdaters, lateUpdater, order);

            if (item is MonoBehaviour mono)
                mono.transform.SetParent(isManager ? Managers : Singleton);

            _singletonMap[type] = item;
            item.Init();
        }
    }

    /// <summary>
    /// 注销一个单例
    /// <para>Unregister a singleton</para>
    /// </summary>
    public static void Unregister(ISingleton item, bool destroyGameObject = true)
    {
        Type type = item.GetType();
        bool isManager = Attribute.IsDefined(type, typeof(ManagerAttribute));

        lock (_lockObj)
        {
            if (!_singletonMap.ContainsKey(type))
                return;

            item.Quit();
            _singletonMap.Remove(type);

            if (isManager)
                _managers.Remove(item);
            else
                _singletons.Remove(item);

            if (item is IUpdate update) _updater.Remove(update);
            if (item is IFixedUpdate fixedUpdater) _fixedUpdaters.Remove(fixedUpdater);
            if (item is ILateUpdate lateUpdater) _lateUpdaters.Remove(lateUpdater);
        }

        if (destroyGameObject && item is MonoBehaviour mono)
            Destroy(mono.gameObject);
    }

    #endregion
}
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

    private static volatile bool _inRunning;

    private static List<ISingleton> _managers;
    private static List<ISingleton> _singletons;
    private static List<IUpdate> _updater;
    private static List<IFixedUpdate> _fixedUpdaters;
    private static List<ILateUpdate> _lateUpdaters;

    private static HashSet<Type> _resolvingSet;
    private static Dictionary<Type, int> _orderCache;
    private static Dictionary<Type, ISingleton> _singletonMap;
    private static Dictionary<Type, Func<object>> _monoSingletonInstanceGetters;
    private static readonly object LockObj = new object();

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

        _orderCache = new Dictionary<Type, int>();
        _singletonMap = new Dictionary<Type, ISingleton>();
        _resolvingSet = new HashSet<Type>();
        _monoSingletonInstanceGetters = new Dictionary<Type, Func<object>>();

        Projects = Resources.Load<ProjectConfig>("Configs/ProjectConfig");

        _inRunning = true;
    }

    private void Update()
    {
        if (!_inRunning) return;
        lock (LockObj)
        {
            for (int i = 0; i < _updater.Count; i++)
            {
                if (_updater[i].IsPaused) continue;
                _updater[i].Update(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!_inRunning) return;
        lock (LockObj)
        {
            for (int i = 0; i < _fixedUpdaters.Count; i++)
            {
                if (_fixedUpdaters[i].IsPaused) continue;
                _fixedUpdaters[i].FixedUpdate(Time.fixedDeltaTime);
            }
        }
    }

    private void LateUpdate()
    {
        if (!_inRunning) return;
        lock (LockObj)
        {
            for (int i = 0; i < _lateUpdaters.Count; i++)
            {
                if (_lateUpdaters[i].IsPaused) continue;
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

        lock (LockObj)
        {
            var allSingletons = new List<ISingleton>(_managers);
            allSingletons.AddRange(_singletons);
            allSingletons.Sort((a, b) => GetOrder(a).CompareTo(GetOrder(b)));

            foreach (var singleton in allSingletons)
                Unregister(singleton, destroyGameObject: true);

            _singletons.Clear();
            _managers.Clear();
            _updater.Clear();
            _fixedUpdaters.Clear();
            _lateUpdaters.Clear();
            _singletonMap.Clear();
        }
        _resolvingSet.Clear();
        _orderCache.Clear();
    }

    #endregion

    // 获取顺序
    private static int GetOrder(ISingleton singleton)
    {
        Type type = singleton.GetType();
        if (_orderCache.TryGetValue(type, out int cached))
            return cached;
        var attr = type.GetCustomAttribute<SingletonPriorityAttribute>(true);
        int order = attr?.Order ?? 0;
        _orderCache[type] = order;
        return order;
    }
    
    // 按 order 降序插入到列表中（大的在前）
    private static void InsertByOrder<T>(List<T> list, T item, int order)
    {
        int index = 0;
        for (; index < list.Count; index++)
        {
            int existingOrder = GetOrder((ISingleton)list[index]);
            if (order > existingOrder)
                break;
        }
        list.Insert(index, item);
    }

    private static object GetOrCreateMonoSingleton(Type type)
    {
        if (!_monoSingletonInstanceGetters.TryGetValue(type, out var getter))
        {
            var instanceProp = type.GetProperty("Instance",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (instanceProp == null || !instanceProp.CanRead)
            {
                D.Error($"Type {type.Name} is a MonoBehaviour singleton but has no static Instance property. Please ensure it derives from MonoSingleton<T>.");
                return null;
            }

            var methodInfo = instanceProp.GetMethod;
            getter = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), methodInfo);
            _monoSingletonInstanceGetters[type] = getter;
        }

        var instance = getter();
        if (instance is ISingleton singleton && !_singletonMap.ContainsKey(type))
            D.Warning($"MonoSingleton {type.Name} was created but not registered in EF. Ensure its Instance property calls EF.Register.");

        return instance;
    }

    #region 依赖解析和自动注册

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
            D.Error($"Type {type.Name} cannot be used as a singleton dependency: must be a reference type (class).");
            return;
        }

        if (!typeof(ISingleton).IsAssignableFrom(type))
        {
            D.Error($"Type {type.Name} does not implement ISingleton.");
            return;
        }

        if (typeof(MonoBehaviour).IsAssignableFrom(type))
        {
            GetOrCreateMonoSingleton(type);
            return;
        }

        // 普通单例：需要无参构造函数
        if (type.GetConstructor(Type.EmptyTypes) == null)
        {
            D.Error($"Type {type.Name} must have a parameterless constructor.");
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
        // 已注册的单例不再重复解析依赖
        lock (LockObj)
        {
            if (_singletonMap.ContainsKey(managerType))
                return;
        }

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

            if (Attribute.IsDefined(dep, typeof(IgnoreAutoRegisterAttribute)))
            {
                D.Error($"Dependency {dep.Name} is marked with IgnoreAutoRegister, cannot be used as a dependency of {managerType.Name}." +
                        "Remove the dependency or remove the IgnoreAutoRegister attribute.");
                continue;
            }
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

        lock (LockObj)
        {
            if (_singletonMap.ContainsKey(type))
                return;

            if (isManager)
                InsertByOrder(_managers, item, order);
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

        lock (LockObj)
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

    /// <summary>
    /// 尝试获取已注册的单例.
    /// <para>Try to get a registered singleton</para>
    /// </summary>
    public static bool TryGetSingleton<T>(out T singleton) where T : ISingleton
    {
        Type t = typeof(T);
        lock (LockObj)
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

    #endregion
}
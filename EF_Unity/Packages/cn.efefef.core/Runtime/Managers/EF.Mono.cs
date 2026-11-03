/*
 * ================================================
 * Describe:      This script is used to control the all managers.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-06-17 16:31:29
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-03 11:50:00
 * Version:       1.0
 * ===============================================
 */

using EasyFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EasyFramework.Managers;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 框架引擎：单例注册中心、三级轮询驱动、协程宿主与应用生命周期通知
/// <para>Framework engine: singleton registry, triple update driver, coroutine host and application lifecycle notification</para>
/// </summary>
public sealed class EFC : MonoBehaviour
{
    #region Fields

    // 框架运行标记，防止重复初始化与退出后仍轮询
    private static volatile bool _inRunning;

    // 带 ManagerAttribute 的核心管理器，按优先级降序
    private static List<ISingleton> _managers;
    // 普通业务单例
    private static List<ISingleton> _singletons;
    // 按优先级降序的帧更新列表
    private static List<IUpdate> _updater;
    // 按优先级降序的物理帧更新列表
    private static List<IFixedUpdate> _fixedUpdaters;
    // 按优先级降序的渲染帧更新列表
    private static List<ILateUpdate> _lateUpdaters;
    // 按优先级降序的应用暂停通知列表
    private static List<IOnAppPause> _pauseListeners;
    // 按优先级降序的应用焦点通知列表
    private static List<IOnAppFocus> _focusListeners;
    // 按优先级降序的应用退出通知列表
    private static List<IOnAppQuit> _quitListeners;
    // 按优先级降序的低内存告警通知列表
    private static List<IOnLowMemory> _lowMemoryListeners;

    // 依赖解析链，用于检测循环依赖
    private static HashSet<Type> _resolvingSet;
    // 单例优先级缓存，按类型记录
    private static Dictionary<Type, int> _orderCache;
    // 已注册单例映射，按类型索引
    private static Dictionary<Type, ISingleton> _singletonMap;
    // MonoSingleton 静态 Instance 取值委托缓存
    private static Dictionary<Type, Func<object>> _monoSingletonInstanceGetters;
    // 注册与轮询共用的锁对象
    private static readonly object LockObj = new object();

    // 协程宿主实例
    private static EFC _monoEFC;

    #endregion

    #region Properties
    /// <summary> Whether to enable debug logs. The default setting is yes.<para>是否开启调试日志，默认为是</para></summary>
    public static bool EnableDebug { get; set; } = true;

    /// <summary> The object for EF framework in scene.<para>场景中的EF对象</para></summary>
    public static Transform Managers { get; private set; }

    /// <summary> The singleton parent node in the scenario.<para>场景中单例父节点</para></summary>
    public static Transform Singleton { get; private set; }

    /// <summary> Project allocation resource.<para>项目配置资源</para></summary>
    public static ProjectConfig Projects { get; private set; }

    #endregion

    #region Methods

    // 防止外部实例化
    private EFC()
    {
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void BeforeSplashScreen() => SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);

    // 框架入口：创建宿主节点、初始化各容器、加载项目配置
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Initialize()
    {
        Managers = new GameObject("GM.Managers").transform;
        Singleton = new GameObject("GM.Singleton").transform;
        _monoEFC = Managers.gameObject.AddComponent<EFC>();

        DontDestroyOnLoad(Managers);
        DontDestroyOnLoad(Singleton);

        _updater = new List<IUpdate>();
        _managers = new List<ISingleton>();
        _singletons = new List<ISingleton>();
        _lateUpdaters = new List<ILateUpdate>();
        _fixedUpdaters = new List<IFixedUpdate>();
        _pauseListeners = new List<IOnAppPause>();
        _focusListeners = new List<IOnAppFocus>();
        _quitListeners = new List<IOnAppQuit>();
        _lowMemoryListeners = new List<IOnLowMemory>();

        _orderCache = new Dictionary<Type, int>();
        _singletonMap = new Dictionary<Type, ISingleton>();
        _resolvingSet = new HashSet<Type>();
        _monoSingletonInstanceGetters = new Dictionary<Type, Func<object>>();

        Projects = Resources.Load<ProjectConfig>("Configs/ProjectConfig");

        Application.lowMemory += _monoEFC.OnLowMemory;

        _inRunning = true;
    }

    private void Update()
    {
        if (!_inRunning) return;
        lock (LockObj)
        {
            for (var i = 0; i < _updater.Count; i++)
            {
                var updater = _updater[i];
                if (updater.IsPaused) continue;
                updater.Update(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!_inRunning) return;
        lock (LockObj)
        {
            for (var i = 0; i < _fixedUpdaters.Count; i++)
            {
                var fixedUpdater = _fixedUpdaters[i];
                if (fixedUpdater.IsPaused) continue;
                fixedUpdater.FixedUpdate(Time.fixedDeltaTime);
            }
        }
    }

    private void LateUpdate()
    {
        if (!_inRunning) return;
        lock (LockObj)
        {
            for (var i = 0; i < _lateUpdaters.Count; i++)
            {
                var lateUpdater = _lateUpdaters[i];
                if (lateUpdater.IsPaused) continue;
                lateUpdater.LateUpdate(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }
    }

    private void OnDestroy() => QuitGames();
    private void OnApplicationQuit() => QuitGames();

    private void OnApplicationPause(bool paused)
    {
        if (!_inRunning) return;
        lock (LockObj)
        {
            for (var i = 0; i < _pauseListeners.Count; i++)
            {
                var listener = _pauseListeners[i];
                listener.OnAppPause(paused);
            }
        }
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!_inRunning) return;
        lock (LockObj)
        {
            for (var i = 0; i < _focusListeners.Count; i++)
            {
                var listener = _focusListeners[i];
                listener.OnAppFocus(focused);
            }
        }
    }

    // 转发系统低内存告警给订阅者
    private void OnLowMemory()
    {
        if (!_inRunning) return;
        lock (LockObj)
        {
            for (var i = 0; i < _lowMemoryListeners.Count; i++)
            {
                var listener = _lowMemoryListeners[i];
                listener.OnLowMemory();
            }
        }
    }

    // 读取单例优先级，带类型级缓存
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

    // 反射获取 MonoSingleton 的静态 Instance 属性
    private static void GetOrCreateMonoSingleton(Type type)
    {
        if (!_monoSingletonInstanceGetters.TryGetValue(type, out var getter))
        {
            var instanceProp = type.GetProperty("Instance",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (instanceProp == null || !instanceProp.CanRead)
            {
                D.Error(
                    $"Type {type.Name} is a MonoBehaviour singleton but has no static Instance property. Please ensure it derives from MonoSingleton<T>.");
                return;
            }

            var methodInfo = instanceProp.GetMethod;
            getter = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), methodInfo);
            _monoSingletonInstanceGetters[type] = getter;
        }

        var instance = getter();
        if (instance is ISingleton singleton && !_singletonMap.ContainsKey(type))
            D.Warning(
                $"MonoSingleton {type.Name} was created but not registered in EF. Ensure its Instance property calls EF.Register.");
    }

    // 强制获取或创建单例
    private static T GetOrCreateSingleton<T>() where T : class, ISingleton, new()
    {
        if (TryGetSingleton<T>(out var existing))
            return existing;
        return Singleton<T>.Instance;
    }

    // 按类型获取或创建单例，分派 MonoSingleton 与普通单例两条路径
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

        var method = typeof(EFC).GetMethod(nameof(GetOrCreateSingleton), BindingFlags.NonPublic | BindingFlags.Static)
            ?.MakeGenericMethod(type);
        method?.Invoke(null, null);
    }

    // 递归解析依赖，确保所有依赖已注册，并检测循环依赖
    private static void EnsureDependencies(Type managerType)
    {
        // 已注册的单例不再重复解析依赖
        lock (LockObj)
        {
            if (_singletonMap.ContainsKey(managerType))
                return;
        }

        if (!_resolvingSet.Add(managerType))
            D.Exception(
                $"[EasyFramework] Circular dependency detected: {managerType.Name} is already in the resolution chain.");

        object[] attributes = managerType.GetCustomAttributes(typeof(DependencyAttribute), false);
        List<Type> dependencies = new List<Type>();
        foreach (var attribute in attributes)
        {
            if (attribute is DependencyAttribute depAttr &&
                typeof(ISingleton).IsAssignableFrom(depAttr.DependencyType))
                dependencies.Add(depAttr.DependencyType);
        }

        foreach (Type dep in dependencies)
        {
            if (dep == managerType) continue;

            if (Attribute.IsDefined(dep, typeof(IgnoreAutoRegisterAttribute)))
            {
                D.Error(
                    $"Dependency {dep.Name} is marked with IgnoreAutoRegister, cannot be used as a dependency of {managerType.Name}." +
                    "Remove the dependency or remove the IgnoreAutoRegister attribute.");
                continue;
            }

            EnsureDependencies(dep);
            GetOrCreateSingletonByType(dep);
        }

        _resolvingSet.Remove(managerType);
    }

    /// <summary>
    /// 退出框架：先按销毁顺序通知 IOnAppQuit 订阅者，再按优先级升序注销全部单例
    /// <para>Quit the framework: notifies IOnAppQuit subscribers in teardown order first, then unregisters all singletons in ascending priority</para>
    /// </summary>
    public static void QuitGames()
    {
        if (!_inRunning) return;
        _inRunning = false;

        Application.lowMemory -= _monoEFC.OnLowMemory;

        lock (LockObj)
        {
            // 与销毁同序（优先级升序）通知，低优先级先收到保存机会
            for (int i = _quitListeners.Count - 1; i >= 0; i--)
                _quitListeners[i].OnAppQuit();

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
            _pauseListeners.Clear();
            _focusListeners.Clear();
            _quitListeners.Clear();
            _lowMemoryListeners.Clear();
            _singletonMap.Clear();
        }

        _resolvingSet.Clear();
        _orderCache.Clear();
    }

    /// <summary>
    /// Register a singleton.
    /// <para>注册一个单例</para>
    /// </summary>
    /// <param name="item">待注册的单例实例<para>The singleton instance to register</para></param>
    public static void Register(ISingleton item)
    {
        Type type = item.GetType();
        bool isManager = Attribute.IsDefined(type, typeof(ManagerAttribute));
        int order = GetOrder(item); // 使用 GetOrder 获取（普通单例 order=0）

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
            if (item is IOnAppPause pauseListener) InsertByOrder(_pauseListeners, pauseListener, order);
            if (item is IOnAppFocus focusListener) InsertByOrder(_focusListeners, focusListener, order);
            if (item is IOnAppQuit quitListener) InsertByOrder(_quitListeners, quitListener, order);
            if (item is IOnLowMemory lowMemoryListener) InsertByOrder(_lowMemoryListeners, lowMemoryListener, order);

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
    /// <param name="item">待注销的单例实例<para>The singleton instance to unregister</para></param>
    /// <param name="destroyGameObject">是否同时销毁其 GameObject<para>Whether to destroy its GameObject as well</para></param>
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
            if (item is IOnAppPause pauseListener) _pauseListeners.Remove(pauseListener);
            if (item is IOnAppFocus focusListener) _focusListeners.Remove(focusListener);
            if (item is IOnAppQuit quitListener) _quitListeners.Remove(quitListener);
            if (item is IOnLowMemory lowMemoryListener) _lowMemoryListeners.Remove(lowMemoryListener);
        }

        if (destroyGameObject && item is MonoBehaviour mono)
            Destroy(mono.gameObject);
    }

    /// <summary>
    /// 尝试获取已注册的单例.
    /// <para>Try to get a registered singleton</para>
    /// </summary>
    /// <typeparam name="T">目标单例类型<para>The target singleton type</para></typeparam>
    /// <param name="singleton">获取到的单例实例<para>The retrieved singleton instance</para></param>
    /// <returns>是否已注册<para>Whether the singleton is registered</para></returns>
    public static bool TryGetSingleton<T>(out T singleton) where T : ISingleton
    {
        Type t = typeof(T);
        lock (LockObj)
        {
            if (null != _singletonMap && _singletonMap.TryGetValue(t, out var inst))
            {
                singleton = (T)inst;
                return true;
            }
        }

        singleton = default;
        return false;
    }

    /// <summary>
    /// Start a coroutine.
    /// <para>开启一个协程</para>
    /// </summary>
    /// <param name="coroutine">协程迭代器<para>The coroutine enumerator</para></param>
    /// <returns>协程句柄<para>The coroutine handle</para></returns>
    public static Coroutine StartCoroutines(IEnumerator coroutine)
    {
        return _monoEFC.StartCoroutine(coroutine);
    }

    /// <summary>
    /// Start a coroutine.
    /// <para>开启一个协程</para>
    /// </summary>
    /// <param name="coroutine">协程方法名字符串<para>The coroutine method name string</para></param>
    /// <param name="value">传递给协程的参数<para>The argument passed to the coroutine</para></param>
    /// <returns>协程句柄<para>The coroutine handle</para></returns>
    public static Coroutine StartCoroutines(string coroutine, object value)
    {
        if (null == value)
            return _monoEFC.StartCoroutine(coroutine);
        return _monoEFC.StartCoroutine(coroutine, value);
    }

    /// <summary>
    /// Stop a coroutines.
    /// <para>停止一个协程</para>
    /// </summary>
    /// <param name="coroutine">协程句柄<para>The coroutine handle</para></param>
    public static void StopCoroutines(Coroutine coroutine)
    {
        _monoEFC.StopCoroutine(coroutine);
    }

    /// <summary>
    /// Stop a coroutines.
    /// <para>停止一个协程</para>
    /// </summary>
    /// <param name="coroutine">协程迭代器<para>The coroutine enumerator</para></param>
    public static void StopCoroutines(IEnumerator coroutine)
    {
        _monoEFC.StopCoroutine(coroutine);
    }

    /// <summary>
    /// Stop a coroutines.
    /// <para>停止一个协程</para>
    /// </summary>
    /// <param name="methodName">协程方法名字符串<para>The coroutine method name string</para></param>
    public static void StopCoroutines(string methodName)
    {
        _monoEFC.StopCoroutine(methodName);
    }

    /// <summary>
    /// Stop all coroutines and use with caution.
    /// <para>停止所有协程, 谨慎使用</para>
    /// </summary>
    public static void StopAllCoroutine()
    {
        _monoEFC.StopAllCoroutines();
    }

    #endregion
}
/*
 * ================================================
 * Describe:      This script is used to control the all managers.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-06-17 16:31:29
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-21
 * Version:       1.1
 */

using EasyFramework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class EFC : MonoBehaviour
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

    // 每帧路径的快照缓存：稳态零分配，注册/注销后的首帧重建
    private static SnapshotCache<IUpdate> _updaterView;
    private static SnapshotCache<IFixedUpdate> _fixedView;
    private static SnapshotCache<ILateUpdate> _lateView;

    // 协程宿主实例
    private static EFC _monoEFC;

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

        _managers = new List<ISingleton>();
        _singletons = new List<ISingleton>();

        _updater = new List<IUpdate>();
        _fixedUpdaters = new List<IFixedUpdate>();
        _lateUpdaters = new List<ILateUpdate>();

        _pauseListeners = new List<IOnAppPause>();
        _focusListeners = new List<IOnAppFocus>();
        _quitListeners = new List<IOnAppQuit>();
        _lowMemoryListeners = new List<IOnLowMemory>();

        _updaterView = new SnapshotCache<IUpdate>(_updater, LockObj);
        _fixedView = new SnapshotCache<IFixedUpdate>(_fixedUpdaters, LockObj);
        _lateView = new SnapshotCache<ILateUpdate>(_lateUpdaters, LockObj);

        _resolvingSet = new HashSet<Type>();
        _orderCache = new Dictionary<Type, int>();
        _singletonMap = new Dictionary<Type, ISingleton>();
        _monoSingletonInstanceGetters = new Dictionary<Type, Func<object>>();

        Projects = Resources.Load<ProjectConfig>("Configs/ProjectConfig");
        // 点名报错：否则下游解引用 ProjectConfig 只表现为裸 NRE，掩盖真正原因
        if (Projects == null)
            D.Error("[ EFC ] 找不到 Resources/Configs/ProjectConfig，语言默认值与内置资源路径不可用。请确认资产位于某个 Resources 目录下。");

        Application.lowMemory += _monoEFC.OnLowMemory;

        _inRunning = true;
    }

    private void Update()
    {
        if (!_inRunning) return;
        foreach (var updater in _updaterView.Get())
        {
            if (updater.IsPaused) continue;
            updater.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (!_inRunning) return;
        foreach (var fixedUpdater in _fixedView.Get())
        {
            if (fixedUpdater.IsPaused) continue;
            fixedUpdater.FixedUpdate(Time.fixedDeltaTime);
        }
    }

    private void LateUpdate()
    {
        if (!_inRunning) return;
        foreach (var lateUpdater in _lateView.Get())
        {
            if (lateUpdater.IsPaused) continue;
            lateUpdater.LateUpdate(Time.deltaTime, Time.unscaledDeltaTime);
        }
    }

    private void OnDestroy() => QuitGames();
    private void OnApplicationQuit() => QuitGames();

    private void OnApplicationPause(bool paused) => Dispatch(_pauseListeners, l => l.OnAppPause(paused));

    private void OnApplicationFocus(bool focused) => Dispatch(_focusListeners, l => l.OnAppFocus(focused));

    private void OnLowMemory() => Dispatch(_lowMemoryListeners, l => l.OnLowMemory());

    // 锁内取快照、锁外派发：回调内重入 Register/Unregister 改原列表也不会让本次遍历错位
    private void Dispatch<T>(List<T> listeners, Action<T> action)
    {
        if (!_inRunning) return;
        T[] snapshot;
        lock (LockObj)
        {
            snapshot = listeners.ToArray();
        }

        foreach (var listener in snapshot)
            action(listener);
    }

    // 读取单例优先级，带类型级缓存； Register 可跨线程调用， Dictionary 读写需上锁
    private static int GetOrder(ISingleton singleton)
    {
        Type type = singleton.GetType();
        lock (LockObj)
        {
            if (_orderCache.TryGetValue(type, out int cached))
                return cached;
        }

        int order = type.GetCustomAttribute<SingletonPriorityAttribute>(true)?.Order ?? 0;
        lock (LockObj)
        {
            _orderCache[type] = order;
        }

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

    // 注册各更新/通知阶段的监听器（必须在 LockObj 内调用）
    private static void AddToPhases(ISingleton item, int order)
    {
        if (item is IUpdate update) { InsertByOrder(_updater, update, order); _updaterView.Invalidate(); }
        if (item is IFixedUpdate fixedUpdater) { InsertByOrder(_fixedUpdaters, fixedUpdater, order); _fixedView.Invalidate(); }
        if (item is ILateUpdate lateUpdater) { InsertByOrder(_lateUpdaters, lateUpdater, order); _lateView.Invalidate(); }
        if (item is IOnAppPause pauseListener) InsertByOrder(_pauseListeners, pauseListener, order);
        if (item is IOnAppFocus focusListener) InsertByOrder(_focusListeners, focusListener, order);
        if (item is IOnAppQuit quitListener) InsertByOrder(_quitListeners, quitListener, order);
        if (item is IOnLowMemory lowMemoryListener) InsertByOrder(_lowMemoryListeners, lowMemoryListener, order);
    }

    // 注销各更新/通知阶段的监听器（必须在 LockObj 内调用）
    private static void RemoveFromPhases(ISingleton item)
    {
        if (item is IUpdate update) { _updater.Remove(update); _updaterView.Invalidate(); }
        if (item is IFixedUpdate fixedUpdater) { _fixedUpdaters.Remove(fixedUpdater); _fixedView.Invalidate(); }
        if (item is ILateUpdate lateUpdater) { _lateUpdaters.Remove(lateUpdater); _lateView.Invalidate(); }
        if (item is IOnAppPause pauseListener) _pauseListeners.Remove(pauseListener);
        if (item is IOnAppFocus focusListener) _focusListeners.Remove(focusListener);
        if (item is IOnAppQuit quitListener) _quitListeners.Remove(quitListener);
        if (item is IOnLowMemory lowMemoryListener) _lowMemoryListeners.Remove(lowMemoryListener);
    }

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
        if (instance is ISingleton && !_singletonMap.ContainsKey(type))
            D.Warning(
                $"MonoSingleton {type.Name} was created but not registered in EF. Ensure its Instance property calls EF.Register.");
    }

    // 强制获取或创建单例（依赖解析内部用）
    private static T GetOrCreateSingleton<T>() where T : class, ISingleton, new()
    {
        return GetOrCreate<T>();
    }

    // Mono 单例走反射 Instance，普通单例走泛型工厂——两条路径互不干扰
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

        lock (LockObj)
        {
            if (!_resolvingSet.Add(managerType))
            {
                D.Exception($"[EasyFramework] Circular dependency detected: [ {managerType.Name} ] is already in the resolution chain.");
                return;
            }
        }

        try
        {
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
        }
        finally
        {
            // 异常（如循环依赖 fail-fast）时也必须清理，否则 _resolvingSet 残留类型，下次误报循环依赖
            lock (LockObj)
            {
                _resolvingSet.Remove(managerType);
            }
        }
    }

    #endregion

    // 每类型独立；[ThreadStatic] 提供线程隔离，跨线程并发靠 Register 判重兜底
    private static class CreationGuard<T>
    {
        [ThreadStatic] internal static bool Creating;
    }

    // IgnoreAutoRegister 类型的缓存容器（泛型静态，每类型独立；Lazy 保证线程安全）
    private static class IgnoredCache<T> where T : class, ISingleton, new()
    {
        internal static readonly Lazy<T> Lazy = new(() => new T());
    }

    // List 未变时复用数组消除每帧 GC；Invalidate 必须与 List 改动同在 LockObj 内调用
    private sealed class SnapshotCache<T>
    {
        private readonly List<T> _list;
        private readonly object _gate;
        private T[] _cache = Array.Empty<T>();
        private int _version;
        private int _cacheVersion = -1;

        public SnapshotCache(List<T> list, object gate)
        {
            _list = list;
            _gate = gate;
        }

        // 写方在持锁修改 List 后调用；版本递增即让旧快照失效
        public void Invalidate() => _version++;

        // 读方每帧调用：稳态直接返回缓存数组（零分配），仅版本变化时重建一次
        public T[] Get()
        {
            lock (_gate)
            {
                if (_cacheVersion != _version)
                {
                    _cache = _list.ToArray();
                    _cacheVersion = _version;
                }

                return _cache;
            }
        }
    }
}
/*
 * ================================================
 * Describe:        EFC 公开 API 分部：单例访问/注册注销、框架退出、协程宿主、调试快照。
 *                  内部字段、生命周期与辅助实现见 EF.Mono.cs。
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-06-17 16:31:29
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-21
 * Version:         1.1
 * ===============================================
 */

using EasyFramework;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 框架引擎（内部实现分部）：单例注册中心、三级轮询驱动、协程宿主与应用生命周期通知。
/// <para>Framework engine (internal partial): singleton registry, triple update driver, coroutine host and lifecycle.</para>
/// </summary>
public sealed partial class EFC
{
    #region Properties

    /// <summary> The object for EF framework in scene.<para>场景中的EF对象</para></summary>
    public static Transform Managers { get; private set; }

    /// <summary> The singleton parent node in the scenario.<para>场景中单例父节点</para></summary>
    public static Transform Singleton { get; private set; }

    /// <summary> Project allocation resource.<para>项目配置资源</para></summary>
    public static ProjectConfig Projects { get; private set; }

    #endregion

    #region Singleton Registry

    /// <summary>
    /// 获取或创建单例的原子入口：已注册直接返回；否则创建并注册，最终以注册表为准返回。
    /// <para>Atomic get-or-create: return registered instance, or create + register, always returning the registry instance.</para>
    /// </summary>
    /// <typeparam name="T">单例类型<para>Singleton type</para></typeparam>
    public static T GetOrCreate<T>() where T : class, ISingleton, new()
    {
        // IgnoreAutoRegister 不进注册表，生命周期自行管理，不随 QuitGames 清除
        if (Attribute.IsDefined(typeof(T), typeof(IgnoreAutoRegisterAttribute)))
            return IgnoredCache<T>.Lazy.Value;

        if (TryGetSingleton(out T existing))
            return existing;

        // 依赖解析可能回环到本 T（此时尚未写表）：fail-fast 而非无限递归
        if (CreationGuard<T>.Creating)
            throw new InvalidOperationException(
                $"[EFC] Re-entrant creation of {typeof(T).Name}. Check for circular dependencies.");

        CreationGuard<T>.Creating = true;
        try
        {
            T instance = new T();
            Register(instance);

            // 并发时 Register 可能因判重未注册 instance —— 以注册表为准
            return TryGetSingleton(out T registered) ? registered : instance;
        }
        finally
        {
            CreationGuard<T>.Creating = false;
        }
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
            // 集合已清空：让缓存失效（_inRunning 已 false 不会再走 Get，但保持写后失效的不变式）
            _updaterView?.Invalidate();
            _fixedView?.Invalidate();
            _lateView?.Invalidate();
            _pauseListeners.Clear();
            _focusListeners.Clear();
            _quitListeners.Clear();
            _lowMemoryListeners.Clear();
            _singletonMap.Clear();
            // 与 GetOrder/EnsureDependencies 的读写在同锁内互斥，避免退出时并发 Clear 损坏集合
            _resolvingSet.Clear();
            _orderCache.Clear();
        }
    }

    /// <summary>
    /// 取当前已注册的全部单例快照（供编辑器调试工具使用）
    /// <para>Snapshot of every registered singleton, for editor tooling and debug panels.</para>
    /// </summary>
    /// <returns>单例快照；框架未初始化时返回空集合<para>Singleton snapshot; empty when the framework is not initialized.</para></returns>
    public static IReadOnlyList<ISingleton> GetAllSingletons()
    {
        if (_singletonMap == null)
            return Array.Empty<ISingleton>();

        lock (LockObj)
        {
            var snapshot = new List<ISingleton>(_singletonMap.Count);
            foreach (var pair in _singletonMap)
                snapshot.Add(pair.Value);

            return snapshot;
        }
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

            AddToPhases(item, order);

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
            if (!_singletonMap.TryGetValue(type, out var registered) || !ReferenceEquals(registered, item))
                return;

            item.Quit();
            _singletonMap.Remove(type);

            if (isManager)
                _managers.Remove(item);
            else
                _singletons.Remove(item);

            RemoveFromPhases(item);
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

    #endregion
}
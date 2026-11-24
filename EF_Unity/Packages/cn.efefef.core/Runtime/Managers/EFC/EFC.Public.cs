/*
 * ================================================
 * Describe:        EFC 公开 API 分部：单例访问/注册注销、框架退出、调试快照、主线程调度。
 *                  主机见 EFC.cs，注册中心见 EFC.Registry.cs，状态容器见 EFC.State.cs。
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-06-17 16:31:29
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-24
 * Version:         2.0
 * ===============================================
 */

using EasyFramework;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 框架引擎公开外观：单例注册中心、三级轮询驱动、应用生命周期通知与主线程调度。
/// <para>Framework facade: singleton registry, triple update driver, lifecycle notifications and main-thread dispatch.</para>
/// </summary>
public sealed partial class EFC
{
    #region Properties

    /// <summary> 框架管理器父节点。<para>Parent transform of framework managers.</para> </summary>
    public static Transform Manager { get; private set; }

    /// <summary> 单例父节点。<para>Parent transform of ordinary singletons.</para> </summary>
    public static Transform Singleton { get; private set; }

    /// <summary> 项目配置资源。<para>Project configuration asset.</para> </summary>
    public static ProjectConfig Projects { get; private set; }

    /// <summary>
    /// 框架是否正在运行且未处于退出流程。QuitGames 完整卸载后为 false，需显式调用
    /// <see cref="Initialize"/> 重启。
    /// <para>Whether the framework is actively driving. False after a full QuitGames teardown; call
    /// <see cref="Initialize"/> explicitly to restart.</para>
    /// </summary>
    public static bool IsRunning
    {
        get
        {
            lock (LockObj)
            {
                return _inRunning && !_quitting;
            }
        }
    }

    /// <summary>
    /// 是否允许创建/注册单例：只有显式 Initialize 后、且未处于退出流程时才允许。
    /// <para>Whether singleton creation is allowed: only after an explicit Initialize and not mid-teardown.</para>
    /// </summary>
    public static bool CanCreateSingleton
    {
        get
        {
            lock (LockObj)
            {
                return _inRunning && !_quitting;
            }
        }
    }

    #endregion

    #region Singleton Registry

    /// <summary>
    /// 获取或创建单例的原子入口：已注册直接返回；否则创建并注册，最终以注册表为准返回。
    /// <para>Atomic get-or-create: return registered instance, or create + register, always returning the registry instance.</para>
    /// </summary>
    /// <typeparam name="T">单例类型<para>Singleton type</para></typeparam>
    /// <returns>已注册或新建的单例实例<para>The registered or newly created singleton</para></returns>
    public static T GetOrCreate<T>() where T : class, ISingleton, new()
    {
        if (!CanCreateSingleton)
        {
            D.Error(LogTag + "框架未运行，拒绝创建 " + typeof(T).Name + "。请先调用 EFC.Initialize()。");
            return default;
        }

        if (TryGetSingleton(out T existing))
            return existing;

        Type type = typeof(T);

        // Mono 类型统一走 Instance（其自身已有单实例缓存），避免 EFC 与 MonoSingleton 双缓存
        if (typeof(MonoBehaviour).IsAssignableFrom(type))
        {
            // IgnoreAutoRegister 的 Mono 类型本就不进注册表，Instance 自身缓存即真源
            if (Attribute.IsDefined(type, typeof(IgnoreAutoRegisterAttribute), true))
            {
                object ignored = Registry.GetMonoSingletonInstance(type);
                return ignored is T ignoredMono ? ignoredMono : default;
            }

            // 先触发 Instance 创建/获取，再以注册表为准返回
            Registry.GetMonoSingletonInstance(type);
            return TryGetSingleton(out T mono) ? mono : default;
        }

        // IgnoreAutoRegister 不进注册表，也不随 QuitGames 清除
        if (Attribute.IsDefined(type, typeof(IgnoreAutoRegisterAttribute), true))
            return IgnoredCache<T>.Value;

        // 同线程重入：此时本 T 尚未写表（依赖解析正在创建它），fail-fast 而非无限递归
        if (!Registry.TryBeginCreation(type))
            throw new InvalidOperationException(
                $"{LogTag} Re-entrant creation of {type.Name}. Check for circular dependencies.");

        try
        {
            T instance = new T();
            Register(instance);

            // 以注册表为准：Init 失败回滚后不得把未注册的实例返回给调用方
            return TryGetSingleton(out T registered) ? registered : default;
        }
        finally
        {
            Registry.EndCreation(type);
        }
    }

    /// <summary>
    /// 注册单例。先完成依赖解析与线程校验，再在锁内写表，最后在锁外调用
    /// <see cref="ISingleton.Init"/>；Init 失败会完整回滚。
    /// <para>Registers a singleton: dependencies and thread check first, registry write under lock,
    /// then <see cref="ISingleton.Init"/> outside the lock with full rollback on failure.</para>
    /// </summary>
    /// <param name="item">待注册的单例实例<para>The singleton instance to register</para></param>
    public static void Register(ISingleton item)
    {
        if (!Registry.Prepare(item, out bool registered)) return;

        bool isManager = item != null && Attribute.IsDefined(item.GetType(), typeof(ManagerAttribute), false);
        Registry.Finish(item, registered, isManager);
    }

    /// <summary>
    /// 注销单例。先从注册表移除以保证幂等，再在锁外调用
    /// <see cref="ISingleton.Quit"/>，最后销毁 GameObject。
    /// <para>Unregisters a singleton: removes it from the registry first for idempotency,
    /// then calls <see cref="ISingleton.Quit"/> outside the lock and destroys the GameObject.</para>
    /// </summary>
    /// <param name="item">待注销的单例实例<para>The singleton instance to unregister</para></param>
    /// <param name="destroyGameObject">是否同时销毁其 GameObject<para>Whether to destroy its GameObject as well</para></param>
    public static void Unregister(ISingleton item, bool destroyGameObject = true) =>
        Registry.Unregister(item, destroyGameObject);

    /// <summary>
    /// 尝试获取已注册的单例；支持精确类型与接口/基类兼容查询，自动过滤已销毁的 Unity 对象。
    /// <para>Tries to get a registered singleton; supports exact and assignable type lookup, filtering destroyed Unity objects.</para>
    /// </summary>
    /// <typeparam name="T">目标单例类型<para>The target singleton type</para></typeparam>
    /// <param name="singleton">获取到的单例实例<para>The retrieved singleton instance</para></param>
    /// <returns>是否已注册且可用<para>Whether a live singleton was found</para></returns>
    public static bool TryGetSingleton<T>(out T singleton) where T : ISingleton => Registry.TryGet(out singleton);

    /// <summary>
    /// 取当前已注册的全部单例快照（供编辑器调试工具使用）。
    /// <para>Snapshot of every registered singleton, for editor tooling and debug panels.</para>
    /// </summary>
    /// <returns>单例快照；框架未初始化时返回空集合<para>Singleton snapshot; empty when the framework is not initialized.</para></returns>
    public static IReadOnlyList<ISingleton> GetAllSingletons() => Registry.SnapshotAll();

    #endregion

    #region Shutdown

    /// <summary>
    /// 完整卸载框架：锁内切换状态，锁外按销毁顺序通知
    /// <see cref="IOnAppQuit"/> 订阅者、注销并销毁全部单例，清空容器，
    /// 最后销毁 EFC 宿主自身与两个根节点。再次使用必须显式调用 <see cref="Initialize"/>。
    /// <para>Fully unloads the framework: notifies <see cref="IOnAppQuit"/> subscribers, unregisters and
    /// destroys every singleton, clears containers, then destroys the EFC host and both root objects.
    /// Call <see cref="Initialize"/> explicitly to use the framework again.</para>
    /// </summary>
    public static void QuitGames()
    {
        if (!_inRunning) return;

        // Unity 对象销毁只能在主线程；先拒绝再切状态，避免留下半退出状态
        if (!IsMainThread())
        {
            D.Error(LogTag + "QuitGames 必须在主线程调用。");
            return;
        }

        lock (LockObj)
        {
            // 双重检查：与 Register 的恢复驱动在同一把锁内互斥
            if (!_inRunning || _quitting) return;
            _inRunning = false;
            _quitting = true;
        }

        try
        {
            UnsubscribeLowMemory();

            // 与销毁同序（优先级升序）通知，低优先级先收到保存机会；快照后锁外派发
            IOnAppQuit[] quitSnapshot;
            lock (LockObj)
            {
                quitSnapshot = EfcState.QuitListeners.ToArray();
            }

            for (int i = quitSnapshot.Length - 1; i >= 0; i--)
            {
                try
                {
                    quitSnapshot[i].OnAppQuit();
                }
                catch (Exception e)
                {
                    // 单个订阅者异常不得中断其余退出流程
                    LogException(e);
                }
            }

            List<ISingleton> all = Registry.SnapshotAll();
            all.Sort(SingletonOrderComparison);
            for (int i = 0; i < all.Count; i++)
            {
                try
                {
                    Registry.Unregister(all[i], destroyGameObject: true);
                }
                catch (Exception e)
                {
                    LogException(e);
                }
            }

            // 先摘除宿主引用再销毁：Destroy 延迟到帧末触发 OnDestroy，
            // 届时 _monoEfc / Managers / Singleton 均已置空，OnDestroy => QuitGames 会被 !_inRunning 短路
            // 宿主组件就挂在 Managers 根节点上，销毁该根节点即连带销毁宿主
            Transform managersRoot = Manager;
            Transform singletonRoot = Singleton;
            _monoEfc = null;
            Manager = null;
            Singleton = null;

            // 退出过程中的 Register 已被拒绝；清空残余容器、递增代数使泛型缓存失效
            lock (LockObj)
            {
                EfcState.Reset();
                _stateInitialized = false;
                Generation++;
            }

            // 完整卸载两个根节点（宿主挂在 Managers 上，随之销毁）；Destroy 延迟帧末，
            // 期间本帧回调不会再创建单例（_inRunning 为 false 且 _monoEfc 已置空）
            if (singletonRoot != null) Destroy(singletonRoot.gameObject);
            if (managersRoot != null) Destroy(managersRoot.gameObject);
        }
        finally
        {
            lock (LockObj)
            {
                // 回调抛异常也必须复位，保证后续 Initialize 不会被 _quitting 阻断
                _quitting = false;
            }
        }
    }

    #endregion

    #region Main Thread Dispatch

    /// <summary>
    /// 将动作投递到主线程，在下一帧 Update 中按入队顺序执行。
    /// <para>Posts an action to the main thread, executed in FIFO order on the next Update.</para>
    /// </summary>
    /// <param name="action">待执行的动作<para>The action to execute</para></param>
    public static void RunOnMainThread(Action action)
    {
        if (action == null) return;
        lock (LockObj)
        {
            // 框架未运行（含 QuitGames 完整卸载后）时拒绝入队：没有宿主消费队列，
            // 入队只会造成任务静默丢失或让误以为已执行。调用方应先显式 Initialize。
            if (_quitting || !_inRunning)
            {
                D.Warning(LogTag + "框架未运行，RunOnMainThread 的任务已丢弃；请先调用 EFC.Initialize。");
                return;
            }

            EfcState.MainThreadJobs.Enqueue(action);
        }
    }

    // 由 EFC.cs 的 Update 在帧首调用；逐项隔离异常，避免一个任务饿死队列
    private static void DrainMainThreadJobs()
    {
        while (true)
        {
            Action job;
            lock (LockObj)
            {
                if (EfcState.MainThreadJobs.Count == 0) return;
                job = EfcState.MainThreadJobs.Dequeue();
            }

            try
            {
                job();
            }
            catch (Exception e)
            {
                LogException(e);
            }
        }
    }

    #endregion
}
/*
 * ================================================
 * Describe:      EFC 主机分部：Unity 生命周期、三级帧驱动、应用通知派发与主线程边界。
 *                注册中心见 EFC.Registry.cs，状态容器见 EFC.State.cs，
 *                公开 API 见 EFC.Public.cs。
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-06-17 16:31:29
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-24
 * Version:       2.0
 * ===============================================
 */

using EasyFramework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class EFC : MonoBehaviour
{
    #region Constants

    // 统一日志前缀：避免三套标签导致日志检索失效
    internal const string LogTag = "[ EFC ] ";

    // MonoSingleton 静态实例属性名（反射约定）
    private const string InstancePropertyName = "Instance";

    // 项目配置的 Resources 路径
    private const string ProjectConfigResourcePath = "Configs/ProjectConfig";

    // 框架宿主节点名
    private const string ManagersRootName = "GM.Managers";
    private const string SingletonRootName = "GM.Singleton";

    // 单例排序比较器缓存：避免退出时每次 Sort 分配闭包
    private static readonly Comparison<ISingleton> SingletonOrderComparison = CompareSingletonOrder;

    #endregion

    #region Fields

    // 注册、状态切换与轮询共用的锁对象
    private static readonly object LockObj = new object();

    // 框架运行标记，防止退出后仍轮询
    private static volatile bool _inRunning;

    // 正在退出：退出期间的 Register 不得恢复驱动
    private static volatile bool _quitting;

    // 框架宿主实例；使用 Unity 重载 == 判定 fake-null
    private static EFC _monoEfc;

    // 初始化标志：关闭 Domain Reload 时用于幂等重建
    private static bool _stateInitialized;

    // lowMemory 静态桥接是否已订阅，防止重复订阅
    private static bool _lowMemorySubscribed;

    // 主线程标识：所有 Unity 对象操作都必须在主线程
    private static int _mainThreadId;

    // 会话代数：关闭 Domain Reload 时用于让每类型泛型静态缓存失效
    internal static int Generation;

    // 只有当前宿主才驱动框架：同帧 QuitGames + Initialize 时旧宿主尚未销毁，
    // 若不校验身份会对新框架的表重复派发 Update/FixedUpdate/LateUpdate
    private bool IsCurrentHost => ReferenceEquals(this, _monoEfc);

    #endregion

    #region Unity Lifecycle

    // 防止外部实例化
    private EFC()
    {
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void BeforeSplashScreen()
    {
        // 构建版是否跳过 Splash 由项目配置决定，不再无条件调用
        if (Projects != null && Projects.SkipSplashScreen)
            SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
    }

    /// <summary>
    /// 初始化/重启框架：创建宿主、加载配置、启动帧驱动。
    /// 启动时由 Unity 自动调用；QuitGames 完整卸载后，宿主再次启用 EF 时需显式调用本方法。
    /// 已在运行且宿主存活时幂等返回。
    /// <para>Initializes or restarts the framework. Called by Unity at startup; after a full QuitGames
    /// teardown the host must call this explicitly. Idempotent while the host is alive.</para>
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    public static void Initialize()
    {
        // 宿主存活且已在运行：幂等返回。用 Unity 重载 == 判定 fake-null（关闭域重载时上一轮残留）
        if (_inRunning && _monoEfc != null) return;

        if (_quitting)
        {
            D.Error(LogTag + "框架正在退出，忽略本次 Initialize 调用。");
            return;
        }

        _mainThreadId = Environment.CurrentManagedThreadId;

        // 域重载关闭或上一轮异常退出会残留状态：先清空再重建
        if (_stateInitialized)
            ResetState();
        else
            _stateInitialized = true;

        CreateHost();

        Projects = Resources.Load<ProjectConfig>(ProjectConfigResourcePath);
        if (Projects == null)
            D.Error(LogTag + "找不到 Resources/Configs/ProjectConfig，语言默认值与内置资源路径不可用。请确认资产位于某个 Resources 目录下。");

        _inRunning = true;
        SubscribeLowMemory();
    }

    private void Update()
    {
        if (!IsCurrentHost) return;

        // 帧首先消费工作线程投递的任务；必须在帧驱动之前，保证注册当帧即可生效
        DrainMainThreadJobs();

        if (!_inRunning) return;
        IUpdate[] snapshot = EfcState.UpdateView.Get();
        foreach (var listener in snapshot)
        {
            if (listener.IsPaused) continue;
            try
            {
                listener.Update(Time.deltaTime, Time.unscaledDeltaTime);
            }
            catch (Exception e)
            {
                // 单个监听器异常不得饿死同帧其余监听器
                LogException(e);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!IsCurrentHost || !_inRunning) return;
        IFixedUpdate[] snapshot = EfcState.FixedUpdateView.Get();
        foreach (var listener in snapshot)
        {
            if (listener.IsPaused) continue;
            try
            {
                listener.FixedUpdate(Time.fixedDeltaTime);
            }
            catch (Exception e)
            {
                LogException(e);
            }
        }
    }

    private void LateUpdate()
    {
        if (!IsCurrentHost || !_inRunning) return;
        ILateUpdate[] snapshot = EfcState.LateUpdateView.Get();
        foreach (var listener in snapshot)
        {
            if (listener.IsPaused) continue;
            try
            {
                listener.LateUpdate(Time.deltaTime, Time.unscaledDeltaTime);
            }
            catch (Exception e)
            {
                LogException(e);
            }
        }
    }

    // Destroy 延迟到帧末：若同帧已经 Initialize 出新宿主，旧宿主的 OnDestroy 不能再退出新框架。
    // QuitGames 会先把 _monoEfc 置空，因此只有"当前宿主"才有资格触发退出。
    private void OnDestroy()
    {
        if (IsCurrentHost)
            QuitGames();
    }

    private void OnApplicationQuit()
    {
        if (IsCurrentHost)
            QuitGames();
    }

    private void OnApplicationPause(bool paused)
    {
        if (IsCurrentHost) DispatchPause(paused);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (IsCurrentHost) DispatchFocus(focused);
    }

    #endregion

    #region Low Memory Bridge

    // 静态桥接：不捕获宿主实例，避免退出后静态事件持有 fake-null 宿主
    private static void OnLowMemoryStatic() => DispatchLowMemory();

    private static void SubscribeLowMemory()
    {
        if (_lowMemorySubscribed) return;
        Application.lowMemory += OnLowMemoryStatic;
        _lowMemorySubscribed = true;
    }

    private static void UnsubscribeLowMemory()
    {
        if (!_lowMemorySubscribed) return;
        Application.lowMemory -= OnLowMemoryStatic;
        _lowMemorySubscribed = false;
    }

    #endregion

    #region Dispatch

    // 锁内取快照、锁外派发：回调内重入不会让本次遍历错位；逐项隔离异常
    private static void DispatchPause(bool paused)
    {
        if (!_inRunning) return;
        IOnAppPause[] snapshot;
        lock (LockObj)
        {
            snapshot = EfcState.PauseListeners.ToArray();
        }

        for (int i = 0; i < snapshot.Length; i++)
        {
            try
            {
                snapshot[i].OnAppPause(paused);
            }
            catch (Exception e)
            {
                LogException(e);
            }
        }
    }

    private static void DispatchFocus(bool focused)
    {
        if (!_inRunning) return;
        IOnAppFocus[] snapshot;
        lock (LockObj)
        {
            snapshot = EfcState.FocusListeners.ToArray();
        }

        for (int i = 0; i < snapshot.Length; i++)
        {
            try
            {
                snapshot[i].OnAppFocus(focused);
            }
            catch (Exception e)
            {
                LogException(e);
            }
        }
    }

    private static void DispatchLowMemory()
    {
        if (!_inRunning) return;
        IOnLowMemory[] snapshot;
        lock (LockObj)
        {
            snapshot = EfcState.LowMemoryListeners.ToArray();
        }

        for (int i = 0; i < snapshot.Length; i++)
        {
            try
            {
                snapshot[i].OnLowMemory();
            }
            catch (Exception e)
            {
                LogException(e);
            }
        }
    }

    #endregion

    #region Host & State

    // 确保宿主存活：OnDestroy 之后再次 Register 时用于恢复真实帧泵
    private static void CreateHost()
    {
        if (_monoEfc != null) return;

        if (Manager == null)
        {
            GameObject managersRoot = new GameObject(ManagersRootName);
            DontDestroyOnLoad(managersRoot);
            Manager = managersRoot.transform;
        }

        if (Singleton == null)
        {
            GameObject singletonRoot = new GameObject(SingletonRootName);
            DontDestroyOnLoad(singletonRoot);
            Singleton = singletonRoot.transform;
        }

        _monoEfc = Manager.gameObject.AddComponent<EFC>();
    }

    // 只清状态、不调用用户回调：用于域重载关闭时的幂等重建
    private static void ResetState()
    {
        EfcState.Reset();
        _inRunning = false;
        _quitting = false;
        // 泛型静态缓存无法在此直接清空，递增代数让下一会话首次访问时重建
        Generation++;
    }

    // 恢复驱动：先确保宿主真实存在，再置运行标志，避免"标志为真但帧泵不存在"
    private static void ResumeDriver()
    {
        // CreateHost 内部访问 UnityEngine 对象，调用方已通过主线程校验
        CreateHost();

        lock (LockObj)
        {
            if (_inRunning || _quitting) return;
            _inRunning = true;
        }

        SubscribeLowMemory();
    }

    private static bool IsMainThread() => _mainThreadId == Environment.CurrentManagedThreadId;

    // D.Exception 可被配置为抛异常；此处兜底，确保异常隔离不会反过来打断 teardown
    private static void LogException(Exception exception)
    {
        try
        {
            D.Exception(exception);
        }
        catch
        {
            // 忽略：日志失败不能改变控制流
        }
    }

    #endregion

    #region Internal Helpers

    private static int CompareSingletonOrder(ISingleton a, ISingleton b) =>
        GetOrder(a.GetType()).CompareTo(GetOrder(b.GetType()));

    // GetOrder 在单次锁内完成查找、反射与写回
    private static int GetOrder(Type type)
    {
        lock (LockObj)
        {
            if (EfcState.OrderCache.TryGetValue(type, out int cached)) return cached;
            int order = type.GetCustomAttribute<SingletonPriorityAttribute>(true)?.Order ?? 0;
            EfcState.OrderCache[type] = order;
            return order;
        }
    }

    // 按 order 降序插入（大的在前）；调用方必须持有 LockObj
    // IUpdate 等阶段接口并不继承 ISingleton，实际对象由注册流程保证实现了 ISingleton
    private static void InsertByOrder<T>(List<T> list, T item, int order)
    {
        int index = 0;
        for (; index < list.Count; index++)
        {
            if (list[index] is ISingleton existing && order > GetOrder(existing.GetType())) break;
        }

        list.Insert(index, item);
    }

    private static void AddToPhases(ISingleton item, int order)
    {
        if (item is IUpdate update)
        {
            InsertByOrder(EfcState.UpdateListeners, update, order);
            EfcState.UpdateView.Invalidate();
        }

        if (item is IFixedUpdate fixedUpdater)
        {
            InsertByOrder(EfcState.FixedUpdateListeners, fixedUpdater, order);
            EfcState.FixedUpdateView.Invalidate();
        }

        if (item is ILateUpdate lateUpdater)
        {
            InsertByOrder(EfcState.LateUpdateListeners, lateUpdater, order);
            EfcState.LateUpdateView.Invalidate();
        }

        if (item is IOnAppPause pauseListener) InsertByOrder(EfcState.PauseListeners, pauseListener, order);
        if (item is IOnAppFocus focusListener) InsertByOrder(EfcState.FocusListeners, focusListener, order);
        if (item is IOnAppQuit quitListener) InsertByOrder(EfcState.QuitListeners, quitListener, order);
        if (item is IOnLowMemory lowMemoryListener)
            InsertByOrder(EfcState.LowMemoryListeners, lowMemoryListener, order);
    }

    private static void RemoveFromPhases(ISingleton item)
    {
        if (item is IUpdate update)
        {
            EfcState.UpdateListeners.Remove(update);
            EfcState.UpdateView.Invalidate();
        }

        if (item is IFixedUpdate fixedUpdater)
        {
            EfcState.FixedUpdateListeners.Remove(fixedUpdater);
            EfcState.FixedUpdateView.Invalidate();
        }

        if (item is ILateUpdate lateUpdater)
        {
            EfcState.LateUpdateListeners.Remove(lateUpdater);
            EfcState.LateUpdateView.Invalidate();
        }

        if (item is IOnAppPause pauseListener) EfcState.PauseListeners.Remove(pauseListener);
        if (item is IOnAppFocus focusListener) EfcState.FocusListeners.Remove(focusListener);
        if (item is IOnAppQuit quitListener) EfcState.QuitListeners.Remove(quitListener);
        if (item is IOnLowMemory lowMemoryListener) EfcState.LowMemoryListeners.Remove(lowMemoryListener);
    }

    #endregion
}
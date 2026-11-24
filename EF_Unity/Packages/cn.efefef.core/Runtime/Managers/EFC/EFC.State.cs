/*
 * ================================================
 * Describe:      EFC 状态容器分部：单例表、阶段监听器、快照缓存与会话代数缓存。
 *                注册中心见 EFC.Registry.cs，主机见 EFC.cs。
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
using System.Threading;

public sealed partial class EFC
{
    // 所有静态状态集中于此，QuitGames 与域重载重建可以一次性清理
    private static class EfcState
    {
        /// <summary>核心管理器列表。<para>Core managers.</para></summary>
        internal static readonly List<ISingleton> Managers = new List<ISingleton>();

        /// <summary>普通单例列表。<para>Ordinary singletons.</para></summary>
        internal static readonly List<ISingleton> Singletons = new List<ISingleton>();

        /// <summary>逻辑帧监听器。<para>Update listeners.</para></summary>
        internal static readonly List<IUpdate> UpdateListeners = new List<IUpdate>();

        /// <summary>物理帧监听器。<para>Fixed update listeners.</para></summary>
        internal static readonly List<IFixedUpdate> FixedUpdateListeners = new List<IFixedUpdate>();

        /// <summary>渲染帧监听器。<para>Late update listeners.</para></summary>
        internal static readonly List<ILateUpdate> LateUpdateListeners = new List<ILateUpdate>();

        /// <summary>应用暂停监听器。<para>Application pause listeners.</para></summary>
        internal static readonly List<IOnAppPause> PauseListeners = new List<IOnAppPause>();

        /// <summary>应用焦点监听器。<para>Application focus listeners.</para></summary>
        internal static readonly List<IOnAppFocus> FocusListeners = new List<IOnAppFocus>();

        /// <summary>应用退出监听器。<para>Application quit listeners.</para></summary>
        internal static readonly List<IOnAppQuit> QuitListeners = new List<IOnAppQuit>();

        /// <summary>低内存监听器。<para>Low memory listeners.</para></summary>
        internal static readonly List<IOnLowMemory> LowMemoryListeners = new List<IOnLowMemory>();

        /// <summary>依赖解析链。<para>Dependency resolution chain.</para></summary>
        internal static readonly HashSet<Type> ResolvingSet = new HashSet<Type>();

        /// <summary>创建中的类型集合。<para>Types currently being created.</para></summary>
        internal static readonly HashSet<Type> CreatingTypes = new HashSet<Type>();

        /// <summary>单例优先级缓存。<para>Singleton priority cache.</para></summary>
        internal static readonly Dictionary<Type, int> OrderCache = new Dictionary<Type, int>();

        /// <summary>已注册单例映射。<para>Registered singleton map.</para></summary>
        internal static readonly Dictionary<Type, ISingleton> SingletonMap = new Dictionary<Type, ISingleton>();

        /// <summary>Mono 单例 Instance 取值委托缓存。<para>Cached Instance getters of Mono singletons.</para></summary>
        internal static readonly Dictionary<Type, Func<object>> MonoSingletonGetters =
            new Dictionary<Type, Func<object>>();

        /// <summary>工作线程投递的任务队列。<para>Queue of jobs posted from worker threads.</para></summary>
        internal static readonly Queue<Action> MainThreadJobs = new Queue<Action>();

        /// <summary>逻辑帧快照。<para>Update snapshot.</para></summary>
        internal static readonly SnapshotCache<IUpdate> UpdateView =
            new SnapshotCache<IUpdate>(UpdateListeners, LockObj);

        /// <summary>物理帧快照。<para>Fixed update snapshot.</para></summary>
        internal static readonly SnapshotCache<IFixedUpdate> FixedUpdateView =
            new SnapshotCache<IFixedUpdate>(FixedUpdateListeners, LockObj);

        /// <summary>渲染帧快照。<para>Late update snapshot.</para></summary>
        internal static readonly SnapshotCache<ILateUpdate> LateUpdateView =
            new SnapshotCache<ILateUpdate>(LateUpdateListeners, LockObj);

        /// <summary>
        /// 清空全部容器并使快照失效。
        /// <para>Clears every container and invalidates snapshots.</para>
        /// </summary>
        internal static void Reset()
        {
            Managers.Clear();
            Singletons.Clear();
            UpdateListeners.Clear();
            FixedUpdateListeners.Clear();
            LateUpdateListeners.Clear();
            PauseListeners.Clear();
            FocusListeners.Clear();
            QuitListeners.Clear();
            LowMemoryListeners.Clear();
            ResolvingSet.Clear();
            CreatingTypes.Clear();
            OrderCache.Clear();
            SingletonMap.Clear();
            MonoSingletonGetters.Clear();
            MainThreadJobs.Clear();
            UpdateView.Invalidate();
            FixedUpdateView.Invalidate();
            LateUpdateView.Invalidate();
        }
    }

    // 每类型独立、延迟创建的实例缓存；仅为 IgnoreAutoRegister 的普通类服务
    private static class IgnoredCache<T> where T : class, ISingleton, new()
    {
        private static int _generation = -1;
        private static Lazy<T> _lazy;

        /// <summary>
        /// 取得当前会话的唯一实例；会话代数变化时重建，避免关闭域重载后复用上一会话残留对象。
        /// <para>Gets the current session instance, rebuilding when the session generation changes.</para>
        /// </summary>
        internal static T Value
        {
            get
            {
                if (_generation != Generation)
                {
                    _lazy = new Lazy<T>(() => new T());
                    _generation = Generation;
                }

                return _lazy.Value;
            }
        }
    }

    // List 未变时复用数组消除每帧 GC；Invalidate 与 List 改动同在 LockObj 内调用
    private sealed class SnapshotCache<T>
    {
        // 被缓存的源列表
        private readonly List<T> _list;

        // 与写方共享的锁
        private readonly object _gate;

        // 最近一次快照数组
        private T[] _cache = Array.Empty<T>();

        // 当前数据版本
        private int _version;

        // 缓存对应的数据版本
        private int _cacheVersion = -1;

        /// <summary>
        /// 创建快照缓存。
        /// <para>Creates a snapshot cache.</para>
        /// </summary>
        /// <param name="list">被缓存的列表。<para>The list to cache.</para></param>
        /// <param name="gate">写操作使用的锁。<para>The gate used by writers.</para></param>
        internal SnapshotCache(List<T> list, object gate)
        {
            _list = list;
            _gate = gate;
        }

        /// <summary>
        /// 标记列表已变更，使旧快照失效。
        /// <para>Marks the list as changed so the cached snapshot is invalidated.</para>
        /// </summary>
        internal void Invalidate() => Interlocked.Increment(ref _version);

        /// <summary>
        /// 获取当前快照；稳态无锁返回，版本变化时才在锁内重建。
        /// <para>Returns the current snapshot lock-free in steady state, rebuilding only on version change.</para>
        /// </summary>
        /// <returns>当前快照数组。<para>The current snapshot array.</para></returns>
        internal T[] Get()
        {
            if (_cacheVersion == _version) return _cache;
            lock (_gate)
            {
                if (_cacheVersion != _version)
                {
                    _cache = _list.ToArray();
                    Volatile.Write(ref _cacheVersion, _version);
                }

                return _cache;
            }
        }
    }
}
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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class EF : MonoBehaviour
{
    private static EF _monoEF;
    
    private static bool _inRunning;
    private static List<ISingleton> _singletons;
    private static List<IManager> _managerList;
    private static List<IUpdate> _manageUpdater;
    private static List<IUpdate> _updater;
    private static List<IFixedUpdate> _fixedUpdaters;
    private static List<ILateUpdate> _lateUpdaters;

    private static readonly object _lockObj = new object();

    private EF() { }

    #region 生命周期

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
        _managerList = new List<IManager>();
        _singletons = new List<ISingleton>();
        _manageUpdater = new List<IUpdate>();
        _lateUpdaters = new List<ILateUpdate>();
        _fixedUpdaters = new List<IFixedUpdate>();
        Projects = Resources.Load<ProjectConfig>("Configs/ProjectConfig");

        _inRunning = true;
    }

    private void Update()
    {
        if (!_inRunning) return;
        lock (_lockObj)
        {
            for (int i = 0; i < _manageUpdater.Count; i++)
                _manageUpdater[i].Update(Time.deltaTime, Time.unscaledDeltaTime);
            for (int i = 0; i < _updater.Count; i++)
                _updater[i].Update(Time.deltaTime, Time.unscaledDeltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (!_inRunning) return;
        lock (_lockObj)
        {
            for (int i = 0; i < _fixedUpdaters.Count; i++)
                _fixedUpdaters[i].FixedUpdate(Time.fixedDeltaTime);
        }
    }

    private void LateUpdate()
    {
        if (!_inRunning) return;
        lock (_lockObj)
        {
            for (int i = 0; i < _lateUpdaters.Count; i++)
                _lateUpdaters[i].LateUpdate(Time.deltaTime, Time.unscaledDeltaTime);
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
            for (int i = _singletons.Count - 1; i >= 0; i--)
                Unregister(_singletons[i]);
            _singletons.Clear();
            _updater.Clear();
            _fixedUpdaters.Clear();
            _lateUpdaters.Clear();

            for (int i = _managerList.Count - 1; i >= 0; i--)
                UnregisterManager(_managerList[i]);
            _managerList.Clear();
            _manageUpdater.Clear();
        }

        _updater = null;
        _singletons = null;
        _managerList = null;
        _manageUpdater = null;
        _fixedUpdaters = null;
        _lateUpdaters = null;
    }

    #endregion

    private static void InsertUpdaterByLevel<T>(List<T> list, T updater, int level)
    {
        int index = 0;
        for (; index < list.Count; index++)
        {
            int existLevel = Projects.AppConst.ManagerLevels.IndexOf(list[index].GetType().Name);
            if (level < existLevel)
                break;
        }
        list.Insert(index, updater);
    }

    #region 公开函数

    public static void Register(ISingleton item)
    {
        lock (_lockObj)
        {
            _singletons.Add(item);
            if (item is IUpdate update) _updater.Add(update);
            if (item is IFixedUpdate fixedUpdater) _fixedUpdaters.Add(fixedUpdater);
            if (item is ILateUpdate lateUpdater) _lateUpdaters.Add(lateUpdater);
        }
    }

    public static void Register(IManager item)
    {
        int level = Projects.AppConst.ManagerLevels.IndexOf(item.GetType().Name);

        lock (_lockObj)
        {
            if (level == -1 || _managerList.Count == 0)
            {
                _managerList.Add(item);
            }
            else
            {
                for (int i = _managerList.Count - 1; i >= 0; i--)
                {
                    if (level > Projects.AppConst.ManagerLevels.IndexOf(_managerList[i].GetType().Name))
                    {
                        _managerList.Insert(i + 1, item);
                        break;
                    }
                    if (i != 0) continue;
                    _managerList.Insert(0, item);
                }
            }

            if (item is IUpdate update)
                InsertUpdaterByLevel(_manageUpdater, update, level);
            if (item is IFixedUpdate fixedUpdater)
                InsertUpdaterByLevel(_fixedUpdaters, fixedUpdater, level);
            if (item is ILateUpdate lateUpdater)
                InsertUpdaterByLevel(_lateUpdaters, lateUpdater, level);
        }
    }

    public static void Unregister(ISingleton item)
    {
        if (item is IManager)
        {
            D.Error("You should not unregister a manager directly. Use EF.UnregisterManager if needed, or managers are auto-cleaned on quit.");
            return;
        }

        lock (_lockObj)
        {
            item.Quit();
            _singletons.Remove(item);
            if (item is IUpdate update) _updater.Remove(update);
            if (item is IFixedUpdate fixedUpdater) _fixedUpdaters.Remove(fixedUpdater);
            if (item is ILateUpdate lateUpdater) _lateUpdaters.Remove(lateUpdater);
        }

        if (item is MonoBehaviour mono)
            Destroy(mono.gameObject);
    }

    public static void UnregisterManager(IManager manager)
    {
        lock (_lockObj)
        {
            manager.Quit();
            if (manager is IUpdate update) _manageUpdater.Remove(update);
            if (manager is IFixedUpdate fixedUpdater) _fixedUpdaters.Remove(fixedUpdater);
            if (manager is ILateUpdate lateUpdater) _lateUpdaters.Remove(lateUpdater);
            _managerList.Remove(manager);
        }

        if (manager is MonoBehaviour mono)
            Destroy(mono.gameObject);
    }

    #endregion
}
/* 
 * ================================================
 * Describe:      This script is used to control the all managers.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-06-17 16:31:29
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-06-17 16:31:29
 * Version:       0.1 
 * ===============================================
 */
using EasyFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The game managers master controller.
/// <para>游戏管理器总控</para>
/// </summary>
public sealed partial class EF : MonoBehaviour
{
    private EF() { }

    #region Skip logo
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void BeforeSplashScreen()
    {
#if !UNITY_WEBGL
        System.Threading.Tasks.Task.Run(AsyncEnter);
#else
        Application.focusChanged += ApplicationFocusChanged;
#endif
    }

#if !UNITY_WEBGL
    private static void AsyncEnter()
    {
        SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
    }
#else
    private static void ApplicationFocusChanged(bool focus)
    {
        Application.focusChanged -= ApplicationFocusChanged;
        SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
    }
#endif
    #endregion

    #region Initialize application
    static EF _monoEF;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        Managers = new GameObject("GM.Managers").transform;
        Singleton = new GameObject("GM.Singleton").transform;
        _monoEF = Managers.gameObject.AddComponent<EF>();
        DontDestroyOnLoad(Managers);
        DontDestroyOnLoad(Singleton);

        _managerList = new List<IManager>();
        _manageUpdater = new List<IUpdate>();
        _updater = new List<IUpdate>();
        _singletons = new List<ISingleton>();
        Projects = Resources.Load<EasyFramework.Edit.Setting.ProjectSetting>("Settings/ProjectSetting");
        
        _exiting = false;
    }
    #endregion

    #region Update
    private void Update()
    {
        if (_exiting)
            return;
        for (int i = 0; i < _mgrUprCount; i++)
        {
            _manageUpdater[i].Update(Time.deltaTime, Time.unscaledDeltaTime);
        }
        for (int i = 0; i < _uprCount; i++)
        {
            _updater[i].Update(Time.deltaTime, Time.unscaledDeltaTime);
        }
    }
    #endregion

    #region Control Mamager and update
    static bool _exiting;
    static int _singletonsCount;
    static int _uprCount;
    static int _mgrUprCount;
    static int _managerCount;
    static List<IManager> _managerList;
    static List<IUpdate> _manageUpdater;
    static List<IUpdate> _updater;
    static List<ISingleton> _singletons;

    /// <summary>
    /// Register a singleton
    /// <para>注册一个单例</para>
    /// </summary>
    public static void Register(ISingleton item)
    {
        if (item is IUpdate)
        {
            _uprCount++;
            _updater.Add(item as IUpdate);
        }
        _singletonsCount++;
        _singletons.Add(item);
    }

    /// <summary>
    /// Register a manager
    /// <para>注册一个管理器</para>
    /// </summary>
    public static void Register(IManager item)
    {
        _managerCount++;

        int level = Projects.AppConst.ManagerLevels.IndexOf(item.GetType().Name);

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
                if (0 == i)
                {
                    _managerList.Insert(0, item);
                    break;
                }
            }
        }

        if (item is IUpdate)
        {
            if (_manageUpdater.Count == 0)
                _manageUpdater.Add(item as IUpdate);
            else
            {
                for (int i = _manageUpdater.Count - 1; i >= 0; i--)
                {
                    
                    if (level > Projects.AppConst.ManagerLevels.IndexOf(_manageUpdater[i].GetType().Name))
                    {
                        _manageUpdater.Insert(i + 1, item as IUpdate);
                        break;
                    }
                    if (0 == i)
                    {
                        _manageUpdater.Insert(0, item as IUpdate);
                        break;
                    }
                }
            }
            _mgrUprCount++;
        }
    }

    /// <summary>
    /// Unregister a manager
    /// <para>注销一个单例</para>
    /// </summary>
    public static void Unregister(ISingleton item)
    {
        if (item is IManager)
        {
            D.Error("You should not unregister this singleton, bescause is a manager.");
            return;
        }
        --_singletonsCount;
        _singletons[_singletons.IndexOf(item)].Quit();
        _singletons.Remove(item);
        if (item is IUpdate)
        {
            _uprCount--;
            _updater.Remove(item as IUpdate);
        }
        if (item is MonoBehaviour _mono)
        {
            Destroy(_mono.gameObject);
            Destroy(_mono);
        }
    }

    static void QuitGames()
    {
        if (_exiting)
            return;
        _exiting = true;
                
        while (--_singletonsCount >= 0)
            _singletons[_singletonsCount].Quit();

        _updater.Clear();
        _updater = null;
        _singletons.Clear();
        _singletons = null;

        while (--_managerCount >= 0)
            _managerList[_managerCount].Quit();

        _managerList.Clear();
        _managerList = null;
        _manageUpdater.Clear();
        _manageUpdater = null;
    }
    #endregion

    #region Destory
    private void OnDestroy()
    {
        QuitGames();
    }
    #endregion
}

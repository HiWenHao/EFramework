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
/// The game managers master controller.游戏管理器总控制器.
/// </summary>
public partial class EF : MonoBehaviour
{
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
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        Managers = new GameObject("GM.Managers").transform;
        Singleton = new GameObject("GM.Singleton").transform;
        Managers.gameObject.AddComponent<EF>();
        DontDestroyOnLoad(Managers);
        DontDestroyOnLoad(Singleton);

        ManagerList = new List<IManager>();
        ManageUpdater = new List<IUpdate>();
        Updater = new List<IUpdate>();
        Singletons = new List<ISingleton>();

        InitInAfterSceneLoad();
    }
    #endregion

    #region Update
    private void Update()
    {
        for (int i = 0; i < MgrUprCount; i++)
        {
            ManageUpdater[i].Update(Time.deltaTime, Time.unscaledDeltaTime);
        }
        for (int i = 0; i < UprCount; i++)
        {
            Updater[i].Update(Time.deltaTime, Time.unscaledDeltaTime);
        }
    }
    #endregion

    #region Control Mamager and update
    static int SingletonsCount;
    static int UprCount;
    static int MgrUprCount;
    static int ManagerCount;
    static List<IManager> ManagerList;
    static List<IUpdate> ManageUpdater;
    static List<IUpdate> Updater;
    static List<ISingleton> Singletons;
    public static void Register(ISingleton item)
    {
        if (item is IUpdate)
        {
            UprCount++;
            Updater.Add(item as IUpdate);
        }
        SingletonsCount++;
        Singletons.Add(item);        
    }
    public static void Register(IManager item)
    {
        ManagerCount++;
        int mgr = item.ManagerLevel;
        if (ManagerList.Count == 0)
            ManagerList.Add(item);
        else
        {
            for (int i = ManagerList.Count - 1; i >= 0; i--)
            {
                if (ManagerList[i].ManagerLevel < mgr)
                {
                    ManagerList.Insert(i + 1, item);
                    break;
                }
                if (0 == i)
                {
                    ManagerList.Insert(0, item);
                    break;
                }
            }
        }

        if (item is IUpdate)
        {
            if (ManageUpdater.Count == 0)
                ManageUpdater.Add(item as IUpdate);
            else
            {
                for (int i = ManageUpdater.Count - 1; i >= 0; i--)
                {
                    if ((ManageUpdater[i] as IManager).ManagerLevel < mgr)
                    {
                        ManageUpdater.Insert(i + 1, item as IUpdate);
                        break;
                    }
                    if (0 == i)
                    {
                        ManageUpdater.Insert(0, item as IUpdate);
                        break;
                    }
                }
            }
            MgrUprCount++;
        }
    }
    public static void Unregister(ISingleton item)
    {
        if (item is IManager)
        {
            EasyFramework.D.Error("You should not unregister this singleton, bescause is a manager.");
            return;
        }
        --SingletonsCount;
        Singletons.Remove(item);
        if (item is IUpdate)
            Updater.Remove(item as IUpdate);
    }

    static void QuitGames()
    {
        while (--UprCount >= 0)
        {
            Updater.RemoveAt(UprCount);
        }
        while (--SingletonsCount >= 0)
        {
            Singletons[SingletonsCount].Quit();
            Singletons.RemoveAt(SingletonsCount);
        }
        Updater.Clear();
        Updater = null;
        Singletons.Clear();
        Singletons = null;

        while (--MgrUprCount >= 0)
        {
            ManageUpdater.RemoveAt(MgrUprCount);
        }
        while (--ManagerCount >= 0)
        {
            ManagerList[ManagerCount].Quit();
            ManagerList.RemoveAt(ManagerCount);
        }
        ManagerList.Clear();
        ManagerList = null;
        ManageUpdater.Clear();
        ManageUpdater = null;
    }
    #endregion
}

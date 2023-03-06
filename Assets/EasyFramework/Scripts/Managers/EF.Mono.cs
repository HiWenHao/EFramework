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
            //XHTools.D.Correct($"{Updater[i].GetType().Name}       {i}  ");
            Updater[i].Update(Time.deltaTime, Time.unscaledDeltaTime);
        }
    }
    #endregion

    #region Control Mamager and update
    static int SingletonsCount;
    static int UprCount;
    static int MgrUprCount;
    static List<IManager> ManagerList;
    static List<IUpdate> ManageUpdater;
    static List<IUpdate> Updater;
    static List<ISingleton> Singletons;
    public static void Register(ISingleton item)
    {
        if (item is IManager)
        {
            if (0 == ManagerList.Count)
            {
                ManagerList.Add(item as IManager);
                ManageUpdater.Add(item as IUpdate);
                MgrUprCount++;
            }
            else
            {
                int mgr = (item as IManager).ManagerLevel;

                for (int i = ManagerList.Count - 1; i > 0; i--)
                {
                    if (ManagerList[i].ManagerLevel < mgr)
                    {
                        MgrUprCount++;
                        ManagerList.Insert(i + 1, item as IManager);
                        ManageUpdater.Insert(i + 1, item as IUpdate);
                        break;
                    }
                }
            }
        }
        else
        {
            if (item is IUpdate)
            {
                UprCount++;
                Updater.Add(item as IUpdate);
            }
            SingletonsCount++;
            Singletons.Add(item);
        }
    }

    public static void Unregister(ISingleton item)
    {

    }

    static void QuitGames()
    {
        while (--SingletonsCount >= 0)
        {
            Singletons[SingletonsCount].Quit();
            Singletons.RemoveAt(SingletonsCount);
        }

        while (--UprCount >= 0)
        {
            Updater.RemoveAt(UprCount);
        }

        Updater.Clear();
        Updater = null;

        Singletons.Clear();
        Singletons = null;
    }
    #endregion
}

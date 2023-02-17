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
        Managers.gameObject.AddComponent<EF>();
        DontDestroyOnLoad(Managers);
        InitInAfterSceneLoad();
    }
    #endregion

    #region Control Mamager
    static Queue<ISingleton> Singletons;
    public static void Register(ISingleton item)
    {
        if (null == Singletons)
            Singletons = new Queue<ISingleton>();
        Singletons.Enqueue(item);
    }

    static void QuitGames()
    {
        while (Singletons.Count != 0)
        {
            Singletons.Dequeue().Quit();
        }
        Singletons.Clear();
        Singletons = null;
    }
    #endregion
}

/*
 * ================================================
 * Describe:        The class is game managers controller.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-01-14:33:01
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2022-05-01-14:33:01
 * Version:         1.0
 * ===============================================
 */
using EasyFramework.Edit.Setting;
using EasyFramework.Managers;
using System.Collections;
using UnityEngine;

public partial class EF
{
    public static Transform Managers { get; private set; }
    public static Transform Singleton { get; private set; }
    public static ProjectSetting Projects { get; private set; }
    public static UIManager Ui => UIManager.Instance;
    public static LoadManager Load => LoadManager.Instance;
    public static ToolManager Tool => ToolManager.Instance;
    public static TimeManager Timer => TimeManager.Instance;
    public static InputManager Input => InputManager.Instance;
    public static HttpsManager Https => HttpsManager.Instance;
    public static ScenesManager Scenes => ScenesManager.Instance;
    public static SocketManager Socket => SocketManager.Instance;
    public static FolderManager Folder => FolderManager.Instance;
    public static SourceManager Sources => SourceManager.Instance;
    public static GameObjectPoolManager ObjectPool => GameObjectPoolManager.Instance;

    static void InitInAfterSceneLoad()
    {
        EasyFramework.D.Init();
#if UNITY_EDITOR
        ClearConsole();
#endif
        #region Set the game run time info
        //Application.targetFrameRate = 60;
        Application.runInBackground = true;
        #endregion

        #region Show platform infomation.展示平台信息
        EasyFramework.D.Correct($"CPU: {SystemInfo.processorType}({SystemInfo.processorCount}cores核心数)   " +
            $"  RAM = {Mathf.RoundToInt(SystemInfo.systemMemorySize / 1024f)}G     " +
            $"  GPU: {SystemInfo.graphicsDeviceName}   " +
            $"  VRAM = {Mathf.RoundToInt(SystemInfo.graphicsMemorySize / 1024f)}G        " +
            $" {Screen.width} * {Screen.height} @{Screen.currentResolution.refreshRate}Hz");
        #endregion

        EasyFramework.D.Log("======================Initialize======================");
        //在这里写初始化内容，音频播放、首页UI进入、数据初始化、各类管理器初始化都可以在此

        Timer.SleepTimeout = SleepTimeout.NeverSleep;
        //ExampleGame.Controller.CameraControl.Instance.gameObject.AddComponent<GMTest.Test>();

        //FPS展示
        EasyFramework.Utils.FPSOnGUI.Instance.allowDrag = true;

        //读表工具初始化
        //EasyFramework.ExcelTool.ExcelDataManager.Init("JsonData");
        //ETB.ExcelDataCacheManager.CacheAllData();
        //资源热更     仅支持Unity2019.4+      加载资源逻辑需要自己实现、根据项目的不同，逻辑也不同
        EasyFramework.Utils.PatchUpdater.Instance.PatchStart(EasyFramework.Utils.EFPlayMode.HostPlayMode);

        //UI进入
        //Ui.Push(new You Class());

        //音频播放
        //Sources.PlayBGMByName("You bgm`s name", true);
    }

    #region Coroutine  协程
    /// <summary>
    /// 开启一个协程
    /// </summary>
    public static Coroutine StartCoroutines(IEnumerator coroutine)
    {
        return m_monoEF.StartCoroutine(coroutine);
    }
    /// <summary>
    /// 开启一个协程
    /// </summary>
    public static Coroutine StartCoroutines(string coroutine)
    {
        return m_monoEF.StartCoroutine(coroutine);
    }
    /// <summary>
    /// 停止一个协程
    /// </summary>
    public static void StopCoroutines(Coroutine coroutine)
    {
        m_monoEF.StopCoroutine(coroutine);
    }
    /// <summary>
    /// 停止一个协程
    /// </summary>
    public static void StopCoroutines(string methodName)
    {
        m_monoEF.StopCoroutine(methodName);
    }
    /// <summary>
    /// 停止所有协程
    /// </summary>
    public static void StopAllCoroutine()
    {
        m_monoEF.StopAllCoroutines();
    }
    #endregion

    /// <summary>
    /// 清理内存
    /// </summary>
    public static void ClearMemory()
    {
        System.GC.Collect();
        Load.ClearAllMemory();
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public static void QuitGame()
    {
        ClearMemory();
        QuitGames();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit(0);
    }

    #region Clear Console
#if UNITY_EDITOR
    static System.Reflection.MethodInfo m_ClearMethod = null;
    /// <summary>
    /// 清空log信息
    /// </summary>
    public static void ClearConsole()
    {
        if (m_ClearMethod == null)
        {
            System.Type _log = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
            m_ClearMethod = _log.GetMethod("Clear");
        }
        m_ClearMethod.Invoke(null, null);
    }
#endif
    #endregion
}

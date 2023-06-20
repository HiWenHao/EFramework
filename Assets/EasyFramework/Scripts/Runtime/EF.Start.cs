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
    /// <summary> 场景中的EF对象 </summary>
    public static Transform Managers { get; private set; }

    /// <summary> 场景中单例父节点 </summary>
    public static Transform Singleton { get; private set; }

    /// <summary> 项目配置资源 </summary>
    public static ProjectSetting Projects { get; private set; }

    /// <summary> UI管理器 </summary>
    public static UIManager Ui => UIManager.Instance;

    /// <summary> 通用工具管理器 </summary>
    public static ToolManager Tool => ToolManager.Instance;

    /// <summary> 资源管理器 </summary>
    public static LoadManager Load => LoadManager.Instance;

    /// <summary> 时间管理器 </summary>
    public static TimeManager Timer => TimeManager.Instance;

    /// <summary> 补丁更新管理器 </summary>
    public static PatchManager Patch => PatchManager.Instance;

    /// <summary> 音频管理器 </summary>
    public static AudioManager Audio => AudioManager.Instance;

    /// <summary> 网络HTTP管理器 </summary>
    public static HttpsManager Https => HttpsManager.Instance;

    /// <summary> 场景管理器 </summary>
    public static ScenesManager Scenes => ScenesManager.Instance;

    /// <summary> 网络Socket管理器 </summary>
    public static SocketManager Socket => SocketManager.Instance;

    /// <summary> 文件夹管理器 </summary>
    public static FolderManager Folder => FolderManager.Instance;

    /// <summary> 对象池管理器 </summary>
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
        Patch.StartUpdatePatch(EFPlayMode.HostPlayMode);

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
    public static Coroutine StartCoroutines(string coroutine, object value)
    {
        if (null == value)
            return m_monoEF.StartCoroutine(coroutine);
        return m_monoEF.StartCoroutine(coroutine, value);
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
    public static void StopCoroutines(IEnumerator coroutine)
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
    /// 停止所有协程,谨慎使用
    /// Be prudent to use
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

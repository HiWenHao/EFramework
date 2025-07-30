/*
 * ================================================
 * Describe:        The class is game managers controller.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-01-14:33:01
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2024-04-28-15:32:44
 * Version:         1.0
 * ===============================================
 */
using EasyFramework.Edit.Setting;
using EasyFramework.Managers;
using System.Collections;
using UnityEngine;

public sealed partial class EF
{
    /// <summary> The object for EF framework in scene.<para>场景中的EF对象</para></summary>
    public static Transform Managers { get; private set; }

    /// <summary> The singleton parent node in the scenario.<para>场景中单例父节点</para></summary>
    public static Transform Singleton { get; private set; }

    /// <summary> Project allocation resource.<para>项目配置资源</para></summary>
    public static ProjectSetting Projects { get; private set; }

    /// <summary> UI manager.<para>UI管理器</para></summary>
    public static UIManager Ui => UIManager.Instance;

    /// <summary> Universal tools manager.<para>通用工具管理器</para></summary>
    public static ToolManager Tool => ToolManager.Instance;

    /// <summary> Load the resources manager.<para>加载资源管理器</para></summary>
    public static LoadManager Load => LoadManager.Instance;

    /// <summary> Time manager.<para>时间管理器</para></summary>
    public static TimeManager Timer => TimeManager.Instance;

    /// <summary> Event manager.<para>事件管理器</para></summary>
    public static EventManager Event => EventManager.Instance;

    /// <summary> Patch update manager.<para>补丁更新管理器</para></summary>
    public static PatchManager Patch => PatchManager.Instance;

    /// <summary> Audio manager.<para>音频管理器</para></summary>
    public static AudioManager Audio => AudioManager.Instance;

    /// <summary> Network (HTTP) manager.<para>网络HTTP管理器</para></summary>
    public static HttpsManager Https => HttpsManager.Instance;

    /// <summary> Scene manager.<para>场景管理器</para></summary>
    public static ScenesManager Scenes => ScenesManager.Instance;

    /// <summary> Network (Socket) manager.<para>网络Socket管理器</para></summary>
    public static SocketManager Socket => SocketManager.Instance;

    /// <summary> Folder manager.<para>文件夹管理器</para></summary>
    public static FolderManager Folder => FolderManager.Instance;

    #region Coroutine  协程
    /// <summary>
    /// Start a coroutine.
    /// <para>开启一个协程</para>
    /// </summary>
    public static Coroutine StartCoroutines(IEnumerator coroutine)
    {
        return _monoEF.StartCoroutine(coroutine);
    }
    /// <summary>
    /// Start a coroutine.
    /// <para>开启一个协程</para>
    /// </summary>
    public static Coroutine StartCoroutines(string coroutine, object value)
    {
        if (null == value)
            return _monoEF.StartCoroutine(coroutine);
        return _monoEF.StartCoroutine(coroutine, value);
    }
    /// <summary>
    /// Stop a coroutines.
    /// <para>停止一个协程</para>
    /// </summary>
    public static void StopCoroutines(Coroutine coroutine)
    {
        _monoEF.StopCoroutine(coroutine);
    }
    /// <summary>
    /// Stop a coroutines.
    /// <para>停止一个协程</para>
    /// </summary>
    public static void StopCoroutines(IEnumerator coroutine)
    {
        _monoEF.StopCoroutine(coroutine);
    }
    /// <summary>
    /// Stop a coroutines.
    /// <para>停止一个协程</para>
    /// </summary>
    public static void StopCoroutines(string methodName)
    {
        _monoEF.StopCoroutine(methodName);
    }
    /// <summary>
    /// Stop all coroutines and use with caution.
    /// <para>停止所有协程, 谨慎使用</para>
    /// </summary>
    public static void StopAllCoroutine()
    {
        _monoEF.StopAllCoroutines();
    }
    #endregion

    /// <summary>
    /// Clear memory
    /// <para>清理内存</para>
    /// </summary>
    public static void ClearMemory()
    {
        System.GC.Collect();
        Load.ClearAllMemory();
    }

    /// <summary>
    /// Quit Game
    /// <para>退出游戏</para>
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

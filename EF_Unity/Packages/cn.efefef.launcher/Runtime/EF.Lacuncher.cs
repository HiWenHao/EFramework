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

using EasyFramework.Managers;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers.Event;
using EasyFramework.Managers.Procedure;
using EasyFramework.Systems.Assets;
using EasyFramework.Systems.Patch;
using EasyFramework.Managers.Pool;
using UnityEngine;

public sealed class EF
{
    /// <summary> Pool manager.<para>对象池系统</para></summary>
    public static PoolManager Pool => PoolManager.Instance;

    /// <summary> Event manager.<para>事件系统</para></summary>
    public static EventsManager Event => EventsManager.Instance;
    
    /// <summary> Procedure system.<para>流程系统</para></summary>
    public static ProcedureManager Procedure => ProcedureManager.Instance;
    
    /// <summary> Load the resources system.<para>加载资源系统</para></summary>
    public static AssetsSystem Assets => AssetsSystem.Instance;
    
    
    /// <summary> Universal tools manager.<para>通用工具管理器</para></summary>
    public static ToolManager Tool => ToolManager.Instance;

    /// <summary> Time manager.<para>时间管理器</para></summary>
    public static TimeManager Timer => TimeManager.Instance;
    
    /// <summary> Event manager.<para>系统级事件管理器</para></summary>
    public static EventManager Events => EventManager.Instance;

    /// <summary> Patch update manager.<para>补丁更新管理器</para></summary>
    public static PatchSystem Patch => PatchSystem.Instance;

    /// <summary> Audio manager.<para>音频管理器</para></summary>
    public static AudioManager Audio => AudioManager.Instance;
    
    /// <summary> Scene manager.<para>场景管理器</para></summary>
    public static ScenesManager Scenes => ScenesManager.Instance;

    /// <summary>
    /// Clear memory
    /// <para>清理内存</para>
    /// </summary>
    public static void ClearMemory()
    {
        System.GC.Collect();
        Assets.CleanupUnusedAssets().Forget();
    }

    /// <summary>
    /// Quit Game
    /// <para>退出游戏</para>
    /// </summary>
    public static void QuitGame()
    {
        ClearMemory();
        EFC.QuitGames();
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

    public static void Get()
    {
        
    }
}

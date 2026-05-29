/*
 * ================================================
 * Describe:        EF 门面模板 —— 按项目需求自定义
 * Author:          EFramework Team
 * Version:         1.0
 * ================================================
 *
 * 使用说明：
 *   1. 把本文件拷贝到你的启动程序集（如 ExampleGame/Scripts/）
 *   2. 程序集 asmdef 需引用 EF.Runtime + 各 EF 包的 GUID
 *   3. 核心管理器始终启用；外部包的管理器按需取消注释
 *   4. 没装的包保持注释状态（否则编译报错）
 *
 * 各包 GUID：
 *   UniTask     → f51ebe6a0ceec4240a699833d6309b23
 *   EF.Runtime  → 396fd9bc7a7442941a71f32479ef054d
 *   EF.Audio    → 9790f86e8b373fd4c80ccc6b291ad2b3
 *   EF.UI       → 9e2083eb85898e14fb6b01d689ad482e
 *   EF.Http     → da27c3a15f55a6446a067d73a600c0af
 *   EF.RedDot   → b95e25f724df103448b1a28ae6dc19e7
 *   EF.YooAsset → 41ab4279c1ae09b4ca0241431788d06d
 *   YooAsset    → e34a5702dd353724aa315fb8011f08c3
 */

using Cysharp.Threading.Tasks;
using EasyFramework.Managers;
using EasyFramework.Managers.Event;
using EasyFramework.Managers.Pool;
using EasyFramework.Managers.Procedure;
using EasyFramework.Systems.Assets;
using EasyFramework.Systems.Patch;
using UnityEngine;

// ─── 根据安装的包取消对应 using ────────────────────────────
// 安装了 cn.efefef.audio  → 取消注释: using EasyFramework.Managers;
// 安装了 cn.efefef.ui     → 取消注释: using EasyFramework.Managers.Ui;
// 安装了 cn.efefef.http   → 取消注释: using EasyFramework.Systems.Http;
// 安装了 cn.efefef.reddot → 取消注释: using EasyFramework.Systems.RedDot;
// ──────────────────────────────────────────────────────────

/// <summary>
/// EF 门面类 —— 统一管理器访问入口
/// <para>EF facade — unified manager access entry point</para>
/// </summary>
public sealed class EF
{
    // ============================================================
    //  核心管理器（始终启用 / Always enabled）
    // ============================================================

    /// <summary>对象池管理器<para>Pool manager</para></summary>
    public static PoolManager Pool => PoolManager.Instance;

    /// <summary>事件管理器<para>Event manager</para></summary>
    public static EventsManager Event => EventsManager.Instance;

    /// <summary>流程系统<para>Procedure system</para></summary>
    public static ProcedureManager Procedure => ProcedureManager.Instance;

    /// <summary>资源加载管理器<para>Asset loading manager</para></summary>
    public static AssetsSystem Assets => AssetsSystem.Instance;

    /// <summary>通用工具管理器<para>Universal tools manager</para></summary>
    public static ToolManager Tool => ToolManager.Instance;

    /// <summary>时间管理器<para>Time manager</para></summary>
    public static TimeManager Timer => TimeManager.Instance;

    /// <summary>系统级事件管理器<para>System-level event manager</para></summary>
    public static EventManager Events => EventManager.Instance;

    /// <summary>补丁更新管理器<para>Patch update manager</para></summary>
    public static PatchSystem Patch => PatchSystem.Instance;

    /// <summary>场景管理器<para>Scene manager</para></summary>
    public static ScenesManager Scenes => ScenesManager.Instance;

    // ============================================================
    //  可选管理器 —— 根据安装的包取消注释
    //  Optional managers — uncomment those matching installed packages
    // ============================================================

    // ── 安装了 cn.efefef.audio? ──
    // public static AudioManager Audio => AudioManager.Instance;

    // ── 安装了 cn.efefef.ui? ──
    // public static UiSystem UI => UiSystem.Instance;

    // ── 安装了 cn.efefef.http? ──
    // public static HttpsSystem Http => HttpsSystem.Instance;

    // ── 安装了 cn.efefef.reddot? ──
    // public static RedDotSystem RedDot => RedDotSystem.Instance;

    // ============================================================
    //  工具方法
    // ============================================================

    /// <summary>清理内存<para>Clear memory</para></summary>
    public static void ClearMemory()
    {
        System.GC.Collect();
        Assets.CleanupUnusedAssets().Forget();
    }

    /// <summary>退出游戏<para>Quit game</para></summary>
    public static void QuitGame()
    {
        ClearMemory();
        EFC.QuitGames();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit(0);
    }

#if UNITY_EDITOR
    private static System.Reflection.MethodInfo _clearMethod;

    /// <summary>清空控制台<para>Clear Console</para></summary>
    public static void ClearConsole()
    {
        if (_clearMethod == null)
        {
            System.Type logType = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
            _clearMethod = logType?.GetMethod("Clear");
        }
        _clearMethod?.Invoke(null, null);
    }
#endif
}

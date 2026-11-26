/*
 * ================================================
 * Describe:      GM 总控门面：命令注册/执行/取消、Init 前暂存、生命周期编排。
 * Author:        Alvin8412
 * CreationTime:  2026-11-24
 * ModifyTime:    2026-11-24
 * Version:       0.4
 * ===============================================
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework;

namespace EasyFramework.GameMaster
{
    /// <summary>
    /// GM 总控门面：命令注册、串行执行、取消、审计与生命周期编排。
    /// <para>GM facade: command registration, serial execution, cancellation, audit and lifecycle orchestration.</para>
    /// </summary>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Manager(Order = -1000)]
    public sealed class GameMasterManager : MonoSingleton<GameMasterManager>, ISingleton
    {
        #region 常量

        // 日志前缀
        private const string LogTag = "[ GM ] ";

        // 空参数文本
        private const string EmptyArgs = "";

        #endregion

        #region 静态状态

        // 当前会话；Init 新建，Quit 断开。旧会话由在途命令局部引用持有，不互相污染
        private static GmSession _current;

        // Init 前注册暂存队列（回放时读到当时的 _current，不绑定旧会话）
        private static readonly List<Action> PendingRegistrations = new List<Action>();

        // 是否已完成初始化
        private static bool _initialized;

        #endregion

        #region 生命周期

        void ISingleton.Init()
        {
            // 先建会话再回放暂存，闭包读到的是本次会话
            _current = new GmSession();

            foreach (var action in PendingRegistrations)
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    D.Error(LogTag + "Pending registration failed: " + e.Message);
                }
            }

            PendingRegistrations.Clear();

            _initialized = true;
        }

        void ISingleton.Quit()
        {
            // 先断开真源，再由旧会话自行收尾；在途命令持有局部 session 引用不受影响
            GmSession session = _current;
            _current = null;
            _initialized = false;

            session?.Dispose();
            PendingRegistrations.Clear();
        }

        #endregion

        #region 公开 API

        /// <summary>
        /// 注册或覆盖一条 GM 命令。Init 之前调用会暂存，Init 末尾统一应用。
        /// <para>Registers or replaces a GM command. Calls before Init are queued and applied at Init.</para>
        /// </summary>
        /// <param name="id">命令 ID，须匹配 [A-Za-z_][A-Za-z0-9_.]*。<para>Command ID matching [A-Za-z_][A-Za-z0-9_.]*</para></param>
        /// <param name="display">面板显示名原文。<para>Literal display name.</para></param>
        /// <param name="risk">风险等级。<see cref="GmRisk"/><para>Risk level.</para></param>
        /// <param name="execute">执行委托。<para>Execution delegate.</para></param>
        /// <param name="paramHint">参数提示文本（可选）。<para>Argument hint text (optional).</para></param>
        /// <param name="section">面板分区名，默认 Common。<para>Panel section name; defaults to Common.</para></param>
        public static void Register(string id, string display, GmRisk risk,
            GmCommandDelegate execute, string paramHint = null, string section = "Common")
        {
            EnsureInitialized();

            GmSession session = _current;
            if (_initialized && session is { IsAlive: true })
                session.Register(id, display, risk, execute, paramHint, section);
            else
                // 回放时读当前会话，不绑定旧对象
                PendingRegistrations.Add(() => _current?.Register(id, display, risk, execute, paramHint, section));
        }

        /// <summary>
        /// 移除一条命令。
        /// <para>Removes a command.</para>
        /// </summary>
        /// <param name="id">命令 ID。<para>Command ID.</para></param>
        /// <returns>是否移除成功。<para>Whether removal succeeded.</para></returns>
        public static bool Remove(string id)
        {
            EnsureInitialized();
            GmSession session = _current;
            return session != null && session.Remove(id);
        }

        /// <summary>
        /// 执行指定命令（串行，带统一取消与异常隔离）。
        /// <para>Executes a command serially with unified cancellation and exception isolation.</para>
        /// </summary>
        /// <param name="id">命令 ID。<para>Command ID.</para></param>
        /// <param name="args">参数原始文本（可选）。<para>Raw argument text (optional).</para></param>
        /// <returns>执行结果；框架未就绪时返回失败结果。<para>Execution result; a failed result when not ready.</para></returns>
        public static async UniTask<GmResult> Run(string id, string args = null)
        {
            if (!TryGetReady(out GmSession session))
                return new GmResult(id, false, "NotInitialized");

            // 首个 await 前捕获会话，后续只走实例成员
            return await session.Run(id, args ?? EmptyArgs);
        }

        /// <summary>
        /// 按注册顺序批量执行全部命令（严格串行）。
        /// <para>Executes all commands in registration order, strictly serially.</para>
        /// </summary>
        /// <returns>结果列表。<para>Result list.</para></returns>
        public static async UniTask<List<GmResult>> RunAll()
        {
            if (!TryGetReady(out GmSession session))
            {
                var failed = new List<GmResult> { new GmResult(string.Empty, false, "NotInitialized") };
                return failed;
            }

            return await session.RunAll();
        }

        /// <summary>
        /// 取消当前会话全部命令。
        /// <para>Cancels all commands in the current session.</para>
        /// </summary>
        public static void CancelAll()
        {
            EnsureInitialized();
            _current?.CancelAll();
        }

        /// <summary>
        /// 获取命令元数据快照（供 Editor 面板使用，不含委托）。
        /// <para>Gets a metadata snapshot for the Editor panel, without delegates.</para>
        /// </summary>
        /// <returns>元数据列表；未就绪时为空列表。<para>Metadata list; empty when not ready.</para></returns>
        public static List<GmCommandInfo> GetInfos()
        {
            EnsureInitialized();
            return _current != null ? _current.GetInfos() : new List<GmCommandInfo>();
        }

        /// <summary>
        /// 获取执行历史快照。
        /// <para>Gets a snapshot of the execution history.</para>
        /// </summary>
        /// <returns>结果列表。<para>Result list.</para></returns>
        public static List<GmResult> GetHistory()
        {
            EnsureInitialized();
            return _current != null ? _current.GetHistory() : new List<GmResult>();
        }

        /// <summary>
        /// GM 门面是否已就绪（Editor 面板据此判断 Play 模式数据是否可用）。
        /// <para>Whether the GM facade is ready (used by the Editor panel to check Play-mode data).</para>
        /// </summary>
        public static bool IsReady
        {
            get
            {
                EnsureInitialized();
                return _initialized && _current is { IsAlive: true };
            }
        }

        #endregion

        #region 私有

        // 按需初始化：EFC 不自动实例化 [Manager]，只有访问 Instance 才会走注册→Init 链
        private static void EnsureInitialized()
        {
            // 框架未运行时不能碰 Instance：getter 会打错误日志并返回 null
            if (!EFC.CanCreateSingleton) return;
            if (_initialized && _current != null) return;

            // 注册表是唯一真源：表中无活实例时访问 Instance 触发创建与 Init
            if (!EFC.TryGetSingleton<GameMasterManager>(out _))
                _ = Instance;
        }

        // 就绪判定并取出当前会话；未就绪时记录一次日志
        private static bool TryGetReady(out GmSession session)
        {
            EnsureInitialized();
            session = _current;
            if (_initialized && session is { IsAlive: true }) return true;
            D.Warning(LogTag + "框架未就绪，GM 命令被忽略。请先启动 EF 框架。");
            session = null;
            return false;
        }

        #endregion
    }
#else
    /// <summary>
    /// 非开发构建下的 GM 门面占位：所有 API 均为空操作，便于业务代码无 #if 分支。
    /// <para>GM facade stub for non-development builds: all APIs are no-ops, allowing branch-free call sites.</para>
    /// </summary>
    public static class GameMasterManager
    {
        /// <summary> 空操作注册。<para>No-op registration.</para> </summary>
        public static void Register(string id, string display, GmRisk risk,
            GmCommandDelegate execute, string paramHint = null, string section = "Common") { }

        /// <summary> 空操作移除。<para>No-op removal.</para> </summary>
        public static bool Remove(string id) => false;

        /// <summary> 空操作执行。<para>No-op execution.</para> </summary>
        public static UniTask<GmResult> Run(string id, string args = null) =>
            UniTask.FromResult(new GmResult(id, false, "DisabledInRelease"));

        /// <summary> 空操作批量执行。<para>No-op batch execution.</para> </summary>
        public static UniTask<List<GmResult>> RunAll() =>
            UniTask.FromResult(new List<GmResult>());

        /// <summary> 空操作取消。<para>No-op cancellation.</para> </summary>
        public static void CancelAll() { }

        /// <summary> 返回空元数据列表。<para>Returns an empty metadata list.</para> </summary>
        public static List<GmCommandInfo> GetInfos() => new List<GmCommandInfo>();

        /// <summary> 返回空历史列表。<para>Returns an empty history list.</para> </summary>
        public static List<GmResult> GetHistory() => new List<GmResult>();

        /// <summary> 发布构建中始终未就绪。<para>Always not ready in release builds.</para> </summary>
        public static bool IsReady => false;
    }
#endif
}
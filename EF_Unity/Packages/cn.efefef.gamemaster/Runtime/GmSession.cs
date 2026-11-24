/*
 * ================================================
 * Describe:      GM 会话：单会话内的注册表、串行执行、取消与审计。
 * Author:        Alvin8412
 * CreationTime:  2026-11-24
 * ModifyTime:    2026-11-24
 * Version:       0.1
 * ===============================================
 */

#if UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace EasyFramework.GameMaster
{
    /// <summary>
    /// GM 单会话状态：持有命令注册表与执行状态，由 <see cref="GameMasterManager"/> 在 Init 时创建、Quit 时释放。
    /// <para>Single GM session state: holds the command registry and execution state; created on Init and disposed on
    /// Quit by <see cref="GameMasterManager"/>.</para>
    /// </summary>
    internal sealed class GmSession
    {
        #region 常量

        // 日志前缀
        private const string LogTag = "[ GM ] ";

        // 稳定 ID 正则：以字母/下划线开头，仅字母数字下划线点
        private static readonly System.Text.RegularExpressions.Regex IdPattern = new("^[A-Za-z_][A-Za-z0-9_.]*$");

        // 复用空参数字典
        private static readonly IReadOnlyDictionary<string, object> EmptyArgs = new Dictionary<string, object>(0);

        #endregion

        #region 类型

        // 命令条目：元数据 + 委托
        private sealed class Entry
        {
            internal GmCommandInfo Info;
            internal GmCommandDelegate Execute;
        }

        #endregion

        #region 字段

        // 注册表：稳定 ID -> 命令条目（保序由 List 负责）
        private readonly List<Entry> _entries = new List<Entry>();

        // ID -> 索引，用于幂等覆盖与查找
        private readonly Dictionary<string, int> _index = new Dictionary<string, int>();

        // 执行历史（ID + 结果）
        private readonly List<GmResult> _history = new List<GmResult>();

        // 会话取消令牌源（Quit 后为 null，作为代际守卫）
        private CancellationTokenSource _sessionCts = new CancellationTokenSource();

        // 是否正在执行（重入保护；只影响本会话）
        private bool _running;

        #endregion

        #region 属性

        /// <summary> 会话是否存活。<para>Whether the session is alive.</para> </summary>
        internal bool IsAlive => _sessionCts != null;

        #endregion

        #region 公开（包内）方法

        /// <summary>
        /// 注册或覆盖一条命令（幂等，支撑域重载后的重新注册）。
        /// <para>Registers or replaces a command (idempotent, supports re-registration after domain reload).</para>
        /// </summary>
        /// <param name="id">稳定 ID。<para>Stable ID.</para></param>
        /// <param name="display">显示名原文。<para>Literal display name.</para></param>
        /// <param name="risk">风险等级。<para>Risk level.</para></param>
        /// <param name="execute">执行委托。<para>Execution delegate.</para></param>
        /// <param name="paramKeys">参数键。<para>Argument keys.</para></param>
        /// <param name="section">面板分区名。<para>Panel section name.</para></param>
        internal void Register(string id, string display, GmRisk risk,
            GmCommandDelegate execute, IReadOnlyList<string> paramKeys, string section)
        {
            if (string.IsNullOrEmpty(id) || !IdPattern.IsMatch(id))
            {
                D.Warning(LogTag + "Register rejected: invalid id: " + id);
                return;
            }

            if (string.IsNullOrEmpty(display))
            {
                D.Warning(LogTag + "Register rejected: empty display name for " + id);
                return;
            }

            if (execute == null)
            {
                D.Warning(LogTag + "Register rejected: null delegate for " + id);
                return;
            }

            var entry = new Entry
            {
                Info = new GmCommandInfo(id, display, risk, paramKeys, section),
                Execute = execute,
            };

            if (_index.TryGetValue(id, out int existing))
                _entries[existing] = entry;
            else
            {
                _index[id] = _entries.Count;
                _entries.Add(entry);
            }
        }

        /// <summary>
        /// 移除一条命令。
        /// <para>Removes a command.</para>
        /// </summary>
        /// <param name="id">命令 ID。<para>Command ID.</para></param>
        /// <returns>是否移除成功。<para>Whether removal succeeded.</para></returns>
        internal bool Remove(string id)
        {
            if (!IsAlive || !_index.TryGetValue(id, out int at)) return false;
            _entries.RemoveAt(at);
            RebuildIndex();
            return true;
        }

        /// <summary>
        /// 执行单条命令（串行，带统一取消与异常隔离）。
        /// <para>Executes a command serially with unified cancellation and exception isolation.</para>
        /// </summary>
        /// <param name="id">命令 ID。<para>Command ID.</para></param>
        /// <param name="args">参数字典。<para>Argument dictionary.</para></param>
        /// <returns>执行结果。<para>Execution result.</para></returns>
        internal async UniTask<GmResult> Run(string id, IReadOnlyDictionary<string, object> args)
        {
            if (_running) return new GmResult(id, false, "Busy");

            Entry entry = Find(id);
            if (entry == null) return new GmResult(id, false, "NotFound");

            _running = true;
            try
            {
                return await RunOne(entry, args);
            }
            finally
            {
                // 只清本会话标志，不影响其它会话
                _running = false;
            }
        }

        /// <summary>
        /// 按注册顺序执行全部命令（严格串行）。
        /// <para>Executes all commands in registration order, strictly serially.</para>
        /// </summary>
        /// <returns>执行结果列表。<para>Execution result list.</para></returns>
        internal async UniTask<List<GmResult>> RunAll()
        {
            var results = new List<GmResult>();
            if (_running)
            {
                results.Add(new GmResult(string.Empty, false, "Busy"));
                return results;
            }

            _running = true;
            try
            {
                // 先快照条目，避免命令内 Register 追加导致索引漂移 / 同一批重复执行
                var batch = GetInfos();
                foreach (var gmCommandInfo in batch)
                {
                    Entry entry = Find(gmCommandInfo.Id);
                    if (entry == null) continue;

                    GmResult result = await RunOne(entry, EmptyArgs);
                    results.Add(result);
                }
            }
            finally
            {
                _running = false;
            }

            return results;
        }

        /// <summary>
        /// 取消本会话全部命令。
        /// <para>Cancels all commands in this session.</para>
        /// </summary>
        internal void CancelAll()
        {
            if (_sessionCts is { IsCancellationRequested: false })
                _sessionCts.Cancel();
        }

        /// <summary>
        /// 获取命令元数据快照（不含委托）。
        /// <para>Gets a snapshot of command metadata, without delegates.</para>
        /// </summary>
        /// <returns>元数据列表。<para>Metadata list.</para></returns>
        internal List<GmCommandInfo> GetInfos()
        {
            var snapshot = new List<GmCommandInfo>(_entries.Count);
            foreach (var entry in _entries)
                snapshot.Add(entry.Info);

            return snapshot;
        }

        /// <summary>
        /// 获取执行历史快照。
        /// <para>Gets a snapshot of the execution history.</para>
        /// </summary>
        /// <returns>结果列表。<para>Result list.</para></returns>
        internal List<GmResult> GetHistory()
        {
            var snapshot = new List<GmResult>(_history.Count);
            snapshot.AddRange(_history);
            return snapshot;
        }

        /// <summary>
        /// 会话收尾：先取消、再清表、再释放 CTS；不重置 _running（旧会话自行持有）。
        /// <para>Disposes the session: cancel, clear tables, dispose CTS; _running is left as-is.</para>
        /// </summary>
        internal void Dispose()
        {
            if (_sessionCts is { IsCancellationRequested: false })
                _sessionCts.Cancel();

            _entries.Clear();
            _index.Clear();
            _history.Clear();
            _sessionCts?.Dispose();
            _sessionCts = null;
        }

        #endregion

        #region 私有方法

        // 执行单条命令：统一 attach 取消令牌，命令作者无需关心
        private async UniTask<GmResult> RunOne(Entry entry, IReadOnlyDictionary<string, object> args)
        {
            string id = entry.Info.Id;
            // 本会话 CTS 先取局部：Quit 置空后仍能安全构建 linked
            CancellationTokenSource sessionCts = _sessionCts;
            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(sessionCts.Token);
            var context = new GmCommandContext(args, linked.Token);
            try
            {
                // 关键：统一 attach，取消能真正中断整条 await 链
                await entry.Execute(context).AttachExternalCancellation(linked.Token);
                var ok = new GmResult(id, true, null, args);
                AddHistory(ok);
                return ok;
            }
            catch (OperationCanceledException) when (linked.IsCancellationRequested)
            {
                var canceled = new GmResult(id, false, "Canceled", args);
                AddHistory(canceled);
                return canceled;
            }
            catch (Exception e)
            {
                D.Exception(LogTag + "Command failed: " + id + " -> " + e.Message);
                var failed = new GmResult(id, false, e.Message, args);
                AddHistory(failed);
                return failed;
            }
        }

        // 会话已释放后不再写入历史（守卫读本会话 CTS，杜绝写入新会话）
        private void AddHistory(GmResult result)
        {
            if (_sessionCts == null) return;
            _history.Add(result);
        }

        // 按 ID 查找条目
        private Entry Find(string id)
        {
            if (!_index.TryGetValue(id, out int at)) return null;
            return at >= 0 && at < _entries.Count ? _entries[at] : null;
        }

        // 移除后重建索引
        private void RebuildIndex()
        {
            _index.Clear();
            for (int i = 0; i < _entries.Count; i++)
                _index[_entries[i].Info.Id] = i;
        }

        #endregion
    }
}

#endif
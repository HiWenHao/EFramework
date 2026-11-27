/*
 * ================================================
 * Describe:        非泛型 int ID 状态机基座
 * Author:          Alvin8412
 * CreationTime:    2026-11-27 14:10:00
 * ScriptVersion:   0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;

namespace EasyFramework
{
    /// <summary>
    /// 轻量状态机：用 int 状态 ID 管理互斥状态，不依赖泛型，跨 AOT/热更安全
    /// <para>Lightweight state machine: manages mutually exclusive states with int IDs, non-generic and safe across AOT/hotfix boundary</para>
    /// </summary>
    /// <remarks>每个持有者一个实例，状态 ID 只在实例内部有效；领域 enum 是唯一 ID 真源。
    /// <para>One instance per owner; state IDs are only meaningful inside that instance; the domain enum is the single source of truth.</para></remarks>
    public sealed class StateMachine
    {
        private const string LogPrefix = "[ StateMachine ]";

        // 状态节点：状态名 + 进入/退出回调
        private sealed class StateNode
        {
            public readonly string Name;
            public readonly Action OnEnter;
            public readonly Action OnExit;

            public StateNode(string name, Action onEnter, Action onExit)
            {
                Name = name;
                OnEnter = onEnter;
                OnExit = onExit;
            }
        }

        private readonly string _ownerName;
        private readonly Dictionary<int, StateNode> _states;
        private readonly Dictionary<int, HashSet<int>> _transitions;
        private int _current;
        private bool _initialized;
        private bool _transitioning;

        /// <summary>
        /// 当前状态 ID，未调用 SetInitial 时为默认值
        /// <para>Current state ID; default value before SetInitial is called</para>
        /// </summary>
        public int Current => _current;

        /// <summary>
        /// 当前状态名，未注册时为空字符串
        /// <para>Current state name; empty string when not registered</para>
        /// </summary>
        public string CurrentName => _states.TryGetValue(_current, out var node) ? node.Name : string.Empty;

        /// <summary>
        /// 是否已设置初始状态
        /// <para>Whether the initial state has been set</para>
        /// </summary>
        public bool IsInitialized => _initialized;

        /// <summary>
        /// 状态变更完成事件，参数为旧状态 ID 与新状态 ID
        /// <para>Event raised after a state change, with the old and new state IDs</para>
        /// </summary>
        public event Action<int, int> OnStateChanged;

        /// <summary>
        /// 构造状态机
        /// <para>Construct the state machine</para>
        /// </summary>
        /// <param name="ownerName">持有者名称，用于日志区分 <para>Owner name, used to distinguish log entries</para></param>
        public StateMachine(string ownerName)
        {
            _ownerName = ownerName;
            _states = new Dictionary<int, StateNode>();
            _transitions = new Dictionary<int, HashSet<int>>();
        }

        /// <summary>
        /// 注册状态；同一实例内重复 ID 会被拒绝
        /// <para>Register a state; duplicate IDs within the same instance are rejected</para>
        /// </summary>
        /// <param name="state">状态 ID <para>State ID</para></param>
        /// <param name="name">状态名，用于日志 <para>State name, used in logs</para></param>
        /// <param name="onEnter">进入回调，可选 <para>Enter callback, optional</para></param>
        /// <param name="onExit">退出回调，可选 <para>Exit callback, optional</para></param>
        /// <returns>注册是否成功 <para>Whether registration succeeded</para></returns>
        public bool Register(int state, string name, Action onEnter = null, Action onExit = null)
        {
            if (_states.TryGetValue(state, out var stateValue))
            {
                Warn($"重复注册状态 ID={state}（{name}），已保留原有状态 {stateValue.Name}");
                return false;
            }

            _states[state] = new StateNode(name, onEnter, onExit);
            return true;
        }

        /// <summary>
        /// 允许从 from 转移到 to
        /// <para>Allow transition from  to</para>
        /// </summary>
        /// <param name="from">起始状态 ID <para>Source state ID</para></param>
        /// <param name="to">目标状态 ID <para>Target state ID</para></param>
        /// <returns>注册是否成功 <para>Whether registration succeeded</para></returns>
        public bool Allow(int from, int to)
        {
            if (!_states.ContainsKey(from) || !_states.ContainsKey(to))
            {
                Warn($"转移规则端点未注册：{from} → {to}");
                return false;
            }

            if (!_transitions.TryGetValue(from, out var targets))
            {
                targets = new HashSet<int>();
                _transitions[from] = targets;
            }

            return targets.Add(to);
        }

        /// <summary>
        /// 设置初始状态：只写入当前状态，不触发 OnEnter
        /// <para>Set the initial state: only writes the current state, without triggering OnEnter</para>
        /// </summary>
        /// <param name="state">初始状态 ID <para>Initial state ID</para></param>
        /// <returns>设置是否成功 <para>Whether the setup succeeded</para></returns>
        public bool SetInitial(int state)
        {
            if (_initialized)
            {
                Warn("初始状态已设置过，忽略重复调用");
                return false;
            }

            if (!_states.ContainsKey(state))
            {
                Warn($"初始状态未注册：{state}");
                return false;
            }

            _current = state;
            _initialized = true;
            return true;
        }

        /// <summary>
        /// 尝试转移到目标状态
        /// <para>Try to transition to the target state</para>
        /// </summary>
        /// <param name="next">目标状态 ID <para>Target state ID</para></param>
        /// <returns>转移是否成功 <para>Whether the transition succeeded</para></returns>
        public bool Transition(int next)
        {
            if (!_initialized)
            {
                Warn("尚未设置初始状态，转移被拒绝");
                return false;
            }

            if (_transitioning)
            {
                Warn("检测到回调内重入转移，已拒绝");
                return false;
            }

            if (!_states.TryGetValue(next, out var nextNode))
            {
                Warn($"目标状态未注册：{next}");
                return false;
            }

            if (next == _current)
            {
                Warn($"目标状态与当前状态相同：{nextNode.Name}，已忽略");
                return false;
            }

            if (!_transitions.TryGetValue(_current, out var targets) || !targets.Contains(next))
            {
                Warn($"非法转移：{CurrentName} → {nextNode.Name}");
                return false;
            }

            int previous = _current;
            _transitioning = true;
            try
            {
                InvokeCallback(_states[previous].OnExit, "OnExit", previous);
                _current = next;
                InvokeCallback(nextNode.OnEnter, "OnEnter", next);
                OnStateChanged?.Invoke(previous, next);
                return true;
            }
            finally
            {
                _transitioning = false;
            }
        }

        // 统一告警入口，附带 owner 名称
        private void Warn(string message)
        {
            D.Warning($"{LogPrefix} [{_ownerName}] {message}");
        }

        // 回调异常只记录，不中断状态转移
        private void InvokeCallback(Action callback, string phase, int state)
        {
            if (callback == null)
                return;

            try
            {
                callback();
            }
            catch (Exception e)
            {
                D.Exception($"{LogPrefix} [{_ownerName}] 状态 {state} 的 {phase} 回调异常：{e}");
            }
        }
    }
}
/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-11 16:29:35
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-11 16:29:35
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework.Systems.Procedure;
using UnityEngine;

namespace EasyFramework.Test
{
    /// <summary>
    /// 流程系统测试驱动器 – 构建深度 [1,5]、节点数 ≥10 的流程树，用于观察监控面板。
    /// 挂载到任意 GameObject 上，运行时点击按钮即可启动。
    /// </summary>
    public class ProcedureTreeTest : MonoBehaviour
    {
        [Header("测试配置")] [SerializeField] private float simulateWorkSeconds = 2f;
        [SerializeField] private bool verboseLog = true;

        private void Start()
        {
            Debug.Log("[ProcedureTreeTest] Start() 执行，开始注册流程...");
            RegisterProcedures();
        }

        private void RegisterProcedures()
        {
            if (EF.Procedure == null)
            {
                Debug.LogError("[ProcedureTreeTest] EF.Procedure 为空！请确保场景中存在 ProcedureSystem 组件。");
                return;
            }

            var sys = EF.Procedure;
            sys.Register<RootProcedure>();
            sys.Register<AProcedure>();
            sys.Register<BProcedure>();
            sys.Register<CProcedure>();
            sys.Register<DProcedure>();
            Debug.Log("[ProcedureTreeTest] 流程注册完成。");
        }

        [ContextMenu("启动流程树测试")]
        public async UniTask StartTreeTest()
        {
            Debug.Log("========== 用户点击：启动流程树测试 ==========");

            if (!Application.isPlaying)
            {
                Debug.LogError("[ProcedureTreeTest] 测试只能在 Play Mode 下运行。");
                return;
            }

            if (EF.Procedure == null)
            {
                Debug.LogError("[ProcedureTreeTest] EF.Procedure 为空！请检查框架初始化。");
                return;
            }

            // 确保流程已注册（如果 Start 未执行或被跳过，这里再次注册）
            RegisterProcedures();

            var parameters = new Dictionary<string, object>
            {
                { "baseDelay", simulateWorkSeconds },
                { "verbose", verboseLog }
            };

            try
            {
                Debug.Log("[ProcedureTreeTest] 准备调用 Switch<RootProcedure>...");
                await EF.Procedure.Switch<RootProcedure>(parameters);
                Debug.Log("[ProcedureTreeTest] Switch 执行完成（等待根流程退出）。");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProcedureTreeTest] Switch 过程中发生异常: {ex}");
            }
            finally
            {
                await UniTask.CompletedTask;
            }

            Debug.Log("========== 流程树测试执行完毕 ==========");
        }
    }

    /// <summary>
    /// 根流程（深度 1）
    /// </summary>
    public class RootProcedure : ProcedureBase
    {
        protected override async UniTask OnEnterAsync()
        {
            Log("Enter");
            float delay = GetParam("baseDelay", 0.5f);
            bool verbose = GetParam("verbose", false);

            // 模拟进入工作
            await UniTask.Delay(TimeSpan.FromSeconds(delay * 0.5f), cancellationToken: Token);

            // 启动子流程 A1, A2（深度 2）
            var subTasks = new List<UniTask>();
            subTasks.Add(StartSubAndWait<AProcedure>("A1", delay, verbose));
            subTasks.Add(StartSubAndWait<AProcedure>("A2", delay, verbose));

            await UniTask.WhenAll(subTasks);

            Log("Exit - all children completed");
            await Context.EndProcedure();
            ;
        }

        protected override UniTask OnLeaveAsync()
        {
            Log("Leave (cleanup)");
            return UniTask.CompletedTask;
        }

        private async UniTask StartSubAndWait<T>(string instanceName, float delay, bool verbose) where T : IProcedure
        {
            var param = new Dictionary<string, object>
            {
                { "instanceName", instanceName },
                { "baseDelay", delay },
                { "verbose", verbose }
            };
            await Context.StartSubProcedure<T>(param);
        }

        private void Log(string msg) => Debug.Log($"[Root] {msg}");
    }

    /// <summary>
    /// 流程 A（深度 2） – 启动两个 B 子流程
    /// </summary>
    public class AProcedure : ProcedureBase
    {
        protected override async UniTask OnEnterAsync()
        {
            string name = GetParam("instanceName", "A?");
            float delay = GetParam("baseDelay", 0.5f);
            bool verbose = GetParam("verbose", false);

            Log($"Enter ({name})");
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: Token);

            // 启动 B1, B2（深度 3）
            var subTasks = new List<UniTask>();
            subTasks.Add(StartSubAndWait<BProcedure>($"{name}_B1", delay, verbose));
            subTasks.Add(StartSubAndWait<BProcedure>($"{name}_B2", delay, verbose));

            await UniTask.WhenAll(subTasks);

            Log($"Exit ({name})");
            await Context.EndProcedure();
            ;
        }

        protected override UniTask OnLeaveAsync()
        {
            Log("Leave (cleanup)");
            return UniTask.CompletedTask;
        }

        private async UniTask StartSubAndWait<T>(string instanceName, float delay, bool verbose) where T : IProcedure
        {
            var param = new Dictionary<string, object>
            {
                { "instanceName", instanceName },
                { "baseDelay", delay },
                { "verbose", verbose }
            };
            await Context.StartSubProcedure<T>(param);
        }

        private void Log(string msg) => Debug.Log($"[A] {msg}");
    }

    /// <summary>
    /// 流程 B（深度 3） – 启动两个 C 子流程
    /// </summary>
    public class BProcedure : ProcedureBase
    {
        protected override async UniTask OnEnterAsync()
        {
            string name = GetParam("instanceName", "B?");
            float delay = GetParam("baseDelay", 0.5f);
            bool verbose = GetParam("verbose", false);

            Log($"Enter ({name})");
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: Token);

            // 启动 C1, C2（深度 4）
            var subTasks = new List<UniTask>();
            subTasks.Add(StartSubAndWait<CProcedure>($"{name}_C1", delay, verbose));
            subTasks.Add(StartSubAndWait<CProcedure>($"{name}_C2", delay, verbose));

            await UniTask.WhenAll(subTasks);

            Log($"Exit ({name})");
            await Context.EndProcedure();
            ;
        }

        protected override UniTask OnLeaveAsync()
        {
            Log("Leave (cleanup)");
            return UniTask.CompletedTask;
        }

        private async UniTask StartSubAndWait<T>(string instanceName, float delay, bool verbose) where T : IProcedure
        {
            var param = new Dictionary<string, object>
            {
                { "instanceName", instanceName },
                { "baseDelay", delay },
                { "verbose", verbose }
            };
            await Context.StartSubProcedure<T>(param);
        }

        private void Log(string msg) => Debug.Log($"[B] {msg}");
    }

    /// <summary>
    /// 流程 C（深度 4） – 启动一个 D 子流程（可选，深度 5）
    /// </summary>
    public class CProcedure : ProcedureBase
    {
        protected override async UniTask OnEnterAsync()
        {
            string name = GetParam("instanceName", "C?");
            float delay = GetParam("baseDelay", 0.5f);
            bool verbose = GetParam("verbose", false);

            Log($"Enter ({name})");
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: Token);

            // 启动 D 子流程（深度 5）
            await StartSubAndWait<DProcedure>($"{name}_D1", delay, verbose);

            Log($"Exit ({name})");
            await Context.EndProcedure();
            ;
        }

        protected override UniTask OnLeaveAsync()
        {
            Log("Leave (cleanup)");
            return UniTask.CompletedTask;
        }

        private async UniTask StartSubAndWait<T>(string instanceName, float delay, bool verbose) where T : IProcedure
        {
            var param = new Dictionary<string, object>
            {
                { "instanceName", instanceName },
                { "baseDelay", delay },
                { "verbose", verbose }
            };
            await Context.StartSubProcedure<T>(param);
        }

        private void Log(string msg) => Debug.Log($"[C] {msg}");
    }

    /// <summary>
    /// 流程 D（深度 5） – 叶子节点，不再启动子流程
    /// </summary>
    public class DProcedure : ProcedureBase
    {
        protected override async UniTask OnEnterAsync()
        {
            string name = GetParam("instanceName", "D?");
            float delay = GetParam("baseDelay", 0.5f);
            bool verbose = GetParam("verbose", false);

            Log($"Enter ({name}) - leaf node");
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: Token);

            Log($"Exit ({name})");
            await Context.EndProcedure();
            ;
        }

        protected override UniTask OnLeaveAsync()
        {
            Log("Leave (cleanup)");
            return UniTask.CompletedTask;
        }

        private void Log(string msg) => Debug.Log($"[D] {msg}");
    }
}

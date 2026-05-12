/*
 * ================================================
 * Describe:      复杂流程系统测试驱动器（完整状态覆盖 + 稳定显示Active）
 * Author:        Alvin5100
 * CreationTime:  2026-05-11 16:42:42
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-11 17:30:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EasyFramework.Systems.Procedure.Test
{
    public class ComplexProcedureTest : MonoBehaviour
    {
        public enum TestComplexity
        {
            BasicTree,
            DeepChain,
            WideTree,
            ParallelSerialMix,
            ResultCollection,
            TimeoutTest,
            ChainRepeatTest,
            ExceptionPropagation,
            StateCycleTest,
            AllInOne
        }

        [Header("测试配置")]
        public TestComplexity complexity = TestComplexity.BasicTree;
        public bool verboseLog = true;
        [Header("超时测试配置")]
        public float timeoutDelaySeconds = 10f;
        [Header("链式重复测试配置")]
        public int chainRepeatCount = 5;

        private void Start()
        {
            RegisterAllProcedures();
        }

        private void RegisterAllProcedures()
        {
            var sys = EF.Procedure;
            if (sys == null)
            {
                Debug.LogError("[ComplexTest] EF.Procedure 为空！");
                return;
            }

            sys.Register<RootProc>();
            sys.Register<AProc>();
            sys.Register<BProc>();
            sys.Register<CProc>();
            sys.Register<DProc>();
            sys.Register<EProc>();
            sys.Register<FProc>();
            sys.Register<CollectorProc>();
            sys.Register<CalculatorProc>();
            sys.Register<TimeoutProc>();
            sys.Register<ChainRepeatProc>();
            sys.Register<ExceptionProc>();
            sys.Register<DeepChainProc>();

            Debug.Log("[ComplexTest] 所有流程类型注册完成");
        }

        [ContextMenu("运行当前选中的测试")]
        public async UniTask RunCurrentTest()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[ComplexTest] 只能在 Play Mode 下运行");
                return;
            }

            if (EF.Procedure == null)
            {
                Debug.LogError("[ComplexTest] EF.Procedure 未初始化");
                return;
            }

            Debug.Log($"========== 开始运行测试：{complexity} ==========");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                switch (complexity)
                {
                    case TestComplexity.BasicTree:
                        await RunBasicTree();
                        break;
                    case TestComplexity.DeepChain:
                        await RunDeepChain();
                        break;
                    case TestComplexity.WideTree:
                        await RunWideTree();
                        break;
                    case TestComplexity.ParallelSerialMix:
                        await RunParallelSerialMix();
                        break;
                    case TestComplexity.ResultCollection:
                        await RunResultCollection();
                        break;
                    case TestComplexity.TimeoutTest:
                        await RunTimeoutTest();
                        break;
                    case TestComplexity.ChainRepeatTest:
                        await RunChainRepeatTest();
                        break;
                    case TestComplexity.ExceptionPropagation:
                        await RunExceptionTest();
                        break;
                    case TestComplexity.StateCycleTest:
                        await RunStateCycleTest();
                        break;
                    case TestComplexity.AllInOne:
                        await RunAllInOne();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ComplexTest] 测试异常: {ex}");
            }

            stopwatch.Stop();
            Debug.Log($"========== 测试完成，总耗时 {stopwatch.Elapsed.TotalSeconds:F2} 秒 ==========");
        }

        #region 各测试模式入口

        private async UniTask RunBasicTree()
        {
            var param = new Dictionary<string, object> { { "testMode", "BasicTree" }, { "verbose", verboseLog } };
            await SwitchRoot<RootProc>(param);
        }

        private async UniTask RunDeepChain()
        {
            var param = new Dictionary<string, object> { { "remainingDepth", 6 }, { "currentDepth", 1 }, { "verbose", verboseLog } };
            await SwitchRoot<DeepChainProc>(param);
        }

        private async UniTask RunWideTree()
        {
            var param = new Dictionary<string, object> { { "testMode", "WideTree" }, { "childCount", 10 }, { "verbose", verboseLog } };
            await SwitchRoot<RootProc>(param);
        }

        private async UniTask RunParallelSerialMix()
        {
            var param = new Dictionary<string, object> { { "testMode", "ParallelSerialMix" }, { "parallelBranches", 3 }, { "serialDepth", 3 }, { "verbose", verboseLog } };
            await SwitchRoot<RootProc>(param);
        }

        private async UniTask RunResultCollection()
        {
            var param = new Dictionary<string, object> { { "targetSum", 100 }, { "verbose", verboseLog } };
            await SwitchRoot<CollectorProc>(param);
        }

        private async UniTask RunTimeoutTest()
        {
            var param = new Dictionary<string, object> { { "timeoutDelay", timeoutDelaySeconds }, { "verbose", verboseLog } };
            Debug.LogWarning("[ComplexTest] 超时测试需要将 ProcedureSystem 的 defaultTimeoutSeconds 改为小于 timeoutDelay 的值（例如3秒）");
            await SwitchRoot<TimeoutProc>(param);
        }

        private async UniTask RunChainRepeatTest()
        {
            var param = new Dictionary<string, object> { { "remaining", chainRepeatCount }, { "verbose", verboseLog } };
            await SwitchRoot<ChainRepeatProc>(param);
        }

        private async UniTask RunExceptionTest()
        {
            var param = new Dictionary<string, object> { { "verbose", verboseLog } };
            await SwitchRoot<ExceptionProc>(param);
        }

        private async UniTask RunStateCycleTest()
        {
            Debug.Log("[StateCycleTest] 开始逐个测试所有流程状态...");

            var allProcedureTypes = new List<Type>
            {
                typeof(RootProc), typeof(AProc), typeof(BProc), typeof(CProc), typeof(DProc),
                typeof(EProc), typeof(FProc), typeof(CollectorProc), typeof(CalculatorProc),
                typeof(TimeoutProc), typeof(ChainRepeatProc), typeof(ExceptionProc), typeof(DeepChainProc)
            };

            int index = 1;
            foreach (var procType in allProcedureTypes)
            {
                Debug.Log($"\n===== [{index}/{allProcedureTypes.Count}] 测试流程: {procType.Name} =====");
                var param = new Dictionary<string, object> { { "verbose", verboseLog } };

                if (procType == typeof(ChainRepeatProc))
                    param["remaining"] = 3;
                else if (procType == typeof(DeepChainProc))
                {
                    param["remainingDepth"] = 4;
                    param["currentDepth"] = 1;
                }
                else if (procType == typeof(TimeoutProc))
                    param["timeoutDelay"] = 999f;
                else if (procType == typeof(CollectorProc))
                    param["targetSum"] = 50;
                else if (procType == typeof(CalculatorProc))
                {
                    param["inputValue"] = 10;
                    param["resultKey"] = "test_calc";
                }

                try
                {
                    await SwitchRootDynamic(procType, param);
                    Debug.Log($"[StateCycleTest] {procType.Name} 已完成，状态机闭环。");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[StateCycleTest] {procType.Name} 执行异常（预期）: {e.Message}");
                }
                await UniTask.Delay(500);
                index++;
            }
            Debug.Log("[StateCycleTest] 所有流程状态覆盖测试完成！");
        }

        private async UniTask RunAllInOne()
        {
            var tests = new[]
            {
                TestComplexity.BasicTree,
                TestComplexity.DeepChain,
                TestComplexity.WideTree,
                TestComplexity.ParallelSerialMix,
                TestComplexity.ResultCollection,
                TestComplexity.ChainRepeatTest,
                TestComplexity.ExceptionPropagation,
                TestComplexity.StateCycleTest
            };
            foreach (var test in tests)
            {
                Debug.Log($"--- 子测试: {test} ---");
                complexity = test;
                await RunCurrentTest();
                await UniTask.Delay(1000);
            }
        }

        #endregion

        #region 辅助反射启动方法

        private async UniTask SwitchRoot<T>(Dictionary<string, object> param) where T : IProcedure
        {
            await EF.Procedure.Switch<T>(param);
        }

        private async UniTask SwitchRootDynamic(Type procType, Dictionary<string, object> param)
        {
            var method = typeof(ProcedureSystem).GetMethod("Switch");
            if (method == null)
            {
                Debug.LogError("Switch 方法未找到");
                return;
            }
            var generic = method.MakeGenericMethod(procType);
            var taskObj = generic.Invoke(EF.Procedure, new object[] { param });
            if (taskObj is UniTask task)
                await task;
            else
                Debug.LogError($"启动 {procType.Name} 失败，返回值不是 UniTask");
        }

        #endregion

        #region 辅助随机等待

        private static async UniTask RandomDelay(string stepName, float minSec = 2f, float maxSec = 5f, System.Threading.CancellationToken token = default)
        {
            float waitSec = UnityEngine.Random.Range(minSec, maxSec);
            Debug.Log($"[{stepName}] 随机等待 {waitSec:F1}s...");
            await UniTask.Delay(TimeSpan.FromSeconds(waitSec), cancellationToken: token);
            Debug.Log($"[{stepName}] 等待结束");
        }

        #endregion

        #region 流程定义（每个流程末尾显式延迟并退出）

        public class RootProc : ProcedureBase
        {
            protected override async UniTask OnEnterAsync()
            {
                bool verbose = GetParam("verbose", false);
                string mode = GetParam("testMode", "BasicTree") as string;
                Log($"Enter Root, mode={mode} [State: Entering]");
                await RandomDelay("RootProc", 2f, 5f, Token);

                if (mode == "BasicTree")
                {
                    var tasks = new List<UniTask>
                    {
                        StartSub<AProc>("A1", verbose),
                        StartSub<AProc>("A2", verbose)
                    };
                    await UniTask.WhenAll(tasks);
                }
                else if (mode == "WideTree")
                {
                    int childCount = GetParam("childCount", 10);
                    var tasks = new List<UniTask>();
                    for (int i = 0; i < childCount; i++)
                        tasks.Add(StartSub<AProc>($"Child_{i}", verbose));
                    await UniTask.WhenAll(tasks);
                }
                else if (mode == "ParallelSerialMix")
                {
                    int branches = GetParam("parallelBranches", 3);
                    int serialDepth = GetParam("serialDepth", 3);
                    var tasks = new List<UniTask>();
                    for (int i = 0; i < branches; i++)
                        tasks.Add(RunSerialChain(i, serialDepth, verbose));
                    await UniTask.WhenAll(tasks);
                }

                Log("RootProc 主要逻辑完成，保持 Active 状态 1.5 秒后退出");
                await UniTask.Delay(1500, cancellationToken: Token);
                await Context.EndProcedure();
            }

            private async UniTask RunSerialChain(int branchId, int depth, bool verbose)
            {
                var param = new Dictionary<string, object>
                {
                    { "instanceName", $"Branch{branchId}_L0" },
                    { "verbose", verbose },
                    { "remainingDepth", depth - 1 }
                };
                await Context.StartSubProcedure<AProc>(param);
            }

            private async UniTask StartSub<T>(string name, bool verbose) where T : IProcedure
            {
                var param = new Dictionary<string, object> { { "instanceName", name }, { "verbose", verbose } };
                await Context.StartSubProcedure<T>(param);
            }

            protected override UniTask OnLeaveAsync()
            {
                Log("Root Leave");
                return UniTask.CompletedTask;
            }
            private void Log(string msg) => Debug.Log($"[RootProc] {msg}");
        }

        public class AProc : ProcedureBase
        {
            protected override async UniTask OnEnterAsync()
            {
                string name = GetParam("instanceName", "A?");
                bool verbose = GetParam("verbose", false);
                int remaining = GetParam("remainingDepth", 2);
                Log($"Enter {name}, remaining={remaining} [State: Entering]");
                await RandomDelay($"AProc({name})", 2f, 5f, Token);

                if (remaining > 0)
                {
                    var tasks = new List<UniTask>
                    {
                        StartSub<BProc>($"{name}_B1", verbose, remaining - 1),
                        StartSub<BProc>($"{name}_B2", verbose, remaining - 1)
                    };
                    await UniTask.WhenAll(tasks);
                }

                Log($"{name} 保持 Active 状态 1.5 秒后退出");
                await UniTask.Delay(1500, cancellationToken: Token);
                await Context.EndProcedure();
            }

            private async UniTask StartSub<T>(string subName, bool verbose, int rem) where T : IProcedure
            {
                var param = new Dictionary<string, object> { { "instanceName", subName }, { "verbose", verbose }, { "remainingDepth", rem } };
                await Context.StartSubProcedure<T>(param);
            }
            protected override UniTask OnLeaveAsync() => UniTask.CompletedTask;
            private void Log(string msg) => Debug.Log($"[AProc] {msg}");
        }

        public class BProc : ProcedureBase
        {
            protected override async UniTask OnEnterAsync()
            {
                string name = GetParam("instanceName", "B?");
                bool verbose = GetParam("verbose", false);
                int remaining = GetParam("remainingDepth", 1);
                Log($"Enter {name}, remaining={remaining} [State: Entering]");
                await RandomDelay($"BProc({name})", 2f, 5f, Token);
                if (remaining > 0)
                {
                    await StartSub<CProc>($"{name}_C1", verbose, remaining - 1);
                    await StartSub<CProc>($"{name}_C2", verbose, remaining - 1);
                }
                Log($"{name} 保持 Active 状态 1.5 秒后退出");
                await UniTask.Delay(1500, cancellationToken: Token);
                await Context.EndProcedure();
            }

            private async UniTask StartSub<T>(string subName, bool verbose, int rem) where T : IProcedure
            {
                var param = new Dictionary<string, object> { { "instanceName", subName }, { "verbose", verbose }, { "remainingDepth", rem } };
                await Context.StartSubProcedure<T>(param);
            }
            protected override UniTask OnLeaveAsync() => UniTask.CompletedTask;
            private void Log(string msg) => Debug.Log($"[BProc] {msg}");
        }

        public class CProc : ProcedureBase
        {
            protected override async UniTask OnEnterAsync()
            {
                string name = GetParam("instanceName", "C?");
                bool verbose = GetParam("verbose", false);
                int remaining = GetParam("remainingDepth", 0);
                Log($"Enter {name}, remaining={remaining} [State: Entering]");
                await RandomDelay($"CProc({name})", 2f, 5f, Token);
                if (remaining > 0)
                {
                    await StartSub<DProc>($"{name}_D1", verbose, remaining - 1);
                    await StartSub<DProc>($"{name}_D2", verbose, remaining - 1);
                }
                Log($"{name} 保持 Active 状态 1.5 秒后退出");
                await UniTask.Delay(1500, cancellationToken: Token);
                await Context.EndProcedure();
            }

            private async UniTask StartSub<T>(string subName, bool verbose, int rem) where T : IProcedure
            {
                var param = new Dictionary<string, object> { { "instanceName", subName }, { "verbose", verbose }, { "remainingDepth", rem } };
                await Context.StartSubProcedure<T>(param);
            }
            protected override UniTask OnLeaveAsync() => UniTask.CompletedTask;
            private void Log(string msg) => Debug.Log($"[CProc] {msg}");
        }

        public class DProc : ProcedureBase
        {
            protected override async UniTask OnEnterAsync()
            {
                string name = GetParam("instanceName", "D?");
                bool verbose = GetParam("verbose", false);
                int remaining = GetParam("remainingDepth", 0);
                Log($"Enter {name}, remaining={remaining} [State: Entering]");
                await RandomDelay($"DProc({name})", 2f, 5f, Token);
                if (remaining > 0)
                {
                    await StartSub<EProc>($"{name}_E1", verbose, remaining - 1);
                    await StartSub<EProc>($"{name}_E2", verbose, remaining - 1);
                }
                Log($"{name} 保持 Active 状态 1.5 秒后退出");
                await UniTask.Delay(1500, cancellationToken: Token);
                await Context.EndProcedure();
            }

            private async UniTask StartSub<T>(string subName, bool verbose, int rem) where T : IProcedure
            {
                var param = new Dictionary<string, object> { { "instanceName", subName }, { "verbose", verbose }, { "remainingDepth", rem } };
                await Context.StartSubProcedure<T>(param);
            }
            protected override UniTask OnLeaveAsync() => UniTask.CompletedTask;
            private void Log(string msg) => Debug.Log($"[DProc] {msg}");
        }

        public class EProc : ProcedureBase
        {
            protected override async UniTask OnEnterAsync()
            {
                string name = GetParam("instanceName", "E?");
                bool verbose = GetParam("verbose", false);
                int remaining = GetParam("remainingDepth", 0);
                Log($"Enter {name}, remaining={remaining} [State: Entering]");
                await RandomDelay($"EProc({name})", 2f, 5f, Token);
                if (remaining > 0)
                {
                    await StartSub<FProc>($"{name}_F1", verbose, remaining - 1);
                    await StartSub<FProc>($"{name}_F2", verbose, remaining - 1);
                }
                Log($"{name} 保持 Active 状态 1.5 秒后退出");
                await UniTask.Delay(1500, cancellationToken: Token);
                await Context.EndProcedure();
            }

            private async UniTask StartSub<T>(string subName, bool verbose, int rem) where T : IProcedure
            {
                var param = new Dictionary<string, object> { { "instanceName", subName }, { "verbose", verbose }, { "remainingDepth", rem } };
                await Context.StartSubProcedure<T>(param);
            }
            protected override UniTask OnLeaveAsync() => UniTask.CompletedTask;
            private void Log(string msg) => Debug.Log($"[EProc] {msg}");
        }

        public class FProc : ProcedureBase
        {
            protected override async UniTask OnEnterAsync()
            {
                string name = GetParam("instanceName", "F?");
                bool verbose = GetParam("verbose", false);
                Log($"Enter {name} (leaf) [State: Entering]");
                await RandomDelay($"FProc({name})", 2f, 5f, Token);
                Log($"{name} 保持 Active 状态 1.5 秒后退出");
                await UniTask.Delay(1500, cancellationToken: Token);
                await Context.EndProcedure();
            }
            protected override UniTask OnLeaveAsync() => UniTask.CompletedTask;
            private void Log(string msg) => Debug.Log($"[FProc] {msg}");
        }

        // 辅助容器
        public static class ResultContainer
        {
            private static Dictionary<string, int> _results = new();
            public static void Store(string key, int value) => _results[key] = value;
            public static int Retrieve(string key) => _results.TryGetValue(key, out int v) ? v : 0;
            public static void Clear() => _results.Clear();
        }

        public class CalculatorProc : ProcedureBase
        {
            protected override async UniTask OnEnterAsync()
            {
                int input = GetParam("inputValue", 0);
                string resultKey = GetParam("resultKey", "default");
                bool verbose = GetParam("verbose", false);
                Log($"Enter, input={input}, resultKey={resultKey} [State: Entering]");
                await RandomDelay("CalculatorProc", 2f, 5f, Token);
                int output = input * 2;
                ResultContainer.Store(resultKey, output);
                Log($"计算结果 {output} 已存储，保持 Active 状态 1.5 秒后退出");
                await UniTask.Delay(1500, cancellationToken: Token);
                await Context.EndProcedure();
            }
            protected override UniTask OnLeaveAsync() => UniTask.CompletedTask;
            private void Log(string msg) => Debug.Log($"[CalculatorProc] {msg}");
        }

        public class CollectorProc : ProcedureBase
        {
            protected override async UniTask OnEnterAsync()
            {
                bool verbose = GetParam("verbose", false);
                int target = GetParam("targetSum", 100);
                Log($"Enter, target={target} [State: Entering]");
                ResultContainer.Clear();
                var tasks = new List<UniTask>();
                for (int i = 0; i < 5; i++)
                {
                    int value = i * 10;
                    string key = $"result_{i}";
                    tasks.Add(StartCalculator(value, key, verbose));
                }
                await UniTask.WhenAll(tasks);
                int total = 0;
                for (int i = 0; i < 5; i++) total += ResultContainer.Retrieve($"result_{i}");
                Log($"收集总和 = {total}，保持 Active 状态 1.5 秒后退出");
                await UniTask.Delay(1500, cancellationToken: Token);
                await Context.EndProcedure();
            }

            private async UniTask StartCalculator(int input, string key, bool verbose)
            {
                var param = new Dictionary<string, object> { { "inputValue", input }, { "resultKey", key }, { "verbose", verbose } };
                await Context.StartSubProcedure<CalculatorProc>(param);
            }
            protected override UniTask OnLeaveAsync() => UniTask.CompletedTask;
            private void Log(string msg) => Debug.Log($"[CollectorProc] {msg}");
        }

        public class TimeoutProc : ProcedureBase
        {
            protected override async UniTask OnEnterAsync()
            {
                float timeout = GetParam("timeoutDelay", 300f);
                bool verbose = GetParam("verbose", false);
                Log($"Enter, timeout={timeout} [State: Entering]");
                float waitSec = timeout < 5f ? timeout : Random.Range(2f, 5f);
                await UniTask.Delay(TimeSpan.FromSeconds(waitSec), cancellationToken: Token);
                Log($"等待结束，保持 Active 状态 1.5 秒后退出");
                await UniTask.Delay(1500, cancellationToken: Token);
                await Context.EndProcedure();
            }
            protected override UniTask OnLeaveAsync() => UniTask.CompletedTask;
            private void Log(string msg) => Debug.Log($"[TimeoutProc] {msg}");
        }

        public class ChainRepeatProc : ProcedureBase
        {
            protected override async UniTask OnEnterAsync()
            {
                int remaining = GetParam("remaining", 5);
                bool verbose = GetParam("verbose", false);
                Log($"Enter, remaining={remaining} [State: Entering]");
                await RandomDelay("ChainRepeatProc", 2f, 5f, Token);
                if (remaining <= 1)
                {
                    Log("叶子节点，保持 Active 1.5 秒后退出");
                    await UniTask.Delay(1500, cancellationToken: Token);
                    await Context.EndProcedure();
                    return;
                }
                var param = new Dictionary<string, object> { { "remaining", remaining - 1 }, { "verbose", verbose } };
                await Context.StartSubProcedure<ChainRepeatProc>(param);
                Log($"本层结束，保持 Active 1.5 秒后退出");
                await UniTask.Delay(1500, cancellationToken: Token);
                await Context.EndProcedure();
            }
            protected override UniTask OnLeaveAsync() => UniTask.CompletedTask;
            private void Log(string msg) => Debug.Log($"[ChainRepeatProc] {msg}");
        }

        public class ExceptionProc : ProcedureBase
        {
            protected override async UniTask OnEnterAsync()
            {
                bool verbose = GetParam("verbose", false);
                Log($"Enter, 即将抛出异常 [State: Entering]");
                await RandomDelay("ExceptionProc", 2f, 5f, Token);
                throw new InvalidOperationException("主动抛出的测试异常，用于验证 Exception 退出原因");
            }
            protected override UniTask OnLeaveAsync()
            {
                Debug.Log("[ExceptionProc] Leave (异常清理)");
                return UniTask.CompletedTask;
            }
            private void Log(string msg) => Debug.Log($"[ExceptionProc] {msg}");
        }

        public class DeepChainProc : ProcedureBase
        {
            protected override async UniTask OnEnterAsync()
            {
                int remainingDepth = GetParam("remainingDepth", 1);
                int currentDepth = GetParam("currentDepth", 1);
                bool verbose = GetParam("verbose", false);
                Log($"Depth {currentDepth}, remaining {remainingDepth} [State: Entering]");
                await RandomDelay($"DeepChainProc(Depth{currentDepth})", 2f, 5f, Token);
                if (remainingDepth > 1)
                {
                    var param = new Dictionary<string, object>
                    {
                        { "remainingDepth", remainingDepth - 1 },
                        { "currentDepth", currentDepth + 1 },
                        { "verbose", verbose }
                    };
                    await Context.StartSubProcedure<DeepChainProc>(param);
                }
                Log($"深度 {currentDepth} 结束，保持 Active 1.5 秒后退出");
                await UniTask.Delay(1500, cancellationToken: Token);
                await Context.EndProcedure();
            }
            protected override UniTask OnLeaveAsync() => UniTask.CompletedTask;
            private void Log(string msg) => Debug.Log($"[DeepChainProc] {msg}");
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework.Systems.Procedure;
using UnityEngine;

public class ProcedureTestSuite : MonoBehaviour
{
    private async void Start()
    {
        await UniTask.Yield();
        await UniTask.Yield();
        await UniTask.Yield();
        await UniTask.Yield();
        await UniTask.Yield();
        // 临时将默认超时改为 3 秒以便测试超时（也可以不改，但需要等待 300 秒）
        // 注意：ProcedureSystem 的 defaultTimeoutSeconds 是 private，这里通过反射修改（仅测试用）
        var mgr = EF.Procedure;
        var field = typeof(ProcedureSystem).GetField("defaultTimeoutSeconds", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(mgr, 3f);

        // 注册流程类型
        mgr.Register<RootProcedure>();
        mgr.Register<ChildProcedure>();
        mgr.Register<DeepProcedure>();

        Debug.Log("===== 开始流程管理器测试 =====");

        // 启动根流程
        var param = new Dictionary<string, object> { { "startTime", DateTime.Now } };
        await mgr.Switch<RootProcedure>(param);

        Debug.Log("===== 所有测试完成 =====");
    }

    // ==================== 测试流程定义 ====================

    public class RootProcedure : ProcedureBase
    {
        private int _updateCount;

        protected override async UniTask OnEnterAsync()
        {
            Debug.Log($"[Root] 进入，参数 startTime = {GetParam<DateTime>("startTime")}");
            await UniTask.Delay(1000); // 模拟初始化

            // 启动子流程
            var childParams = new Dictionary<string, object> { { "message", "Hello from Root" } };
            await Ctx.StartSubProcedure<ChildProcedure>(childParams);
        }

        protected override async UniTask OnLeaveAsync()
        {
            Debug.Log("[Root] 离开");
            await UniTask.CompletedTask;
        }

        public override void OnUpdate(float elapse, float realElapse)
        {
            _updateCount++;
            if (_updateCount % 60 == 0) // 大约 1 秒输出一次（假设 60fps）
            {
                Debug.Log($"[Root] 持续运行中... 已运行 {_updateCount / 60} 秒");
            }
        }
    }

    public class ChildProcedure : ProcedureBase
    {
        protected override async UniTask OnEnterAsync()
        {
            Debug.Log($"[Child] 进入，参数 message = {GetParam<string>("message")}");
            await UniTask.Delay(500);

            // 启动一个深层流程
            await Ctx.StartSubProcedure<DeepProcedure>();
        }

        protected override async UniTask OnLeaveAsync()
        {
            Debug.Log("[Child] 离开");
            await UniTask.CompletedTask;
        }
    }

    public class DeepProcedure : ProcedureBase
    {
        private static int _instanceCount = 0;
        private int _myId;

        protected override async UniTask OnEnterAsync()
        {
            _myId = ++_instanceCount;
            Debug.Log($"[Deep#{_myId}] 进入，Depth={Depth}, Uid={Uid}");
            // 如果是第一次进入的 DeepProcedure（ID=1），等待 2 秒后自动退出
            if (_myId == 1)
            {
                await UniTask.Delay(2000);
                Debug.Log($"[Deep#{_myId}] 2 秒到期，主动退出");
                await Ctx.EndProcedure();
            }
            else
            {
                // 第二次进入的 DeepProcedure 模拟较长时间工作，测试超时
                Debug.Log($"[Deep#{_myId}] 将运行超过 3 秒，等待超时触发");
                await UniTask.Delay(5000); // 超过超时时间 3 秒
                Debug.Log($"[Deep#{_myId}] 正常结束（不应执行到此，因为超时已经强制退出）");
            }
        }

        protected override async UniTask OnLeaveAsync()
        {
            Debug.Log($"[Deep#{_myId}] 离开");
            await UniTask.CompletedTask;
        }
    }
}
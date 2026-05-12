using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.Systems.Event;
using UnityEngine;

/// <summary>
/// EventSystem 全功能冒烟测试——约10秒内执行所有公开方法
/// <para>Smoke test for EventSystem, covering all public APIs within ~10 seconds</para>
/// </summary>
public class EventSystemTest : MonoBehaviour
{
    // 测试用事件（全部符合 where T : struct）
    public struct TestEventA { public int Value; }
    public struct TestEventB { public string Message; }
    public struct TestEventC { public float Float; }

    private EventSystem _eventSystem;

    private async void Start()
    {
        _eventSystem = EventSystem.Instance;
        Debug.Log("===== EventSystem 测试开始 =====");

        // 1. 基础同步订阅与发布
        Test_SyncSubscribeAndPublish();
        await UniTask.Delay(500);

        // 2. 异步订阅与 PublishAsync
        await Test_AsyncSubscribeAndPublishAsync();
        await UniTask.Delay(1000);

        // 3. 分组订阅与取消分组
        Test_GroupUnsubscribe();
        await UniTask.Delay(800);

        // 4. 延迟队列 (Enqueue / Flush)
        Test_DelayedQueue();
        await UniTask.Delay(1200);

        // 5. 单个令牌取消
        Test_TokenDispose();
        await UniTask.Delay(800);

        // 6. UnsubscribeAll 清空特定事件
        Test_UnsubscribeAll();
        await UniTask.Delay(800);

        // 7. 空订阅/无订阅发布（健壮性）
        Test_EmptyPublish();
        await UniTask.Delay(500);

        // 8. ClearDelayedEvents（取消延迟队列）
        Test_ClearDelayedEvents();
        await UniTask.Delay(600);

        // 9. 多异步并行等待
        await Test_MultipleAsyncHandlers();
        await UniTask.Delay(1000);

        // 10. CancellationToken 取消异步发布
        Test_Cancellation();
        await UniTask.Delay(1500);

        Debug.Log("===== EventSystem 测试全部完成 =====");
    }

    private void Test_SyncSubscribeAndPublish()
    {
        Debug.Log(">>> 测试1：同步订阅与发布");
        int callCount = 0;
        IDisposable token = _eventSystem.Subscribe<TestEventA>(e =>
        {
            callCount++;
            Debug.Log($"  收到 TestEventA, Value = {e.Value}");
        });

        _eventSystem.Publish(new TestEventA { Value = 42 });
        _eventSystem.Publish(new TestEventA { Value = 99 });

        if (callCount == 2) Debug.Log("  [PASS] 同步事件收到2次");
        else Debug.LogError("  [FAIL] 同步事件计数错误");

        token.Dispose();
        _eventSystem.Publish(new TestEventA { Value = -1 });
        if (callCount == 2) Debug.Log("  [PASS] 取消订阅后不再触发");
        else Debug.LogError("  [FAIL] 取消订阅未生效");
    }

    private async UniTask Test_AsyncSubscribeAndPublishAsync()
    {
        Debug.Log(">>> 测试2：异步订阅与 PublishAsync");
        bool asyncReceived = false;

        _eventSystem.Subscribe<TestEventB>(async e =>
        {
            await UniTask.Delay(300);
            Debug.Log($"  async收到 TestEventB: {e.Message}");
            asyncReceived = true;
        });

        await _eventSystem.PublishAsync(new TestEventB { Message = "异步发布测试" });

        if (asyncReceived)
            Debug.Log("  [PASS] 异步处理完成");
        else
            Debug.LogError("  [FAIL] 异步处理器未执行");

        // 清理
        _eventSystem.UnsubscribeAll<TestEventB>();
    }

    private void Test_GroupUnsubscribe()
    {
        Debug.Log(">>> 测试3：分组订阅与 UnsubscribeGroup");
        int groupCall = 0;

        _eventSystem.Subscribe<TestEventA>(e => groupCall++, group: "TestGroup");
        _eventSystem.Subscribe<TestEventA>(e => groupCall++, group: "OtherGroup");
        _eventSystem.Subscribe<TestEventB>(e => { }, group: "TestGroup"); // 不同事件也进组

        _eventSystem.Publish(new TestEventA { Value = 1 });
        if (groupCall == 2) Debug.Log("  [PASS] 初始两个处理器触发");
        else Debug.LogError("  [FAIL] 初始计数不对");

        // 移除 "TestGroup"
        _eventSystem.UnsubscribeGroup("TestGroup");
        groupCall = 0;
        _eventSystem.Publish(new TestEventA { Value = 2 });
        if (groupCall == 1) Debug.Log("  [PASS] 取消分组后仅剩OtherGroup");
        else Debug.LogError("  [FAIL] 分组移除不彻底");

        // 清理
        _eventSystem.UnsubscribeAll<TestEventA>();
        _eventSystem.UnsubscribeAll<TestEventB>();
    }

    private void Test_DelayedQueue()
    {
        Debug.Log(">>> 测试4：延迟队列 Enqueue / Flush");
        int delayedCount = 0;
        _eventSystem.Subscribe<TestEventC>(e => delayedCount++);

        _eventSystem.Enqueue(new TestEventC { Float = 1.0f });
        _eventSystem.Enqueue(new TestEventC { Float = 2.0f });
        if (delayedCount == 0) Debug.Log("  [PASS] Enqueue 后未立即触发");
        else Debug.LogError("  [FAIL] Enqueue 意外触发了处理器");

        _eventSystem.Flush();
        if (delayedCount == 2) Debug.Log("  [PASS] Flush 后触发2次");
        else Debug.LogError("  [FAIL] Flush 计数错误");

        _eventSystem.UnsubscribeAll<TestEventC>();
    }

    private void Test_TokenDispose()
    {
        Debug.Log(">>> 测试5：单个令牌取消");
        int tokenCount = 0;
        IDisposable token = _eventSystem.Subscribe<TestEventA>(e => tokenCount++);

        _eventSystem.Publish(new TestEventA());
        token.Dispose();
        _eventSystem.Publish(new TestEventA());

        if (tokenCount == 1) Debug.Log("  [PASS] Dispose 后只触发1次");
        else Debug.LogError("  [FAIL] 令牌移除失败");
    }

    private void Test_UnsubscribeAll()
    {
        Debug.Log(">>> 测试6：UnsubscribeAll<T> 清空特定事件");
        _eventSystem.Subscribe<TestEventC>(e => { });
        _eventSystem.Subscribe<TestEventC>(e => { });
        _eventSystem.UnsubscribeAll<TestEventC>();

        // 发布不会触发
        _eventSystem.Publish(new TestEventC());
        Debug.Log("  [PASS] UnsubscribeAll 后无异常(如果上面没抛异常则通过)");
    }

    private void Test_EmptyPublish()
    {
        Debug.Log(">>> 测试7：空订阅发布");
        _eventSystem.UnsubscribeAll<TestEventA>(); // 确保空
        _eventSystem.Publish(new TestEventA());    // 不应崩溃
        Debug.Log("  [PASS] 无订阅发布无异常");
    }

    private void Test_ClearDelayedEvents()
    {
        Debug.Log(">>> 测试8：ClearDelayedEvents");
        int afterClear = 0;
        _eventSystem.Subscribe<TestEventC>(e => afterClear++);

        _eventSystem.Enqueue(new TestEventC());
        _eventSystem.ClearDelayedEvents();
        _eventSystem.Flush();

        if (afterClear == 0) Debug.Log("  [PASS] 清空后 Flush 不触发");
        else Debug.LogError("  [FAIL] 清除未生效");

        _eventSystem.UnsubscribeAll<TestEventC>();
    }

    private async UniTask Test_MultipleAsyncHandlers()
    {
        Debug.Log(">>> 测试9：多个异步并行等待");
        int asyncA = 0, asyncB = 0;
        _eventSystem.Subscribe<TestEventB>(async e =>
        {
            await UniTask.Delay(200);
            asyncA++;
        });
        _eventSystem.Subscribe<TestEventB>(async e =>
        {
            await UniTask.Delay(100);
            asyncB++;
        });

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _eventSystem.PublishAsync(new TestEventB { Message = "并行" });
        sw.Stop();

        if (asyncA == 1 && asyncB == 1) Debug.Log("  [PASS] 两个异步处理器均完成");
        else Debug.LogError("  [FAIL] 异步处理器执行不完整");

        Debug.Log($"  耗时: {sw.ElapsedMilliseconds}ms (应接近200ms)");
        _eventSystem.UnsubscribeAll<TestEventB>();
    }

    private void Test_Cancellation()
    {
        Debug.Log(">>> 测试10：CancellationToken 取消异步发布");
        bool cancelled = false;
        var cts = new CancellationTokenSource();
        cts.Cancel(); // 立即取消

        _eventSystem.Subscribe<TestEventB>(async e =>
        {
            await UniTask.Delay(1000); // 永远不会执行完
            cancelled = false;
        });

        try
        {
            _eventSystem.PublishAsync(new TestEventB(), cts.Token).Forget(); // 不需要等待，观测异常
        }
        catch (OperationCanceledException) { }
        // 由于内部已捕获异常，不会抛出，这里仅测试是否不崩溃
        Debug.Log("  [PASS] 取消令牌下发布无崩溃");

        _eventSystem.UnsubscribeAll<TestEventB>();
    }
}
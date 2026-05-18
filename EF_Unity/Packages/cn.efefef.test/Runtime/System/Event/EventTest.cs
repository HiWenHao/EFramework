/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-05-18 14:30:12
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-18 14:30:12
 * ScriptVersion: 0.1
 * ===============================================
 */

using UnityEngine;
using EasyFramework.Managers.Event;
using Cysharp.Threading.Tasks;
using System;

namespace EFExample
{
    public class EventTest : MonoBehaviour
    {
        // 定义几个测试用的事件结构体
        private struct TestEventA
        {
        }

        private struct TestEventB
        {
            public string Message;
        }

        private struct TestEventC
        {
            public int Value;
        }

        private IDisposable _tokenA;
        private IDisposable _tokenB;
        private IDisposable _tokenC;
        private IDisposable _asyncToken;

        private void Start()
        {
            // 订阅同步事件 TestEventB
            _tokenB = EventManager.Instance.Subscribe<TestEventB>(OnTestEventB);

            // 订阅同步事件 TestEventC
            _tokenC = EventManager.Instance.Subscribe<TestEventC>(OnTestEventC);

            Debug.Log("[EventTest] 已订阅事件：TestEventA, TestEventB, TestEventC (同步) + TestEventA (异步)");
        }

        private void Update()
        {
            // 按键盘数字键发布不同事件，方便测试
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                if (null == _tokenA)
                {
                    // 订阅同步事件 TestEventA
                    _tokenA = EventManager.Instance.Subscribe<TestEventA>(OnTestEventA, group: "TestGroup");
                    // 订阅异步事件（示例）
                    _asyncToken = EventManager.Instance.Subscribe<TestEventA>(OnAsyncTestEventA);
                }
                else
                {
                    EventManager.Instance.Publish(new TestEventA());
                    Debug.Log("发布 TestEventA");
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                EventManager.Instance.Publish(new TestEventB { Message = "Hello from test" });
                Debug.Log("发布 TestEventB");
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                EventManager.Instance.Publish(new TestEventC { Value = 42 });
                Debug.Log("发布 TestEventC");
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                // 异步发布
                _ = EventManager.Instance.PublishAsync(new TestEventA());
                Debug.Log("异步发布 TestEventA (PublishAsync)");
            }
        }

        private void OnTestEventA(TestEventA data)
        {
            Debug.Log("收到 TestEventA (同步)");
        }

        private void OnTestEventB(TestEventB data)
        {
            Debug.Log($"收到 TestEventB: {data.Message}");
        }

        private void OnTestEventC(TestEventC data)
        {
            Debug.Log($"收到 TestEventC: Value={data.Value}");
        }

        private async UniTask OnAsyncTestEventA(TestEventA data)
        {
            await UniTask.Delay(100);
            Debug.Log("收到 TestEventA (异步处理器)");
        }

        private void OnDestroy()
        {
            // 取消订阅（可选，EventManager内部会自动处理对象销毁时的令牌？但最好手动）
            _tokenA?.Dispose();
            _tokenB?.Dispose();
            _tokenC?.Dispose();
            _asyncToken?.Dispose();
        }
    }
}
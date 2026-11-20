# EF.Net

长连接模块：UDP + KCP 客户端，含会话管理、消息分发、RPC 与序列化扩展点。

## 门面

`EasyFramework.Managers.Net.NetworkManager`（`[Manager]`，`MonoSingleton`）

```csharp
NetworkManager.Instance.RegisterHandler(opcode, packet => { /* ... */ });
NetworkManager.Instance.Send(sessionId, opcode, payload);
TResponse res = await NetworkManager.Instance.Call<TResponse>(sessionId, opcode, payload);
NetworkManager.Instance.CloseSession(sessionId);
```

- `Dispatcher`（`MessageDispatcher`）注册 Handler、设置 `IMessageSerializer`。
- 会话：`Sessions` / `Connections` / `TryGetSession`；`SetToken` 供握手鉴权票据。
- 协议与帧格式见 `MessagePipe` / `NetOpcode`；KCP 实现见 `Kcp`。
- 诊断当前经 `EventManager` 事件发布（NetErrorEvent 等），`OpenDebug` 为预留开关。

依赖：`cn.efefef.core`。配套服务端见独立仓库 `EFramework_Server`。

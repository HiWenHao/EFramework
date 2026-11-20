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


## 安装

在项目的 `Packages/manifest.json` 添加依赖。日常开发用 Gitee 镜像（国内快，跟随最新）：

```json
"cn.efefef.net": "https://gitee.com/AlvinCN/EFramework.git?path=EF_Unity/Packages/cn.efefef.net"
```

需要锁定版本时改用 GitHub 源库 + CD 发布的 tag（Gitee 是镜像，没有 tag）：

```json
"cn.efefef.net": "https://github.com/HiWenHao/EFramework.git?path=EF_Unity/Packages/cn.efefef.net&ref=cn.efefef.net-v0.0.2"
```

依赖：`cn.efefef.core`。配套服务端见独立仓库 `EFramework_Server`。

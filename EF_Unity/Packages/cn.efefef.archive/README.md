# EF.Archive

游戏存档系统：多槽位管理、可替换存储后端、脏标记自动保存、完整性校验。

## 门面

`EasyFramework.Systems.Archive.ArchiveManager`（`[Manager]`，`MonoSingleton`）

```csharp
await ArchiveManager.Instance.SaveAsync("gold", 1234);
int gold = await ArchiveManager.Instance.LoadAsync<int>("gold");
await ArchiveManager.Instance.SaveToSlotAsync(slotId, "progress", data);
await ArchiveManager.Instance.FlushAsync();       // 落地所有脏数据
```

- 槽位：`CreateSlotAsync` / `DeleteSlotAsync` / `GetAllSlotsAsync`；`ActiveSlot` 为默认槽。
- 读取兜底：`LoadOrDefaultAsync<T>` / `LoadOrCreateAsync<T>`。
- 存储后端经 `SetProvider(IArchiveProvider)` 注入，默认走文件系统（见 `FileArchiveProvider`）。
- `OpenDebug` 控制诊断日志；错误日志始终输出。


## 安装

在项目的 `Packages/manifest.json` 添加依赖。日常开发用 Gitee 镜像（国内快，跟随最新）：

```json
"cn.efefef.archive": "https://gitee.com/AlvinCN/EFramework.git?path=EF_Unity/Packages/cn.efefef.archive"
```

需要锁定版本时改用 GitHub 源库 + CD 发布的 tag（Gitee 是镜像，没有 tag）：

```json
"cn.efefef.archive": "https://github.com/HiWenHao/EFramework.git?path=EF_Unity/Packages/cn.efefef.archive&ref=cn.efefef.archive-v0.1.0"
```

依赖：`cn.efefef.core`。

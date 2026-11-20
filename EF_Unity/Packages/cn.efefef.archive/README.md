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

依赖：`cn.efefef.core`。

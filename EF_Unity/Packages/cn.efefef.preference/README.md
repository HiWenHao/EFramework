# EF.Preference

玩家偏好管理：存储可替换、类型化读写、键级变更通知、生命周期自动落盘。

## 门面

`EasyFramework.Managers.Preference.PreferenceManager`（`[Manager]`，`MonoSingleton`）

```csharp
PreferenceManager.Instance.Set("audio.master", 0.8f);
float vol = PreferenceManager.Instance.Get<float>("audio.master", 1f);
PreferenceManager.Instance.Bind("audio.master", v => Apply(v));   // 键级变更通知
PreferenceManager.Instance.Flush();                                // 立即落盘
```

- 存储：默认 `PlayerPrefStorage`；可注入 `JsonFileStorage`（`SetStorage(IKeyValueStorage)`）。
- 加密：`SetCrypto(IPreferenceCrypto)`，包内 `XorPreferenceCrypto` 仅防明文、非安全加密。
- 单键单类型约定；跨类型读写会告警并拒绝/回退。`OpenDebug` 控制这些提示。
- 生命周期：`IOnAppQuit` / `Quit` 兜底落盘。

依赖：`cn.efefef.core`。

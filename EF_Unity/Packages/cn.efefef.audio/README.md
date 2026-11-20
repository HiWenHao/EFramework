# EF.Audio

基于混音器（AudioMixer）的音频播放系统，支持 BGM / 音效、URL 加载、音量与静音持久化。

## 门面

`EasyFramework.Managers.Audio.AudioManager`（`[Manager]`，`MonoSingleton`）

```csharp
AudioManager.Instance.PlayBGM("bgm_lobby");
await AudioManager.Instance.Play2DEffect("click");   // 返回 effect id，可用于 StopEffect
AudioManager.Instance.SetMasterVolume(0.8f);
```

- 播放：`PlayBGM` / `PlayBGMAsync` / `PlayBGMFromUrl`；音效 `Play2DEffect` / `Play3DEffect` /
  `PlayLoopEffect`（均返回 id / source，配合 `StopEffect` / `PauseEffect` 管理）。
- 默认音量经 `cn.efefef.preference` 持久化（退出时落盘），运行时字段缓存避免每次读偏好。
- 四类服务可单独替换：`SetPlayer(IAudioPlayer)` / `SetClipLoader(IAudioClipLoader)` /
  `SetClipCache(IAudioClipCache)` / `SetSettings(IAudioSettings)`；`Configure(AudioServices)` 一次注入全部。
- `OpenDebug` 控制警告日志。

依赖：`cn.efefef.core`、`cn.efefef.preference`。

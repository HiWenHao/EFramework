# EF.YooAsset

统一管理 YooAsset 的版本与资源加载：分包注册、版本检查、下载、版本持久化。

## 门面

`EasyFramework.Managers.YooAsset.PatchManager`（`[Manager]`，`MonoSingleton`）负责更新；
`YooAssetsSystem`（`IAssetsSystem` 实现）承担运行时资源加载，与 `AssetsManager` 门面对接。

```csharp
PatchManager.Instance.RegisterSubPackages(configs);
bool need = await PatchManager.Instance.CheckAllPackagesForUpdate(mode);
bool ok = await PatchManager.Instance.DownloadEssentialPackages(mode, tags, onProgress);
```

- 更新：`CheckForUpdatePatches` / `StartUpdatePatches` / `CreateDownloader` / `StartDownloader`。
- 版本持久化：`LoadLocalVersion` / `SaveLocalVersion` / `ClearLocalVersion`。
- 运行模式 `EPlayMode`（HostPlay / Offline / WebPlay 等）见 YooAsset。
- `OpenDebug` 控制日志；`YooAssetsSystem.OpenDebug` 独立控制加载侧日志。


## 安装

在项目的 `Packages/manifest.json` 添加依赖。日常开发用 Gitee 镜像（国内快，跟随最新）：

```json
"cn.efefef.yooasset": "https://gitee.com/AlvinCN/EFramework.git?path=EF_Unity/Packages/cn.efefef.yooasset"
```

需要锁定版本时改用 GitHub 源库 + CD 发布的 tag（Gitee 是镜像，没有 tag）：

```json
"cn.efefef.yooasset": "https://github.com/HiWenHao/EFramework.git?path=EF_Unity/Packages/cn.efefef.yooasset&ref=cn.efefef.yooasset-v0.0.1"
```

依赖：`cn.efefef.core`、`com.tuyoogame.yooasset`。

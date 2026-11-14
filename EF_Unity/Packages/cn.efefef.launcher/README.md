# EF.Launcher

Unity 全量热更的 **AOT 启动内核**。负责：YooAsset 初始化 → 版本检查 → 下载 → 补 AOT 元数据 → 加载热更程序集 → 调热更入口。

---

## 一、铁律（违反必炸）

**本包（`EF.Launcher.Runtime`）不得引用任何 `cn.efefef.*` 包。**

原因：HybridCLR 在打包时会把热更程序集从包体里剔除（`FilterHotFixAssemblies`），
所以 AOT 程序集一旦引用了热更程序集，il2cpp 阶段就会断。EF.Core / EF.UI / EF.YooAsset
都会进热更层，因此启动段必须是零 EF 依赖的。

`EF.Launcher.Runtime.asmdef` 只允许引用：`YooAsset`、`HybridCLR.Runtime`。

---

## 二、启动顺序

| # | 状态 | 做的事 |
|---|---|---|
| 1 | `Booting` | `ILauncherHook.OnBoot`：隐私协议 / 渠道 SDK / 登录 |
| 2 | `Initializing` | `YooAssets.Initialize()` → `CreatePackage` → `InitializeAsync` |
| 3 | `CheckingVersion` | `RequestPackageVersionAsync` → `UpdatePackageManifestAsync` |
| 4 | `Downloading` | `CreateResourceDownloader` → `BeginDownload`（轮询进度） |
| 5 | `LoadingMetadata` | `RuntimeApi.LoadMetadataForAOTAssembly(..., SuperSet)` |
| 6 | `LoadingHotfix` | 按配置顺序 `Assembly.Load` |
| 7 | `Invoking` | 反射调用 `EntryType.EntryMethod()` |
| 8 | `Completed` | 交棒热更层 |

**顺序不能换**：AOT 元数据必须在热更程序集之前加载，否则热更代码里出现 AOT 泛型实例化会踩空。

---

## 三、接入步骤

1. 场景里建一个空物体，挂 `Launcher`（菜单 `EF/Launcher` 添加组件）。
2. `Assets → Create → EF → Launcher Config` 生成配置，拖到 `Launcher.Config`。
3. 在同一个物体（或子物体）上挂一个 `ILauncherView` 实现——参考 `Samples~/BootScene`
   （Package Manager 里点 Import 导入）。
4. 填配置：
   - `PlayMode`：编辑器用 `EditorSimulateMode`，出包用 `HostPlayMode`
   - `PackageName`：与资源构建配置一致
   - `MainUrl` / `FallbackUrl`
   - `AotMetadataDlls`：随包的裁剪后 AOT dll（与 `HybridCLRSettings.patchAOTAssemblies` 对齐）
   - `HotfixDlls`：**拓扑序**，被依赖的排前面（`EF.Core` 必须在 `EF.UI` 之前）
   - `EntryType` / `EntryMethod`：热更入口，例如 `EFExample.HotfixTest` / `Init`
5. `HybridCLRSettings` 里确认 `EF.Launcher.Runtime` / `EF.Launcher.Editor` **不在**热更列表。

---

## 四、界面为什么必须自己写

`EF.UI` 在热更层，启动时它还不存在，用它必然 Missing Script。
能用的只有 AOT 侧的 **UGUI / TextMeshPro**（`Launcher` 本身不依赖任何 UI 包，只暴露 `ILauncherView`）。

---

## 五、AOT 边界怎么守（已内置闸门）

`Editor/LauncherBoundaryValidator.cs` 会在三个时机检查：

| 时机 | 行为 |
|---|---|
| 编辑器每次重编译（`[InitializeOnLoadMethod]` + `delayCall`） | 违规 → Console 报 Error |
| 打包前（`IPreprocessBuildWithReport`） | 违规 → `BuildFailedException`，直接中止出包 |
| 手动 / CI（`EF/Launcher/校验 AOT 边界` 或 `-executeMethod`） | 违规 → 返回 false / 退出码 1 |

检查四项：

1. **直接引用**：`EF.Launcher.Runtime.asmdef` 的 `references` / `precompiledReferences` 里不能有 EF 程序集
2. **传递引用**：BFS 走完引用闭包，`Launcher → A → EF.Core` 这种也会被揪出来（Unity 会把闭包一起塞进编译引用）
3. **自举**：Launcher 自己的 asmdef 不能出现在 `HybridCLRSettings` 的 `hotUpdateAssemblyDefinitions` / `hotUpdateAssemblies` / `preserveHotUpdateAssemblies` 里
4. **GUID 兜底**：asmdef 按 GUID 引用时，解析到 `Packages/cn.efefef.*` 路径也算违规

CI 用法：

```bash
Unity -batchmode -quit -projectPath <proj> \
  -executeMethod EasyFramework.Launcher.Editor.LauncherBoundaryValidator.ValidateFromCommandLine \
  -logFile -
# 退出码 1 = 边界被破坏
```

> 为什么不用 Roslyn 分析器当主闸门：分析器只看 `Compilation.References`，和 asmdef 是同一份信息，
> 没有额外准确度，而且可以被关闭、也可能没随包安装。分析器适合做「编辑期红波浪线」，门禁必须在 Editor 回调里。

---

## 六、已知边界


- **不要用 `EFC.Projects` 取 CDN 地址**：那个 ScriptableObject 在热更层，启动时读不到。
  `LauncherConfig` 是唯一真源，热更层应该反过来读它。
- **`YooAssets.Initialize()` 是幂等的**：重复调用只会打一条 warning，不会崩。
  但热更层的 `PatchManager` 建议改成 `if (!YooAssets.Initialized)`，省掉噪音日志。
- 本包只管「把热更层拉起来」，不管分包下载、资源热更策略——那些属于热更层的 `EF.Patch`。

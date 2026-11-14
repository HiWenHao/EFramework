# EF.Launcher

Unity 全量热更的 **AOT 启动内核**。负责：YooAsset 初始化 → 版本检查 → 下载 → 补 AOT 元数据 → 加载热更程序集 → 调热更入口。

---

## 一、铁律（违反必炸）

**本包（`EF.Launcher.Runtime`）不得引用任何 `cn.efefef.*` 包。**

原因：HybridCLR 在打包时会把热更程序集从包体里剔除（`FilterHotFixAssemblies`），
所以 AOT 程序集一旦引用了热更程序集，il2cpp 阶段就会断。EF.Core / EF.UI / EF.YooAsset
都会进热更层，因此启动段必须是零 EF 依赖的。

`EF.Launcher.Runtime.asmdef` 只允许引用：`YooAsset`、`HybridCLR.Runtime`。

> **这条铁律只管 Runtime。** `EF.Launcher.Editor` 是编辑器程序集（不进包体），它引用了
> `EF.Editor`（为了复用 `LC.Tr` 双语与 `MenuItemToolkit` 菜单规范）和 `HybridCLR.Editor`，
> 不影响 AOT 边界。package.json 里也因此声明了 `cn.efefef.core` 依赖。

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
| 手动 / CI（`EFTools/🎈 Utility/🔍 Validate AOT Boundary` 或 `-executeMethod`） | 违规 → 返回 false / 退出码 1 |

检查四项：

1. **直接引用**：`EF.Launcher.Runtime.asmdef` 的 `references` / `precompiledReferences` 里不能有 EF 程序集
2. **传递引用**：BFS 走完引用闭包，`Launcher → A → EF.Core` 这种也会被揪出来（Unity 会把闭包一起塞进编译引用）
3. **自举**：Launcher 自己的 asmdef 不能出现在 `HybridCLRSettings` 的 `hotUpdateAssemblyDefinitions` / `hotUpdateAssemblies` / `preserveHotUpdateAssemblies` 里
4. **GUID 兜底**：asmdef 按 GUID 引用时，解析到 `Packages/cn.efefef.*` 路径也算违规

**白名单**：`EasyFrameworkTools.dll`（`cn.efefef.core/Plugins/`）名字像 EF，但只依赖 BCL + UnityEngine
（里面是 `D` 日志门面与 `TStatic` 字符串工具），已在 `AllowedPluginDlls` 里放行，可以放心被 launcher 引用。

**盲区**：Plugins 下的 DLL 默认 `Auto Reference`，不会写进 asmdef，校验器只查显式声明的 `precompiledReferences`。
如果将来新增一个**引用了 EF 程序集**的插件 DLL，这条闸门是看不见的 —— 需要人工确认，或把该 DLL 改成
`overrideReferences: true` + 显式 `precompiledReferences`，让它进入检查范围。

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

## 六、一键切换热更列表

菜单 `EFTools/🎈 Utility/`（平铺，最多三层）：

| 菜单项 | 作用 |
|---|---|
| **🔍 Validate AOT Boundary** | 手动跑一次边界校验，结果打到 Console |
| **➕ Add All EF Assemblies To Hot Update** | 把 10 个 EF 运行时 asmdef 追加进 `HybridCLRSettings.hotUpdateAssemblyDefinitions`（已存在的跳过，不覆盖原有条目） |
| **🗑️ Remove All EF Assemblies From Hot Update** | 把它们移出列表，回到 Classic 形态 |

菜单名保持英文：`[MenuItem]` 的路径必须是编译期常量，没法跟着语言设置动态切换
（EF 自己的菜单也是英文）。**对话框、日志、异常信息全部走 `LC.Tr(zh, en)`**，跟随
`ProjectConfig.DisplayLanguage` / 系统语言切换。

**加入前会先扫一遍「还有哪些 AOT 程序集在引用 EF」**，并在确认框里直接列出来 ——
那些引用会让打包当场失败（il2cpp 找不到被剔除的程序集）。看到警告就先去改 AOT 侧，别硬点。

自动排除的：`EF.Launcher.*`、所有 `*.Editor`、`/Editor/` 路径、`/Plugins/` 下的 UniTask。

加入后仍需手动两步：

1. 执行 `HybridCLR/Generate/All`
2. 把 `HybridCLRData/HotUpdateDlls/<target>/` 里的 dll 按拓扑序填进 `LauncherConfig.HotfixDlls`

> 这个工具只改配置，不改代码。`ExampleGame` 对 EF 的引用、`EF.cs` 门面、启动场景，都得按
> 《EF_Launcher_设计方案.md》第七节那 7 项改造自己动手。

---

## 七、已知边界


- **不要用 `EFC.Projects` 取 CDN 地址**：那个 ScriptableObject 在热更层，启动时读不到。
  `LauncherConfig` 是唯一真源，热更层应该反过来读它。
- **`YooAssets.Initialize()` 是幂等的**：重复调用只会打一条 warning，不会崩。
  但热更层的 `PatchManager` 建议改成 `if (!YooAssets.Initialized)`，省掉噪音日志。
- 本包只管「把热更层拉起来」，不管分包下载、资源热更策略——那些属于热更层的 `EF.Patch`。

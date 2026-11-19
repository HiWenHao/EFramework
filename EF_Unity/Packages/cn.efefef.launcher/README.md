# EF.Launcher

Unity 热更的 **AOT 启动内核**（当前形态：**Core 留 AOT**）。负责：YooAsset 初始化 → 版本检查 → 下载 → 补 AOT 元数据 → 加载热更程序集 → 调热更入口。

---

## 一、形态与铁律（违反必炸）

| 程序集 | 形态 |
|---|---|
| `EF.Core`（含 `EFC` / `ISingleton` / `ProjectConfig` / UniTask） | **AOT**，不进热更列表 |
| 其余 9 个 EF 运行时程序集 + `ExampleGameHotfix` | 热更 |
| `EF.Launcher` | AOT 启动内核，永远不进热更列表 |

热更程序集**单向引用** AOT 程序集是 HybridCLR 的标准形态；反向引用（AOT → 热更）会在 il2cpp 阶段断。
`EF.Core` 留在 AOT 还带来三个好处：`ProjectConfig.asset` 可正常反序列化、UniTask 的泛型实例化本就在 AOT 侧、
Core 内 4 个 Mono 单例与 3 个 Inspector 不需要迁移。

**铁律：`EF.Launcher` 不得引用任何会进入热更列表的程序集。**
`EF.Launcher.asmdef` 允许引用 `YooAsset`、`HybridCLR.Runtime`、`UniTask`，以及**唯一白名单** `EF.Core`（常驻 AOT 的框架底座）。
`EF.Core.asmdef` 只依赖 `UniTask` 与 `YooAsset`，引用闭包里没有任何热更代码，因此不会触发剔除断链。
`UniTask` 是纯 UPM 包、恒定 AOT 常驻，名字不含 `EF.`，边界门禁按名字自动放行。

> **注意「EF 包目录」≠「EF 程序集」。** UniTask 全家被 vendor 在 `cn.efefef.core/Plugins/Unitask/` 下，
> 路径属于 EF 包，程序集名却是 `UniTask`，且随 Core 一起常驻 AOT。边界判定按**热更列表 + 程序集名**，
> 不按 `Packages/cn.efefef.*` 路径前缀，否则 `EF.Launcher → EF.Core → UniTask` 会被误报成越界。

> **白名单不代表可以随便用 Core。** Core 里的 `[Manager]` 单例按需注册，真正的资源系统实现由热更层注入；
> 热更 DLL 加载前访问它们只会拿到未注入的默认实现。启动段**只允许读 `EFC.Projects` 这类静态配置**
> （语言、资源地址都在里面），其余 `EFC.*` 一律由门禁拦下。
> **启动流程参数**（包名 / 运行模式 / 重试 / 热更 dll 列表）仍以 `LauncherConfig` 为唯一真源。
> 这条由 `LauncherBoundaryValidator` 的源码静态检查强制保证。

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

**热更入口的返回值**：入口必须是**无参 static** 方法（同名重载会被显式按 `Type.EmptyTypes` 解析，不会报歧义）。
如果它返回 `IEnumerator` 或 `Task`，启动器会**等它跑完**才广播 `Completed`；返回其它类型（例如 `UniTask`）
启动器无法等待，会打一条 Warning 并立刻标完成——需要「初始化完成后才算启动完成」时，请返回前两种之一。

**运行约束**

- `AotMetadataDlls` 里出现引用丢失（`.dll.bytes` 重新导入后 `.meta` guid 变化）会**直接失败**并点名数量，
  不再静默跳过——补元数据没生效是隐形故障，只会在热更代码踩到泛型时以难定位的方式崩。
  重新 `HybridCLR/Generate/All` 之后跑一次 `🔄 Sync Hotfix Dll Entries` 即可按 `patchAOTAssemblies` 重绑；
  有缺失时它**整体不写**，避免把配置改成必然启动失败的形态。
- 资源句柄无论加载成功失败都会 `Release()`。
- 已加载过的热更程序集在重试时跳过：`Assembly.Load(byte[])` 每次都会产生新程序集，
  重复加载同名 dll 会分裂出第二份类型身份（典型症状 `InvalidCastException`）。
- 失败时 `Diagnose()` 一次打全现场：应用版本、状态、已加载热更程序集数、重试次数、耗时、Core 是否就绪。
- `Retry()` 带次数上限与退避（`LauncherState.Retrying`），等待用
  `UniTask.Delay(..., DelayType.Realtime)`，不受 `Time.timeScale` 影响；
  `MaxRetryCount` / `RetryDelaySeconds` 为 0 时走内置默认（3 次 / 2 秒）。
- **全流程基于 UniTask**（`async UniTask`，不再用 Unity 协程）：等待包操作/资源句柄直接
  `await op.Task`（完成即续，零帧延迟），逐帧循环用 `UniTask.NextFrame(token)`。
  取消令牌来自 `this.GetCancellationTokenOnDestroy()`——**GameObject 销毁后流程自动取消**，
  不会在半销毁的对象上继续跑；取消是静默的，不计入 `Failed`。
- **异常一律 `Fail(Unexpected, ...)`**：协程时代未捕获异常会被 Unity 吞掉、流程静默中断；
  改成 async 后由 `RunPipelineAsync` 统一捕获（`D.Exception` + `LauncherErrorCode.Unexpected`），
  `finally` 里复位 `_running`，重试按钮不会失效。
- **只保留必要日志，且全部双语**：警告 / 错误 / 失败诊断（`D.Warning` / `D.Error` / `D.Exception` / `Diagnose`）按
  `EFC.Projects.AutoLanguage / DisplayLanguage` 在 `L(zh, en)` 间择一输出（与 `PreferenceManager` 同口径）；
  生命周期 info 日志（状态流转、已加载程序集、重试倒计时等）已删除，不再需要逐条开关。
  ```csharp
  private static bool IsZh() { /* AutoLanguage 时跟随 Application.systemLanguage */ }
  private static string L(string zh, string en) => IsZh() ? zh : en;
  ```
- **日志开关两层，别搞混**：
  | 开关 | 作用范围 | 关掉后 |
  |---|---|---|
  | `D.Init(openLog)` | `D` 门面的**全局**总闸（本工程没调用过，走默认开启） | 全工程所有 `D.*` 静默 |
  | `EFC.EnableDebug` | ⚠️ 目前是**死属性**：声明了但没人读，不影响 `D` | — |

---

## 三、接入步骤

1. 场景里建一个空物体，挂 `Launcher`（菜单 `EF/Launcher` 添加组件）。
2. `Assets → Create → EF → Launcher Config` 生成配置，拖到 `Launcher.Config`。
3. 在同一个物体（或子物体）上挂一个 `ILauncherView` 实现——参考 `Samples~/BootScene`
   （Package Manager 里点 Import 导入）。
4. 填配置：
   - `PlayMode`：编辑器用 `EditorSimulateMode`，出包用 `HostPlayMode`
   - `PackageName`：与资源构建配置一致
   - `AotMetadataDlls`：随包的裁剪后 AOT dll（与 `HybridCLRSettings.patchAOTAssemblies` 对齐）
   - `HotfixDlls`：**拓扑序**，被依赖的排前面。Core-AOT 形态下 **列表里不应出现 `EF.Core`**，
     它已在包体里，再 `Assembly.Load` 一份会导致类型身份分裂
   - `EntryType` / `EntryMethod`：热更入口，例如 `EFExample.HotfixTest` / `Init`
5. **资源地址不在 `LauncherConfig` 里**：去「EF 配置面板 → 项目设置 → 出包资源存放地」填
   `ProjectConfig.ResourcesArea`（内网 / 备用）。它是全工程唯一的 CDN 配置，启动器与热更分包都读它。
6. `HybridCLRSettings` 里确认 `EF.Launcher` / `EF.Launcher.Editor` **不在**热更列表。

> 运行时 `AddComponent<Launcher>()` 后要立刻 `Configure(config)`；启动流程一旦开始就不再接受配置注入。

---

## 四、界面为什么必须自己写

`EF.UI` 在热更层，启动时它还不存在，用它必然 Missing Script。
能用的只有 AOT 侧的 **UGUI / TextMeshPro**（`Launcher` 本身不依赖任何 UI 包，只暴露 `ILauncherView`）。

**契约不传文案。** `Launcher` 是通用包，只下发结构化数据（状态 / 错误码 / 进度），界面文案由 `ILauncherView` 实现方自行解析：

```csharp
void OnStateChanged(LauncherState state);                    // 按 state 查表显示文案
void OnProgress(long current, long total, long curBytes, long totalBytes);
void OnError(LauncherErrorCode code, Action retry);          // 按 code 查表显示文案
```

`LauncherState` / `LauncherErrorCode` 的每个值都带中英双语注释，可直接作为文案表的基准。
诊断详情（异常信息、资源名等）只写日志，不下发给界面——避免把内部实现暴露给玩家。
`Samples~/BootScene/SampleLauncherView.cs` 里有一份现成的双语解析表可复制。

**启动界面的收场**：`LauncherState.Completed` 就是收尾信号，收到后把 Canvas 关掉/淡出即可
（`Samples~/BootScene` 里演示了这个做法）。注意别 `SetActive(false)` 整个物体——`Launcher`
可能挂在同一个物体上；流程本身不会因此中断（UniTask 不受激活状态影响，只有销毁才取消），
但界面会一起失活，后续状态就没人显示了。

**没有 SO 事件通道。** 启动是一次性流程，状态 / 进度 / 失败只走 `ILauncherView`，不再广播给额外的
ScriptableObject 通道——少一层间接，也少一处订阅泄漏风险。需要跨系统监听启动时，直接实现
`ILauncherView` 并由 `Launcher` 的 Inspector 注入（或留空自动查找）即可。

---

## 五、AOT 边界怎么守（已内置闸门）

`Editor/LauncherBoundaryValidator.cs` 会在三个时机检查：

| 时机 | 行为 |
|---|---|
| 编辑器每次重编译（`[InitializeOnLoadMethod]` + `delayCall`） | 违规 → Console 报 Error |
| 打包前（`IPreprocessBuildWithReport`） | 违规 → `BuildFailedException`，直接中止出包；同时拦下仍是 `EditorSimulateMode` 的启动配置 |
| 手动 / CI（`EFTools/🎈 Utility/🔍 Validate AOT Boundary` 或 `-executeMethod`） | 违规 → 返回 false / 退出码 1 |

检查七项：

1. **直接引用**：`EF.Launcher.asmdef` 的 `references` / `precompiledReferences` 里不能有热更程序集
2. **传递引用**：BFS 走完引用闭包，`Launcher → A → 热更程序集` 这种也会被揪出来（Unity 会把闭包一起塞进编译引用）
3. **自举**：Launcher 自己的 asmdef 不能出现在 `HybridCLRSettings` 的 `hotUpdateAssemblyDefinitions` / `hotUpdateAssemblies` / `preserveHotUpdateAssemblies` 里
4. **真源驱动**：违规判定以 `HybridCLRSettings` 的 `hotUpdateAssemblyDefinitions`（GUID → 路径）、`hotUpdateAssemblies` / `preserveHotUpdateAssemblies`（名字）为唯一真源，按路径 / 名字 / GUID 三重匹配；**不再按 `Packages/cn.efefef.*` 路径一刀切**
5. **白名单前提**：`EF.Core` 必须真的不在热更列表里；被误加进去时门禁报错，避免放行变成断链
6. **源码收口**：`Runtime/` 下（**含子目录**）只允许出现 `EFC.Projects`（纯静态配置），其余 `EFC.*` 都属于管理器单例，会被拦下；检查前会先剥掉 `//` 与 `/* */` 注释，注释里写关键字不算违规
7. **出包模式**：构建场景真正依赖到的 `LauncherConfig` 不能停在 `EditorSimulateMode`（该分支在包体里被 `#if UNITY_EDITOR` 剔除，
   出包后 `CreateInitializeParameters` 会掉进 `default` 直接抛）。只查场景依赖，工程里留一份没被引用的调试配置不会拦住出包

`HotUpdateIndex.Build()` 按 YAML 段解析设置文件；若 `hotUpdateAssemblyDefinitions` 里出现无法解析的列表项
（HybridCLR 改了格式），会打 Warning 提示门禁可能失效，不会静默放行。

**白名单**：`EasyFrameworkTools.dll`（`cn.efefef.core/Plugins/`）名字像 EF，但只依赖 BCL + UnityEngine
（里面是 `D` 日志门面与 `TStatic` 字符串工具），已在 `AllowedPluginDlls` 里放行，可以放心被 launcher 引用。
另有一个**精确白名单** `EF.Core`，由 `AllowedAotBasePath` / `AllowedAotBaseName` 两个常量做路径 + 名字双重匹配，
不是放开整个 `cn.efefef.*`。其余 EF 自有程序集按**程序集名**（`EF.` 前缀）识别，一律拒绝；
`UniTask` / `YooAsset` / `HybridCLR.Runtime` 这类名字不含 `EF.` 的 AOT 第三方按名字放行。

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
| **➕ Add All EF Assemblies To Hot Update** | 把 EF 运行时 asmdef 追加进 `HybridCLRSettings.hotUpdateAssemblyDefinitions`（已存在的跳过） |
| **🗑️ Remove All EF Assemblies From Hot Update** | 把它们移出列表，回到 Classic 形态 |
| **🔄 Sync Hotfix Dll Entries** | 一条管线做四件事：补缺失条目 → 绑定 `Location` / `FallbackAsset` → 按 `patchAOTAssemblies` 重绑 `AotMetadataDlls` → 按 asmdef 依赖图拓扑排序；对话框会念出最终加载顺序，并点名匹配不到 asmdef 的旧条目与缺失的 AOT 元数据 |

> 这三件事本来就是同一条管线（补 → 绑 → 排），`LauncherHotfixEntryBinder.Build` 一次做完；
> 原先拆成 `Sort / Fill / Auto Bind` 三个菜单，其中 `Auto Bind` 与 `Fill` 调的是同一个方法，属于重复入口，已合并。
> 只想**预览不写回**时用「EF 配置面板 → 热更程序集」里的 `🔍 扫描预览`。

**Core-AOT 形态下，`CollectEfRuntimeAsmdefs` 默认排除 `EF.Core`**：加入列表与填充 `HotfixDlls` 都只会得到
9 个 EF 程序集（外加 `ExampleGameHotfix` 共 10 条）。要回到「含 Core 的全量热更」实验形态，
需显式传 `includeCore: true`。

菜单名保持英文：`[MenuItem]` 的路径必须是编译期常量，没法跟着语言设置动态切换
（EF 自己的菜单也是英文）。**对话框、日志、异常信息全部走 `LC.Tr(zh, en)`**，跟随
`ProjectConfig.DisplayLanguage` / 系统语言切换。

**加入前会先扫一遍「还有哪些 AOT 程序集在引用 EF」**，并在确认框里直接列出来 ——
那些引用会让打包当场失败（il2cpp 找不到被剔除的程序集）。看到警告就先去改 AOT 侧，别硬点。

自动排除的：`EF.Launcher.*`、**`EF.Core`（Core 留 AOT）**、所有 `*.Editor`、`/Editor/` 路径、`/Plugins/` 下的 UniTask。

加入后仍需手动两步：

1. 执行 `HybridCLR/Generate/All`
2. 把 dll 放进 YooAsset，并核对每个条目的 `Location`

> **顺序和骨架都不用手排。** 点 `➕ Add All EF Assemblies To Hot Update` 时会顺带跑一遍同步
> （按 asmdef 依赖图排好序，已有条目原样保留）。之后单独重跑用 `🔄 Sync Hotfix Dll Entries`。
>
> `Location` 由「EF 配置面板 → 热更程序集」里的规则生成（默认 `AddressByFileName`，取 `*.dll.bytes` 去后缀）。
> 匹配不到同名 asmdef 的条目会排到末尾，并在同步结果的对话框里点名（多用于发现改名或拼写错误）。

> 这个工具只改配置，不改代码。`ExampleGame` 对 EF 的引用、`EF.cs` 门面、启动场景，都得按
> 《EF_Launcher_设计方案.md》第七节那 7 项改造自己动手。

---

## 七、已知边界

- **日志统一走 `D`**（`EasyFrameworkTools` 的日志门面）：`D.Log` / `D.Warning` / `D.Error` / `D.Exception`，
  包内不允许出现 `UnityEngine.Debug.*`。`D` 的五个方法都只接受 `object`，需要拼参数请用字符串插值。
  编辑器侧同样适用——`D` 是 `Plugins` 下的自动引用 DLL，`EF.Launcher.Editor` 无需额外声明引用。
- **地址只有一个真源：`ProjectConfig.ResourcesArea`**（core，AOT 常驻，`EFC` 在 `AfterAssembliesLoaded` 阶段加载）。
  启动器直接读 `EFC.Projects`，热更层的 `PatchManager` 分包回落也读同一份。这样 `EF.YooAsset`
  不装 `EF.Launcher` 也能独立跑，也不存在两份 CDN 互相漂移。
  `LauncherConfig` 只管**启动流程参数**（包名 / 运行模式 / 重试 / 热更 dll 列表），不管服务器地址。
- **读 `EFC.Projects` 时务必判空**：资产缺失时它是 `null`，`Launcher` 内部统一走
  `ProjectConfigOrNull` / `ProjectMainUrl` 等私有属性兜空，不要在业务代码里直接链式解引用。
- **重试字段的默认值**：`MaxRetryCount` / `RetryDelaySeconds` 为 0 时走代码内置默认，
  避免旧 `LauncherConfig.asset` 新增序列化字段后被反序列化成 0；`AutoRetryOnFailure` 默认关闭，由界面按钮触发。
- **`YooAssets.Initialize()` 是幂等的**：重复调用只会打一条 warning，不会崩。
  但热更层的 `PatchManager` 建议改成 `if (!YooAssets.Initialized)`，省掉噪音日志。
- **`EFC.Initialize()` 的时序**：Core 留 AOT 后 `[RuntimeInitializeOnLoadMethod(AfterAssembliesLoaded)]` 会生效，
  它早于 Launcher 的 `Assembly.Load`。若 `EFC` 在初始化阶段按依赖链创建热更层单例，此时类型还没加载。
  需把「容器初始化」与「单例解析」拆成两段，后者延后到热更入口。
- 本包只管「把热更层拉起来」，不管分包下载、资源热更策略——那些属于热更层的 `EF.Patch`。

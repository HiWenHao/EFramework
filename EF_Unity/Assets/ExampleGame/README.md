# ExampleGame — EF 框架能力展示工程

这个工程是 EFramework 的**能力展示实例**：用尽可能少的代码，演示框架各模块「解决什么问题、怎么调用」。

融合后启动链收口为**单一入口场景** `Scenes/Launcher.unity`（Build Settings 索引 0），启动更新 → 补 AOT 元数据 → 加载热更程序集 → 调用 `EFExample.HotfixTest.Init()` → `UiSystem.OpenView<UiAView>()`。

> 工程里带 `_Test`、`CahtView`、`Haoheng` 音效、`127.0.0.1` 地址等内容都是演示数据，不代表生产配置。

---

## 快速索引：模块 × 演示状态

框架共 13 个包，按功能分四组。**演示状态**一栏如实标注，避免「以为有演示其实没有」。

| 分组 | 模块（程序集） | 解决什么问题 | 演示状态 |
|---|---|---|---|
| 核心底座 | **core** `EF.Core` | 单例容器、日志门面 `D`、配置、对象池/事件/时间/场景/流程管理器、编辑器工具链 | ✅ 充分 |
| 核心底座 | **launcher** `EF.Launcher` | HybridCLR 热更启动内核：YooAsset 初始化 → 版本检查 → 下载 → 补 AOT 元数据 → 加载热更 DLL → 调入口 | ✅ 场景即演示 |
| 核心底座 | **yooasset** `EF.YooAsset` | 资源地址加载、`PatchManager` 补丁更新、分包/按需下载 | ⚠️ 仅门面 |
| 表现层 | **ui** `EF.UI` | UI 生命周期、层级、传参、Overlay、动画、常用控件（ImagePro/ScrollRectPro/RadarMap…） | ✅ 充分 |
| 表现层 | **audio** `EF.Audio` | 2D/3D 音效、BGM、混音器、可替换加载器与播放器 | ⚠️ 仅 1 行 |
| 表现层 | **reddot** `EF.RedDot` | 树形红点、数值累加、脏标记增量刷新、批量合并、多种渲染器 | ❌ 无演示 |
| 表现层 | **localization** `EF.Localization` | 多语言文本、key 检索、`LocalizedText` 组件、运行时切语言 | ❌ 无演示 |
| 数据服务 | **archive** `EF.Archive` | 多槽位存档、AES 加密、SHA256 完整性校验、自动落盘 | ❌ 无演示 |
| 数据服务 | **http** `EF.Http` | HTTP 请求门面：可替换发送/缓存/重试/设置，支持 GET/POST/上传/下载 | ❌ 无演示 |
| 数据服务 | **net** `EF.Net` | 长连接（KCP/UDP）、会话管理、消息分发、请求-响应 Call | ❌ 无演示 |
| 数据服务 | **preference** `EF.Preference` | 玩家偏好：类型化读写、键级变更通知、可替换存储与加密、生命周期落盘 | ❌ 无演示 |
| 数据服务 | **datable** | Luban 配置表工作流（Excel → 生成代码），`JsonConfig/` 下即生成产物 | ⚠️ 仅代码 |
| 工程工具 | **packages** | 包资源同步、本地包服务、配置面板等编辑器工具 | ➖ 编辑器向 |

> 图例：✅ 有可运行演示 · ⚠️ 有残留/门面引用但未构成演示 · ❌ 框架有实现、本工程未提供演示 · ➖ 纯编辑器工具

---

## 一、核心底座

### core `EF.Core` — 框架内核（常驻 AOT）

**门面演示**：[`ScriptsHotfix/EF.cs`](ScriptsHotfix/EF.cs) 是全框架管理器的统一访问入口，注释里保留了外部包访问器模板——按项目实际安装的包取消注释即可。

```csharp
EF.Pool      // 对象池      EF.Events    // 系统事件
EF.Timer     // 时间管理    EF.Scenes    // 场景管理
EF.Tool      // 通用工具    EF.Procedure // 流程系统
EF.Assets    // 资源加载    EF.UI / EF.Audio / EF.Patch

EF.ClearMemory();  // GC + 卸载未使用资源
EF.QuitGame();     // 清内存 → 关管理器 → 退出
```

**其他演示**：[`FPSOnGUI.cs`](ScriptsHotfix/FPSOnGUI.cs)（帧率显示）、[`EventSystemTest.cs`](ScriptsHotfix/EventSystemTest.cs)（事件系统）。

**包内实现**：`MonoSingleton` / `ISingleton` / `ProjectConfig` / `D` 日志门面 / 编辑器菜单工具链（`EFTools/...`）。

### launcher `EF.Launcher` — 热更启动内核（常驻 AOT）

**整个启动场景就是演示**：[`Scenes/Launcher.unity`](Scenes/Launcher.unity) 上 `GameLauncher` 挂 `Launcher` 组件，配置引用 [`FillLauncherConfig.asset`](Resources/FillLauncherConfig.asset)。

- 更新进度 UI：[`LaunchViewTips.cs`](Scripts/LaunchViewTips.cs)
- 权限/隐私 Hook：[`PermissionShow.cs`](Scripts/PermissionShow.cs)（`ILauncherView` / `ILauncherHook` 实现）

启动 8 阶段：`Booting → Initializing → CheckingVersion → Downloading → LoadingMetadata → LoadingHotfix → Invoking → Completed`。顺序不能换——AOT 元数据必须先于热更程序集加载。

**关键铁律**：`EF.Launcher` 不得引用任何进入热更列表的程序集；`EF.Core` 常驻 AOT 是唯一白名单。包内 `LauncherBoundaryValidator` 在重编译、打包前、命令行三处强制检查。`EFTools/🎈 Utility/` 下提供一键切换热更列表、拓扑排序 HotfixDlls 等工具。

> 详细设计见 `Packages/cn.efefef.launcher/README.md`。

### yooasset `EF.YooAsset` — 资源与补丁

⚠️ **演示状态**：仅 `EF.cs` 门面暴露 `EF.Patch`，[`PatchUpdater.cs`](ScriptsHotfix/_Test/UiView/PatchUpdater/PatchUpdater.cs) 有空壳 UI，无实际检查/下载流程演示。

```csharp
using EasyFramework.Managers.Assets;
using YooAsset; // EPlayMode

// 资源加载
YooAssetsSystem.Instance.Load<Sprite>("ui/icon");
await YooAssetsSystem.Instance.LoadAsync<GameObject>("prefabs/role");

// 热更检查与下载（通常交给 Launcher 自动跑，此处为手动调用示意）
await PatchManager.Instance.CheckForUpdatePatches(EPlayMode.HostPlayMode);
await PatchManager.Instance.StartUpdatePatches(...);
```

---

## 二、表现层

### ui `EF.UI` — UI 系统（演示最充分）

**演示位置**：[`ScriptsHotfix/_Test/UiView/`](ScriptsHotfix/_Test/UiView/) 与 [`UiViewLogic/`](ScriptsHotfix/_Test/UiViewLogic/)，共 12 组 View/Logic 配对。入口 [`HotfixTest.Init()`](ScriptsHotfix/HotfixTest.cs:51)。

覆盖的能力点：

| 能力 | 演示代码 |
|---|---|
| 打开/关闭/带参打开 | `UiAViewLogic` → `UiBViewLogic` → `UiCViewLogic` |
| 关闭并通知上一页传参 | `CloseViewAndNotify<UiBView, UiAView, int>(456456465)` |
| 层级（Top/Bottom/Overlay） | `TestTopView*`、`TestBottomView*`、`PopupView` |
| 覆盖层 + 自定义附加数据 | `TipsView` + `TipsViewExtraData` |
| Query 链跳转 | `LoginViewLogic → CahtView → ChatMessageView → BottomSubfieldView` |
| 定时事件 | `UiCViewLogic` 的 `TimerEvent` |

```csharp
await UiSystem.Instance.OpenView<UiAView>();
await UiSystem.Instance.OpenView<UiBView, string>("向B传递参数");
await UiSystem.Instance.OpenViewOverlay<TipsView, string, TipsViewExtraData>(...);
await UiSystem.Instance.CloseViewAndNotify<UiCView, UiBView, string>("回传参数");
await UiSystem.Instance.CloseAllView();
```

包内还提供 `ImagePro` / `ScrollRectPro` / `RadarMap` / `Slideshow` / `ButtonPro` / `UiBinding` 等控件，本工程未逐一演示。

### audio `EF.Audio` — 音频

⚠️ **演示状态**：[`HotfixTest.cs:76`](ScriptsHotfix/HotfixTest.cs:76) 仅一行 2D 音效；[`UiBViewLogic`](ScriptsHotfix/_Test/UiViewLogic/UiBView/UiBViewLogic.cs) 里 2D/3D 调用是注释状态。

```csharp
using EasyFramework.Managers.Audio;

await AudioManager.Instance.Play2DEffect("Haoheng");
// Play3DEffect / 停止 / 混音器分组 等：包内已实现，本工程未演示
```

### reddot `EF.RedDot` — 红点系统

❌ **无演示**。示例用法（API 已核实）：

```csharp
using EasyFramework.Systems.RedDot;

var node = new RedDotNode("mail", RedDotDisplayType.Number);
node.OnValueChanged += n => Debug.Log($"红点变化: {n.Number}");
node.SetNumber(5);                    // 数值累加
node.SetParent(rootNode);             // 挂到红点树

RedDotSystem.Instance.BeginBatch();   // 批量变更
// ... 多个 SetNumber ...
RedDotSystem.Instance.EndBatch();     // 一次性刷新，避免逐条重绘
```

包内含 `DirtySystem`（脏标记增量刷新）、`BatchSystem`（批量合并）、`ImageRenderer` / `NumberRenderer` / `DotRenderer` 三种渲染器。

### localization `EF.Localization` — 多语言

❌ **无演示**。工程配置里已开启：[`ProjectConfig.asset`](Resources/Configs/ProjectConfig.asset) 的 `_autoLanguage: 1`、`_displayLanguage: 6`。

```csharp
using EasyFramework.Localization;

I18NManager.Instance.Get("ui.start");            // 取文本
I18NManager.Instance.GetFormat("ui.hi", name);   // 格式化
await I18NManager.Instance.SwitchLanguage("en"); // 运行时切换
```

UI 侧把 `TextMeshProUGUI` 换成 `LocalizedText` 组件，填 `Key` 即自动跟随语言；`SetFormatArgs(params object[])` 处理带参文本。

---

## 三、数据服务

### archive `EF.Archive` — 存档

❌ **无演示**。示例用法（API 已核实）：

```csharp
using EasyFramework.Systems.Archive;

var mgr = ArchiveManager.Instance;
int slot = await mgr.CreateSlotAsync("slot1");
await mgr.SaveToSlotAsync(slot, "gold", 1000);
int gold = await mgr.LoadOrDefaultFromSlotAsync<int>(slot, "gold", 0);
mgr.StartAutoSave();  // 生命周期自动落盘
```

支持多槽位、AES 加密、SHA256 完整性校验；`SetProvider(new FileArchiveProvider())` 可替换存储后端。

### http `EF.Http` — HTTP 请求

❌ **无演示**（`EF.cs` 里访问器仍是注释）。示例用法（API 已核实）：

```csharp
using EasyFramework.Systems.Http;

var http = HttpManager.Instance;
string text = await http.GetStringAsync(url);
T data = await http.GetAsync<T>(url);
await http.PostJsonAsync<TReq, TRes>(url, request);
await http.DownloadFileAsync(url, savePath);

http.SetRetryPolicy(...);  // 可替换重试策略 / 缓存 / 发送器 / 设置
```

> 工程里 [`FeishuNotifier.cs`](Scripts/FeishuNotifier.cs) 用原生 `UnityWebRequest` 发飞书通知——这是业务残留，不是 EF.Http 的演示。迁到 HttpManager 会是个好的补齐点。

### net `EF.Net` — 长连接

❌ **无演示**。示例用法（API 已核实）：

```csharp
using EasyFramework.Systems.Net;

var net = NetworkManager.Instance;
net.RegisterHandler(opcode, packet => { /* 处理推送 */ });
net.Send(sessionId, opcode, payload);
var res = await net.Call<TResponse>(sessionId, opcode, payload, timeoutSeconds: 10f);
net.CloseSession(sessionId, "bye");
```

包内提供 KCP / UDP 两种 Socket、消息分发器、会话管理。

### preference `EF.Preference` — 玩家偏好

❌ **无演示**。示例用法（API 已核实）：

```csharp
using EasyFramework.Preference;

var pref = PreferenceManager.Instance;
if (!pref.Exist("bgm_volume")) pref.Set("bgm_volume", 0.8f);
float vol = pref.Get<float>("bgm_volume", 1f);
pref.Flush();  // 主动落盘（也会在生命周期自动落盘）

pref.SetStorage(new JsonFileStorage());  // 可替换存储/加密实现
```

### datable — Luban 配置表

⚠️ **仅生成代码**。 [`ScriptsHotfix/JsonConfig/`](ScriptsHotfix/JsonConfig/) 下 `LC`、`TbItem`、`Item`、`EQuality` 等是 Luban 从 Excel 生成的产物；[`HotfixTest.cs:78-88`](ScriptsHotfix/HotfixTest.cs:78) 有一段被注释的加载示例（JSON / ByteBuf 两种 loader）。

### packages — 包资源工具

➖ 纯编辑器：配置面板、本地包服务、包资源同步。无运行时 API，不需要演示。

---

## 四、怎么给用户演示

建议的演示路线（一条龙，约 1 分钟）：

1. **打开工程** → Build Settings 只有 `Scenes/Launcher.unity` → 说明启动链已收口
2. **Play** → 看到更新进度条 → 对应 `launcher` 模块的 8 阶段
3. **进入 UI** → `UiAView` 点按钮走 A→B→C → 对应 `ui` 的打开/传参/关闭通知
4. **看 Console** → `D` 日志门面输出 + `FPSOnGUI` → 对应 `core`

想演示更多模块，按本文「❌ 无演示」章节的示例代码临时贴进 `HotfixTest.Init()` 即可——API 签名均已从包内源码核实。

---

## 附：模块 → 程序集对照

| 包 | 程序集 |
|---|---|
| core / launcher / yooasset / ui / audio / reddot / localization / archive / http / net / preference | `EF.Core` / `EF.Launcher` / `EF.YooAsset` / `EF.UI` / `EF.Audio` / `EF.RedDot` / `EF.Localization` / `EF.Archive` / `EF.Http` / `EF.Net` / `EF.Preference` |
| datable / packages | 仅编辑器工具，无运行时程序集 |

**AOT / 热更边界**：`EF.Core`、`EF.Launcher` 常驻 AOT；其余 9 个 EF 运行时程序集 + `ExampleGameHotfix` 在热更列表。`EF.Launcher` 不得引用任何热更程序集。

> 每个包的详细说明见 `Packages/cn.efefef.<模块>/README.md`（launcher 的最完整）。

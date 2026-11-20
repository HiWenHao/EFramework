# EF.UI

UI 管理器资产包：View 栈式打开/关闭、动画、子面板，以及一组 UGUI 增强控件。

## 门面

`EasyFramework.Managers.Ui.UiManager`（`[Manager]`，`MonoSingleton`）

```csharp
await UiManager.Instance.OpenView<UiAView>();
await UiManager.Instance.OpenView<UiBView, string>("参数");
await UiManager.Instance.CloseView<UiBView, string>("回传参数");
await UiManager.Instance.CloseAndNotify<UiCView, UiBView, string>("回传");
```

- View 契约：`IUiView` / `IUiEnable` / `IUiDisable` / `IUiChildView`（子面板标记）。
- 子面板：`AddChild<TParent, TChild>` 系列，配合 `UiBinding.IsChild`；子面板禁止 `OpenView` 独立打开。
- 动画：`UiAnimationConfig` + `UiViewAnimationType`。
- 增强控件：`ImagePro` / `ButtonPro` / `RadarMap` / `Slideshow` /
  `CircularScrollList`（循环滚动）/ `ScrollListPro`（无限不规则滚动）。
- 绑定：`UiBinding` + 编辑器 `UiBindingEditor` 自动生成强类型 View 代码。
- `OpenDebug` 控制诊断日志；错误日志始终输出。


## 安装

在项目的 `Packages/manifest.json` 添加依赖。日常开发用 Gitee 镜像（国内快，跟随最新）：

```json
"cn.efefef.ui": "https://gitee.com/AlvinCN/EFramework.git?path=EF_Unity/Packages/cn.efefef.ui"
```

需要锁定版本时改用 GitHub 源库 + CD 发布的 tag（Gitee 是镜像，没有 tag）：

```json
"cn.efefef.ui": "https://github.com/HiWenHao/EFramework.git?path=EF_Unity/Packages/cn.efefef.ui&ref=cn.efefef.ui-v0.0.1"
```

依赖：`cn.efefef.core`、`com.unity.ugui`、`com.unity.render-pipelines.universal`。
示例见 `Samples~`。

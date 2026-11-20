# EF.RedDot

一套完整且成熟的红点系统：树形节点、脏标记刷新、批处理、可替换渲染器。

## 门面

`EasyFramework.Systems.RedDot.RedDotSystem`（`MonoSingleton`）

```csharp
RedDotSystem.Instance.InitSubsystem();                       // 首次使用前调用一次
var node = RedDotSystem.Instance.RegisterNode("task.reward");
node.SetNumber(1);                                           // 下一帧 LateUpdate 生效
RedDotSystem.Instance.BeginBatch();
// ... 批量修改
RedDotSystem.Instance.EndBatch();                            // 深度归零时统一 Flush
```

- 子系统：`DirtySystem`（收集脏节点）、`BatchSystem`（批处理）、`EventSystem`（变更事件），
  均可经 `InitSubsystem(dirty, batch, event)` 注入自定义实现。
- **数据驱动批量注册**：`RegisterTree(IReadOnlyList<RedDotTreeNodeDef>)` 按拓扑序注册，
  不要求父先于子；引用了但未定义的父键自动建 Dot 占位节点。
- **批量设值**：`SetNumbers(IReadOnlyList<KeyValuePair<string,int>>)` 一次 Begin/End 完成。
- **显式值 vs 聚合**：`SetNumber(n)` 设值后仍会被子节点变化重新聚合（默认行为）；
  需要"父节点强制态"（强制关闭/常亮）时用 `SetOverride(n)`，它不会被子节点覆盖，
  调 `ClearOverride()` 后恢复聚合。
- **解耦测试**：`IRedDotContext` 暴露 DirtySystem/EventSystem/LoadSpriteAsync/GetNode；
  节点构造与 `RedDotView.SetContext` 可注入 fake，无需启动 Unity 单例。
- 渲染器：`DotRenderer` / `NumberRenderer` / `TMPNumberRenderer` / `ImageRenderer`，
  均继承 `RedDotRendererBase`。
- 资源：`SetResourceProvider(Func<string, UniTask<Sprite>>)` 注入贴图加载；`ClearSpriteCache` 清缓存。
- 示例见 `Samples~` 下的 `RedDotExample`。


## 安装

在项目的 `Packages/manifest.json` 添加依赖。日常开发用 Gitee 镜像（国内快，跟随最新）：

```json
"cn.efefef.reddot": "https://gitee.com/AlvinCN/EFramework.git?path=EF_Unity/Packages/cn.efefef.reddot"
```

需要锁定版本时改用 GitHub 源库 + CD 发布的 tag（Gitee 是镜像，没有 tag）：

```json
"cn.efefef.reddot": "https://github.com/HiWenHao/EFramework.git?path=EF_Unity/Packages/cn.efefef.reddot&ref=cn.efefef.reddot-v0.1.0"
```

依赖：`cn.efefef.core`。

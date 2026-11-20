# EF.RedDot

一套完整且成熟的红点系统：树形节点、脏标记刷新、批处理、可替换渲染器。

## 门面

`EasyFramework.Systems.RedDot.RedDotSystem`（`MonoSingleton`）

```csharp
var node = RedDotSystem.Instance.RegisterNode("task.reward");
node.SetValue(1);
RedDotSystem.Instance.BeginBatch();
// ... 批量修改
RedDotSystem.Instance.EndBatch();   // 内部 Flush，统一刷新
```

- 子系统：`DirtySystem`（收集脏节点）、`BatchSystem`（批处理）、`EventSystem`（变更事件）。
- 渲染器：`DotRenderer` / `NumberRenderer` / `ImageRenderer`，均继承 `RedDotRendererBase`。
- 资源：`SetResourceProvider(Func<string, UniTask<Sprite>>)` 注入贴图加载；`ClearSpriteCache` 清缓存。
- 示例见 `Samples~` 下的 `RedDotExample`。

依赖：`cn.efefef.core`。

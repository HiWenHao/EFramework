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

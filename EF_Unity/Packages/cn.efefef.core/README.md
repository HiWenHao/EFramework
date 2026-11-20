# EF.Core

EF 框架核心库：单例管理底座、事件/对象池/流程/资源门面、生命周期通知、内嵌 UniTask 与静态分析器。

## 安装

在项目的 `Packages/manifest.json` 添加依赖。日常开发用 Gitee 镜像（国内快，跟随最新）：

```json
"cn.efefef.core": "https://gitee.com/AlvinCN/EFramework.git?path=EF_Unity/Packages/cn.efefef.core"
```

需要锁定版本时改用 GitHub 源库 + CD 发布的 tag（Gitee 是镜像，没有 tag）：

```json
"cn.efefef.core": "https://github.com/HiWenHao/EFramework.git?path=EF_Unity/Packages/cn.efefef.core&ref=cn.efefef.core-v0.1.0"
```

## 主要能力

- `EFC`：框架根，负责 `[Manager]` 单例注册与生命周期分发。
- `EventManager`：泛型事件总线（零装箱 + 池化快照）。
- `PoolManager` / `ObjectPool` / `GameObjectPool`：对象池。
- `ProcedureManager` / `IProcedure`：流程栈。
- `AssetsManager`：资源门面，句柄化引用计数（`AssetHandle<T>`），可切换 `DefaultAssetsSystem` / `YooAssetsSystem`。
- `BindableProperty<T>`：可绑定属性。

依赖：UniTask、Newtonsoft.Json（见 package.json）。

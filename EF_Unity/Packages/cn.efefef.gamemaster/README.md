# EF.GameMaster

游戏总控台（GM）命令总线：运行时注册/执行/取消 + Editor 可视化面板。

> **本包只提供机制，安装后需注册命令。** 加金币、跳关、无敌等具体命令属于项目业务，需在项目层调用 `GameMasterManager.Register(...)` 注册。装包后面板为空属正常现象。

## 快速开始

```csharp
// 在项目热更入口（EFC.Initialize 之后）注册命令
GameMasterManager.Register(
    id: "gamemaster.data.addgold",
    display: "加金币",             // 显示名原文，写什么显示什么（不走多语言）
    risk: GmRisk.Normal,
    execute: ctx =>
    {
        int amount = ctx.Args.TryGetValue("amount", out var v) ? (int)v : 100;
        // YourRoleManager.Instance.AddGold(amount);
        D.Log("加金币：" + amount);
        return UniTask.CompletedTask;
    },
    paramKeys: new[] { "amount" },   // 面板会显示参数输入框
    section: "角色");                // 面板分区，默认 Common
```

编辑器菜单 `EFTools/🎮 Game Master/Open Panel`（仅 Play 模式、EF 框架就绪后有数据）。

注册可在 EF 初始化前调用（进入暂存队列，Init 末尾统一应用），也可在热更入口初始化后直接调用。所有静态 API 会自动按需触发 `GameMasterManager.Instance` 的创建与注册——但前提是 EFC 已启动。

## 说明

- 命令 ID 必须匹配 `[A-Za-z_][A-Za-z0-9_.]*`；`displayKey` 是**显示名原文**，注册写什么面板就显示什么，不做多语言解析（中文也可以）。面板自身的按钮/图例文本仍走 `Editorlanguages.json` 的 `GameMaster.*`
- `GmRisk` 在面板按等级着色：ReadOnly 灰 / Normal 绿 / Dangerous 橙 / Destructive 红；Dangerous 及以上执行前二次确认
- 执行由 `GameMasterManager` 串行驱动并统一 `AttachExternalCancellation`；`QuitGames` 时先取消再释放，清空注册表与暂存队列
- 玩家正式包不含 GM 入口（`#if UNITY_EDITOR || DEVELOPMENT_BUILD`），门面在 Release 下退化为无操作桩
- 门面只走静态方法，不要调用 `GameMasterManager.Instance`（Release 桩无该成员）

## 取消机制（两层）

取消分两层，简单命令只需第一层（系统自动），复杂命令才需要主动配合第二层：

**第一层 · 系统自动（无需命令作者介入）**

`GameMasterManager` 在派发每条命令时统一执行 `AttachExternalCancellation(token)`。因此调用 `CancelAll()` 或框架退出时，**任何命令的 await 链都会被中断**，无论命令体是否读取 token。你的简单命令（如上面的加金币）不需要碰 `ctx.Cancellation`。

**第二层 · 命令作者主动配合（可选）**

`ctx.Cancellation` 暴露给命令体，用于需要精细控制的长耗时/循环命令：

```csharp
// 场景 1：分段循环里设检查点
execute: async ctx =>
{
    for (int i = 0; i < 100; i++)
    {
        ctx.Cancellation.ThrowIfCancellationRequested();
        await ProcessOne(i);
    }
}

// 场景 2：把 token 传给下游接受 token 的 API
execute: ctx => SomeLongOperationAsync(ctx.Cancellation);
```

> 注意：若命令体 `await` 的是**不接受 token** 的调用（如无参 `UniTask.Delay`），第一层会中断外层 await、命令记为 Canceled，但该调用本身在后台仍会跑完。想彻底干净就走第二层，把 token 传下去。

> `CancelAll()` 之后本会话后续 `Run` 均返回 Canceled，需重启框架（`QuitGames` → `Initialize`）恢复。

## 边界（必须知道）

- **GM Cancel ≠ 流程事务回滚**：`ProcedureManager.Switch<T>()` 不接受取消令牌；GM 取消只停止派发新命令并让 await 链尽快返回，已进入的流程由 `ProcedureManager.Quit()` 兜底清栈
- **禁止在 `IOnAppQuit` 回调里执行 GM 命令**：GM 的 Order 最低（最先 Quit），此时注册表可能仍存命令，会创建游离实例
- **批量执行按注册序串行执行全部命令**（`RunAll()`）：不再按类别过滤，注意命令间的副作用顺序
- **不提供 Undo**：加金币/跳流程等副作用不可逆；机制层不承诺撤销

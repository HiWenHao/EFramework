# EF.Localization

运行时多语言（I18N）与编辑器多语言词条管理：语言表按需加载、切换回落、编辑器词条键清单唯一数据源。

## 运行时门面

`EasyFramework.Managers.Localization.I18NManager`（`[Manager]`，`MonoSingleton`）

```csharp
string text = I18NManager.Instance.Get("SomeKey");               // 查当前语言词条
string fmt = I18NManager.Instance.GetFormat("SomeKey", arg);     // 带占位符
bool ok = await I18NManager.Instance.SwitchLanguage("zh");       // 切换并加载语言表
```

- 语言表经 `AssetsManager` 按需加载，内存中任何时刻只有当前语言一张表（YooAsset 热更友好）。
- 目标语言加载失败回落到兜底语言；`IsLoaded` / `CurrentLanguage` 可查询状态。
- 玩家语言选择经 `cn.efefef.preference` 持久化。
- `OpenDebug` 控制装载/格式化诊断日志；兜底语言缺失等错误始终输出。

## 编辑器词条管理

- 各包词条键清单 `{pkg}/Editor Resources/Description/Editorlanguages.json` 为唯一语言数据源。
- 菜单：EFTools 的语言同步 / ReloadLanguages（改键清单后必须重跑才刷新运行时表）。
- 词条窗口：`EF.LanguageEntryWindow`（🌐 Language Entries）。

依赖：`cn.efefef.core`、`cn.efefef.preference`、`UniTask`。

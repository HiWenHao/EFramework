# Boot Scene 示例

一个**零预制体、零美术资源**的启动界面示例：所有 UI 在运行时用 UGUI 拼出来。

## 用法

1. 在 Package Manager 里选中 `EF.Launcher` → Samples → **Boot Scene (UGUI)** → Import
2. 新建场景，建一个空物体，挂两个组件：
   - `Launcher`（菜单 `EF/Launcher`）
   - `SampleLauncherView`（本目录下的脚本）
3. `Assets → Create → EF → Launcher Config` 生成配置，拖到 `Launcher.Config`
4. 填 `EntryType` / `EntryMethod` / `HotfixDlls`，然后 Play

## 这个示例演示了什么

| 文件 | 说明 |
|---|---|
| `SampleLauncherView.cs` | 实现 `ILauncherView`，画状态文案 + 进度条 + 重试按钮 |
| `EF.LauncherSample.asmdef` | **必须保留**：只引 `EF.Launcher.Runtime` + `UnityEngine.UI` |

## 为什么要单独的 asmdef

导入到 `Assets/Samples/...` 后，如果没有 asmdef，脚本会掉进 `Assembly-CSharp`，
而 `Assembly-CSharp` 会自动引用所有 `autoReferenced` 程序集（包含 `EF.Core`）。
某天有人顺手写一句 `using EasyFramework;`，EF.Core 就被拖回 AOT 引用链，全量热更当场失效。

有了这个 asmdef，误用会**立刻编译报错**，而不是埋到打包那天才炸。

## 改造成自己的界面

`SampleLauncherView` 只是把三个回调画到屏幕上，你可以：

- 换成预制体 + Animator / DOTween 动效（DOTween 也在 AOT，随便用）
- 换成 TextMeshPro（TMP 也在 AOT）
- 拆成「检查更新 / 下载中 / 解压中 / 失败重试」多个页面，按 `LauncherState` 切换

**唯一不能用的就是 `EF.UI`** —— 它在热更层，启动时还不存在。

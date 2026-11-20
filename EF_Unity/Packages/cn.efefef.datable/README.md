# EF.Datable

Luban 导表工具的 Unity 编辑器封装，统一表格生成流程（全量 / 单表）。

## 编辑器面板

菜单入口见 `DatableConfigPanel`（归属 `EF.Datable.Editor` 程序集）。

- 扫描工程 `EFTools/Datable/Datas` 下的 `.xlsx/.xls/.xlsm/.csv` 表格。
- **全量生成**：按 `luban.conf` 输出代码与数据。
- **单表生成**：临时精简 schema + 临时 conf + 输出隔离，仅合并该表数据；不覆盖代码目录
  （避免破坏 `DatableMgr` 注册表）。
- 路径一律在磁盘落工程根相对值，运行时换算为绝对路径，避免跨机器 git 噪音。

依赖：`cn.efefef.core`。导表需本地安装 Luban（见面板配置）。

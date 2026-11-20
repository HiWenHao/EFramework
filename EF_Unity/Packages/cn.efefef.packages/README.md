# EF.Packages

EF 包管理与配置中心（**仅编辑器**，`EF.PackageManager.Editor` 程序集）。

- 集中配置面板 `EFPackageConfigPanel`：管理本地包 / Git 包清单、版本状态、更新检查。
- `ServerToolkit`：包资源的上传、下载与刷新流程。
- 配置数据模型见 `Editor/PackageConfig/JsonData/`（`EFPackageCatalog` / `EFPackageInfo` / `GitPackageInfo` 等）。

依赖：`cn.efefef.core`。无运行时程序集。

# EF.Packages

EF 包管理与配置中心（**仅编辑器**，`EF.PackageManager.Editor` 程序集）。

- 集中配置面板 `EFPackageConfigPanel`：管理本地包 / Git 包清单、版本状态、更新检查。
- `ServerToolkit`：包资源的上传、下载与刷新流程。
- 配置数据模型见 `Editor/PackageConfig/JsonData/`（`EFPackageCatalog` / `EFPackageInfo` / `GitPackageInfo` 等）。


## 安装

在项目的 `Packages/manifest.json` 添加依赖。日常开发用 Gitee 镜像（国内快，跟随最新）：

```json
"cn.efefef.packages": "https://gitee.com/AlvinCN/EFramework.git?path=EF_Unity/Packages/cn.efefef.packages"
```

需要锁定版本时改用 GitHub 源库 + CD 发布的 tag（Gitee 是镜像，没有 tag）：

```json
"cn.efefef.packages": "https://github.com/HiWenHao/EFramework.git?path=EF_Unity/Packages/cn.efefef.packages&ref=cn.efefef.packages-v0.1.1"
```

依赖：`cn.efefef.core`。无运行时程序集。

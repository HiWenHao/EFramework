# 🎮 EasyFramework

<div align="center">

![EasyFramework](https://img.shields.io/badge/EasyFramework-Unity%20Framework-blue?style=for-the-badge&logo=unity)
![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black?style=for-the-badge&logo=unity&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Active-yellow?style=for-the-badge)
![GitHub Stars](https://img.shields.io/github/stars/HiWenHao/EFramework?style=social)

<br>

**一套模块化、可扩展的 Unity 游戏开发框架，帮助研发团队快速部署和交付游戏**

[快速开始](#-快速开始) · [文档](https://github.com/HiWenHao/EFramework/wiki) · [QQ群](https://jq.qq.com/?_wv=1027&k=4GvMJd6w) · [问题反馈](https://github.com/HiWenHao/EFramework/issues)

</div>

---

## 📋 目录

- [简介](#-简介)
- [核心特性](#-核心特性)
- [包结构](#-包结构)
- [快速开始](#-快速开始)
- [路线图](#-路线图)
- [推荐仓库](#-推荐仓库)
- [贡献指南](#-贡献指南)
- [许可证](#-许可证)

---

## 💡 简介

**EasyFramework (EF)** 是一套模块化、可扩展的 Unity 开发框架，旨在提供一套完整、高效的游戏开发解决方案。

框架采用 **包化管理** 的设计理念，各功能模块独立封装，开发者可以根据项目需求灵活选择使用哪些包，避免不必要的代码冗余。

### 🎯 设计理念

| 理念 | 说明 |
|------|------|
| **模块化** | 每个功能独立成包，按需引入 |
| **可扩展** | 基于接口和委托的设计，方便自定义实现 |
| **高性能** | 集成 UniTask，优化异步编程体验 |
| **易用性** | 完善的编辑器和工具支持 |

---

## ✨ 核心特性

### 🔧 核心系统 (`cn.efefef.core`)
- **UniTask 集成** - 高性能异步编程支持
- **日志系统** - 统一的日志管理
- **工具类库** - 常用工具方法和扩展

### 🎨 UI 系统 (`cn.efefef.ui`)
- **UIView 管理** - 基于堆栈的 UI 视图管理
- **ImagePro** - 增强型图片组件，支持远程图片加载
- **ButtonPro** - 增强型按钮组件
- **ScrollRectPro** - 增强型滚动视图

### 🌐 HTTP 系统 (`cn.efefef.http`)
- **异步请求** - 基于 UniTask 的异步 HTTP 请求
- **文件上传/下载** - 支持大文件传输
- **超时控制** - 灵活的超时配置

### 📦 资源系统 (`cn.efefef.assets`)
- **YooAsset 集成** - 强大的资源管理方案
- **资源加载** - 统一的资源加载接口

### 🔊 音频系统 (`cn.efefef.audio`)
- **音频管理** - 背景音乐和音效管理
- **音量控制** - 灵活的音量调节

### 🚀 启动系统 (`cn.efefef.launch`)
- **启动流程** - 游戏启动流程管理
- **初始化顺序** - 可控的初始化顺序

### 📊 配置系统 (`cn.efefef.luban`)
- **Luban 集成** - 强大的游戏配置解决方案
- **配置热更** - 支持配置热更新

### 🔴 红点系统 (`cn.efefef.reddot`)
- **红点管理** - 游戏内红点提示系统
- **树形结构** - 支持复杂的红点关系

### 📦 包管理系统 (`cn.efefef.packages`)
- **包发现** - 自动发现和记录项目中的包
- **版本管理** - 包版本比对和更新

---

## 📦 包结构

```
EasyFramework/
├── cn.efefef.core        # 核心系统 (UniTask, 工具类)
├── cn.efefef.ui          # UI 系统 (UIView, ImagePro, ButtonPro)
├── cn.efefef.http        # HTTP 系统 (异步请求, 文件传输)
├── cn.efefef.assets      # 资源系统 (YooAsset 集成)
├── cn.efefef.audio       # 音频系统 (音乐, 音效管理)
├── cn.efefef.launch      # 启动系统 (启动流程管理)
├── cn.efefef.luban       # 配置系统 (Luban 集成)
├── cn.efefef.reddot      # 红点系统 (红点提示管理)
├── cn.efefef.uiview      # UIView 系统 (UI 视图框架)
├── cn.efefef.yooasset    # YooAsset 集成包
└── cn.efefef.packages    # 包管理系统 (包发现和管理)
```

---

## 🚀 快速开始

### 1️⃣ 环境要求

- **Unity**: 2021.3.6f1 或更高版本
- **Visual Studio**: 2022 或更高版本 (推荐)

### 2️⃣ 安装框架

#### 方式一：通过 Git URL 安装 (推荐)

1. 打开 Unity 项目的 `Packages/manifest.json`
2. 添加以下依赖：

```json
{
  "dependencies": {
    "cn.efefef.core": "https://github.com/HiWenHao/EFramework.git?path=/EF_Unity/Packages/cn.efefef.core",
    "cn.efefef.ui": "https://github.com/HiWenHao/EFramework.git?path=/EF_Unity/Packages/cn.efefef.ui"
  }
}
```

#### 方式二：克隆仓库到本地

```bash
git clone https://github.com/HiWenHao/EFramework.git
```

然后在 Unity 的 `Packages/manifest.json` 中添加本地包引用：

```json
{
  "dependencies": {
    "cn.efefef.core": "file:../../EFramework/EF_Unity/Packages/cn.efefef.core"
  }
}
```

### 3️⃣ 基础使用示例

#### UI 系统 - 显示视图

```csharp
// 显示 UI 视图
await UiSystem.I.ShowView<MainMenuView>();

// 关闭 UI 视图
await UiSystem.I.HideView<MainMenuView>();
```

#### HTTP 系统 - 发送请求

```csharp
// GET 请求
string result = await HttpsSystem.I.GetTextAsync("https://api.example.com/data");

// POST 请求
string response = await HttpsSystem.I.PostJsonAsync(url, jsonData);
```

#### 资源系统 - 加载资源

```csharp
// 加载资源
var prefab = await AssetSystem.I.LoadAssetAsync<GameObject>("Assets/Prefabs/Player.prefab");
```

---

## 🗺️ 路线图

- [x] 核心系统架构搭建
- [x] UI 系统基础功能
- [x] HTTP 系统异步请求
- [x] 资源管理系统集成
- [ ] 完善文档和示例
- [ ] 添加更多单元测试
- [ ] 性能优化和基准测试
- [ ] 提供更多示例项目

---

## 🌟 推荐仓库

### [YooAsset](https://github.com/tuyoogame/YooAsset)
> YooAsset 是一套用于 Unity3D 的资源管理系统，用于帮助研发团队快速部署和交付游戏。它可以满足商业化游戏的各类需求，并且经历多款百万 DAU 游戏产品的验证。

### [代码哲学](https://code-philosophy.com)
> 让开发者专注于创造更有乐趣的游戏。下边两个都是源自于该公司，提升自我技术、期待有一天可以像他们一样厉害。

#### [HybridCLR](https://github.com/focus-creative-games/hybridclr)
> 一个特性完整、零成本、高性能、低内存的近乎完美的 Unity 全平台原生 C# 热更方案。

#### [Luban](https://luban.doc.code-philosophy.com)
> 一个强大、易用、优雅、稳定的游戏配置解决方案。

---

## 🤝 贡献指南

我们欢迎任何形式的贡献！

### 如何贡献

1. **Fork 本仓库**
2. **创建你的特性分支** (`git checkout -b feature/AmazingFeature`)
3. **提交你的更改** (`git commit -m 'Add some AmazingFeature'`)
4. **推送到分支** (`git push origin feature/AmazingFeature`)
5. **打开一个 Pull Request**

### 贡献者

感谢所有为本项目做出贡献的开发者！

---

## 📞 联系方式

- **QQ 群**: [点击加入【EF】](https://jq.qq.com/?_wv=1027&k=4GvMJd6w)
- **GitHub Issues**: [问题反馈](https://github.com/HiWenHao/EFramework/issues)
- **Xmind 文件**: [EasyFramework.xmind](https://github.com/HiWenHao/EFramework/blob/master/Assets/EasyFramework/EFAssets/Other/EasyFramework-Unity.xmind) - 统计了项目整体框架内容

---

## 📄 许可证

本项目采用 **MIT 许可证** - 查看 [LICENSE](LICENSE) 文件了解详情。

---

<div align="center">

**⭐ 如果这个项目对你有帮助，请给我们一个 Star！⭐**

Made with ❤️ by [HiWenHao](https://github.com/HiWenHao)

</div>

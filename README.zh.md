# Polymerium

![Polymerium](https://socialify.git.ci/d3ara1n/Polymerium/image?description=1&font=Jost&forks=1&issues=1&language=1&name=1&owner=1&pattern=Overlapping%20Hexagons&pulls=1&stargazers=1&theme=Auto)

<div align="center">

**下一代 Minecraft 实例管理器，以全新思维重新定义游戏管理。**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](https://opensource.org/licenses/MIT)
[![.NET 9.0](https://img.shields.io/badge/.NET-9-5C2D91?style=for-the-badge&logoColor=white)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-11-3355FF?style=for-the-badge&logoColor=white)](https://avaloniaui.net/)
[![C#](https://img.shields.io/badge/C%23-14-239120?style=for-the-badge&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)

[![Codacy Badge](https://app.codacy.com/project/badge/Grade/8516e3e1a3994d138a1adc537d7c6ecd)](https://app.codacy.com/gh/d3ara1n/Polymerium/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)
[![CodeFactor](https://www.codefactor.io/repository/github/d3ara1n/polymerium/badge)](https://www.codefactor.io/repository/github/d3ara1n/polymerium)

[📥 下载](https://github.com/d3ara1n/Polymerium/releases) • [📖 文档](https://github.com/d3ara1n/Polymerium/wiki) • [🐛 报告问题](https://github.com/d3ara1n/Polymerium/issues) • [💡 功能建议](https://github.com/d3ara1n/Polymerium/issues)

</div>

---

## 🎮 体验 Polymerium 的实际效果

<div align="center">

[![Screenshot](assets/screenshots/overview.avif)](#-体验-polymerium-的实际效果)

*从启动到游戏，只需几秒钟* ⚡

</div>

---

## 🌟 Polymerium 的独特之处

> **传统的 Minecraft 启动器管理文件。Polymerium 管理体验。**

Polymerium 不是复制和存储每个实例的数千个模组文件，而是将您的游戏设置描述为轻量级元数据，并按需构建实际的游戏文件。这种革命性的方法带来了几个改变游戏规则的好处：

### 🎯 **智能资源管理**

- **零重复**：多个实例共享相同的模组？磁盘上只存在一个副本
- **符号链接魔法**：文件被智能链接而非复制，节省数 GB 的存储空间
- **即时切换**：在几秒钟内切换完全不同的模组包

### 📦 **便携式游戏体验**

- **元数据驱动**：您的整个游戏设置都包含在一个小小的配置文件中
- **版本控制就绪**：使用 Git 协作开发模组包
- **真正的便携性**：通过简单的文件传输分享您的确切游戏体验

### 🔧 **轻松维护**

- **完整性保证**：每次部署都会验证文件的完整性和正确性
- **依赖解析**：自动处理模组依赖和冲突
- **一键更新**：无缝升级单个模组或整个模组包

### 🎮 **以玩家为中心的设计**

- **无需寻找 Java**：配置一次 Java，到处可用
- **账户灵活性**：多个账户，每个都链接到特定实例
- **干净卸载**：通过删除两个文件夹完全移除 Polymerium

---

## ✨ 主要功能

### 🏗️ 现代架构

- 🎨 **Avalonia UI**：美观、响应式的跨平台界面
- 📋 **元数据引擎**：轻量级实例描述，每次都能完美重建
- 🚀 **部署系统**：智能文件管理与完整性检查
- 🔗 **资源池**：共享文件存储与符号链接分发

### 🌐 平台集成

- 🎟️ **CurseForge 和 Modrinth**：与主要模组仓库的原生集成
- 📦 **模组包发布**：将您的实例导出为可分发的模组包
- 📝 **自动更新日志**：为您的模组包版本生成文档

### 👨‍💻 开发者体验

- 📸 **实例快照**：保存和恢复完整的游戏状态
- 📜 **分层配置**：将用户设置与核心游戏数据分离
- 🔄 **构建可重现性**：从相同元数据进行相同部署

---

## 开始使用

### 前置要求

> [!IMPORTANT]
> **需要 Windows 开发者模式**
>
> Polymerium 使用[符号链接](https://www.wikiwand.com/en/Symbolic_link)进行高效的文件管理。启用开发者模式以允许在没有管理员权限的情况下创建符号链接。

#### 📋 如何启用开发者模式

##### Windows 11

```sh
设置 → 系统 → 开发者选项 → 开发者模式
```

##### Windows 10

```sh
设置 → 更新和安全 → 开发者选项 → 开发者模式
```

##### Windows 7/8

```sh
请先升级到 Windows 10+ 😉
```

### 📥 安装

> [!NOTE]
> Polymerium 目前正在积极开发中。功能和界面可能在版本之间发生变化。

**📥 下载** → **📂 解压** → **🚀 运行** → **⚙️ 设置**

1. **下载** [GitHub Releases](https://github.com/d3ara1n/Polymerium/releases) 的最新版本
2. **运行** 安装程序 `Polymerium-[arch]-Setup.exe`
3. **配置** 设置向导配置您的第一个实例

### 🚀 快速开始

**🎮 创建** → **📦 添加内容** → **🔧 部署** → **▶️ 游戏**

1. **创建实例**：定义您的 Minecraft 版本和模组加载器
2. **添加内容**：从 CurseForge 或 Modrinth 浏览和安装模组
3. **部署**：让 Polymerium 构建您的游戏文件
4. **游戏**：直接启动或导出为模组包

---

## 🏗️ 架构概览

| 🛠️ 技术                     | 📋 用途                                 | 🔗 集成          |
|-----------------------------|----------------------------------------|------------------|
| **.NET 9.0**                | 具有 C# 预览功能的最新运行时              | 核心平台         |
| **Avalonia 11**             | 跨平台 XAML UI 框架                     | 表示层           |
| **MVVM 模式**               | 清晰的关注点分离                         | 架构模式         |
| **依赖注入**                | 模块化、可测试的架构                      | 服务管理         |
| **响应式扩展**              | 响应式数据处理                           | 数据流           |

### 📁 项目结构

```sh
Polymerium/
├── 🛠️ .kiro/steering/         # 开发指南
├── 📚 docs/                   # 文档
├── 🎨 src/Polymerium.App/     # UI 应用程序层
├── 🔗 submodules/             # 共享组件
└── 📦 Releases/               # 构建产物
```

---

## 开发

### 🔨 从源码构建

```bash
# 克隆包含子模块
git clone --recursive https://github.com/d3ara1n/Polymerium.git
cd Polymerium

# 构建解决方案
dotnet build

# 以开发模式运行
./Development.ps1
```

### 🛠️ 开发命令

```powershell
# 开发模式
./Development.ps1

# 生产模式
./Production.ps1

# 构建和发布
./Publish.ps1

# 获取版本信息
dotnet gitversion

# 生成更新日志
git cliff
```

### 🤝 贡献

我们欢迎贡献！请确保您的代码遵循既定的模式：

| 方面                        | 要求                                       |
|-----------------------------|-------------------------------------------|
| 🏗️ **架构**                | 具有清晰关注点分离的 MVVM 模式              |
| 💉 **依赖注入**             | 全程使用构造函数注入                        |
| 🔥 **现代 C#**              | 利用最新的语言功能和模式                    |
| ✨ **代码风格**             | 遵循 .editorconfig 指南                   |

> [!TIP]
> 查看我们的[指导文档](.kiro/steering/)了解详细的项目指南和架构模式。

---

## 🖥️ 平台支持

| 平台                                                                                                       | 状态               | 备注                                       |
|------------------------------------------------------------------------------------------------------------|--------------------|--------------------------------------------|
| ![Windows](https://img.shields.io/badge/Windows-10+-0078D6?style=flat-square&logo=windows&logoColor=white) | ✅ **稳定**         | 主要平台，具有完整功能支持                   |
| ![Linux](https://img.shields.io/badge/Linux-WIP-FCC624?style=flat-square&logo=linux&logoColor=black)       | 🚧 **进行中**       | 核心功能正常工作                            |
| ![macOS](https://img.shields.io/badge/macOS-Planned-000000?style=flat-square&logo=apple&logoColor=white)   | 📋 **计划中**       | 未来发布目标                               |

---

## 隐私与安全

Polymerium 尊重您的隐私：

- **少量遥测**：仅收集最少数据用于调试错误
- **本地存储**：所有数据都保留在您的机器上
- **最小占用**：干净卸载不留痕迹
- **开源**：透明、可审计的代码库

---

## 许可证

本项目采用 MIT 许可证 - 详情请参阅 [LICENSE](LICENSE) 文件。

---

## 📊 项目统计

[![Star History Chart](https://api.star-history.com/svg?repos=d3ara1n/Polymerium&type=Date)](https://www.star-history.com/#d3ara1n/Polymerium&Date)

![Repobeats Analytics](https://repobeats.axiom.co/api/embed/594b206d199e6aae83226e6b7b834f6896322858.svg "Repobeats analytics image")

## 📚 参考资料与致谢

### 🔗 技术参考

- [Inside a Minecraft Launcher](https://ryanccn.dev/posts/inside-a-minecraft-launcher) - 游戏启动过程和 Fabric/Quilt 部署
- [Tutorial: Writing a Launcher](https://minecraft.fandom.com/zh/wiki/%E6%95%99%E7%A8%8B/%E7%BC%96%E5%86%99%E5%90%AF%E5%8A%A8%E5%99%A8) - 游戏启动过程指南
- [ForgeWrapper](https://github.com/ZekerZhayard/ForgeWrapper) - Forge 集成参考
- [Microsoft Authentication Scheme](https://wiki.vg/Microsoft_Authentication_Scheme) - 身份验证实现

### 🙏 特别感谢

- **Minecraft 社区** - 为了令人难以置信的模组生态系统
- **Avalonia 团队** - 为了出色的跨平台 UI 框架
- **API 提供商** - CurseForge 和 Modrinth 提供的公共 API
- **贡献者** - 每一个帮助 Polymerium 变得更好的人

## 📄 许可证

[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fd3ara1n%2FPolymerium.svg?type=large&issueType=license)](https://app.fossa.com/projects/git%2Bgithub.com%2Fd3ara1n%2FPolymerium?ref=badge_large&issueType=license)

本项目采用 **MIT 许可证** - 详情请参阅 [LICENSE](LICENSE.txt) 文件。

---

<div align="center">

**Polymerium：为现代时代重新思考 Minecraft 实例管理** ✨

由 Polymerium 团队用 ❤️ 制作

</div>

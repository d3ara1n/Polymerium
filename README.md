# Polymerium

![Polymerium](https://socialify.git.ci/d3ara1n/Polymerium/image?description=1&font=Jost&forks=1&issues=1&language=1&name=1&owner=1&pattern=Overlapping%20Hexagons&pulls=1&stargazers=1&theme=Auto)

<div align="center">

**A next-generation Minecraft instance manager that thinks differently about game management.**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](https://opensource.org/licenses/MIT)
[![.NET 9.0](https://img.shields.io/badge/.NET-9-5C2D91?style=for-the-badge&logoColor=white)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-11-3355FF?style=for-the-badge&logoColor=white)](https://avaloniaui.net/)
[![C#](https://img.shields.io/badge/C%23-14-239120?style=for-the-badge&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)

[![Codacy Badge](https://app.codacy.com/project/badge/Grade/8516e3e1a3994d138a1adc537d7c6ecd)](https://app.codacy.com/gh/d3ara1n/Polymerium/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)
[![CodeFactor](https://www.codefactor.io/repository/github/d3ara1n/polymerium/badge)](https://www.codefactor.io/repository/github/d3ara1n/polymerium)

[📥 Download](https://github.com/d3ara1n/Polymerium/releases) • [📖 Documentation](https://github.com/d3ara1n/Polymerium/wiki) • [🐛 Report Bug](https://github.com/d3ara1n/Polymerium/issues) • [💡 Request Feature](https://github.com/d3ara1n/Polymerium/issues)

</div>

---

## 🎮 Experience Polymerium in Action

<div align="center">

[![Screenshot](assets/screenshots/overview.avif)](#-experience-polymerium-in-action)

*From launch to gameplay in seconds* ⚡

</div>

---

## 🌟 What Makes Polymerium Different

> **Traditional Minecraft launchers manage files. Polymerium manages experiences.**

Instead of copying and storing thousands of mod files for each instance, Polymerium describes your game setup as
lightweight metadata and builds the actual game files on-demand. This revolutionary approach brings several
game-changing benefits:

### 🎯 **Smart Resource Management**

- **Zero Duplication**: Multiple instances sharing the same mods? Only one copy exists on disk
- **Symlink Magic**: Files are intelligently linked, not copied, saving gigabytes of storage
- **Instant Switching**: Change between completely different modpacks in seconds

### 📦 **Portable Game Experiences**

- **Metadata-Driven**: Your entire game setup fits in a tiny configuration file
- **Version Control Ready**: Use Git to collaborate on modpack development
- **True Portability**: Share your exact game experience with a simple file transfer

### 🔧 **Effortless Maintenance**

- **Integrity Guaranteed**: Every deployment validates file completeness and correctness
- **Dependency Resolution**: Automatically handles mod dependencies and conflicts
- **One-Click Updates**: Upgrade individual mods or entire modpacks seamlessly

### 🎮 **Player-Focused Design**

- **No Java Hunting**: Configure Java once, works everywhere
- **Account Flexibility**: Multiple accounts, each linked to specific instances
- **Clean Uninstall**: Remove Polymerium completely by deleting two folders

---

## ✨ Key Features

<details open>
<summary><strong>🏗️ Modern Architecture</strong></summary>

- 🎨 **Avalonia UI**: Beautiful, responsive interface that works across platforms
- 📋 **Metadata Engine**: Lightweight instance descriptions that rebuild perfectly every time
- 🚀 **Deployment System**: Intelligent file management with integrity checking
- 🔗 **Resource Pooling**: Shared file storage with symlink distribution

</details>

<details open>
<summary><strong>🌐 Platform Integration</strong></summary>

- 🎟️ **CurseForge & Modrinth**: Native integration with major mod repositories
- 📦 **Modpack Publishing**: Export your instances as distributable modpacks
- 📝 **Automatic Changelogs**: Generated documentation for your modpack versions

</details>

<details open>
<summary><strong>👨‍💻 Developer Experience</strong></summary>

- 📸 **Instance Snapshots**: Save and restore complete game states
- 📜 **Layered Configuration**: Separate user settings from core game data
- 🔄 **Build Reproducibility**: Identical deployments from the same metadata

</details>

---

## Getting Started

### Prerequisites

> [!IMPORTANT]
> **Windows Developer Mode Required**
>
> Polymerium uses [symbolic links](https://www.wikiwand.com/en/Symbolic_link) for efficient file management. Enable
> Developer Mode to allow symlink creation without administrator privileges.

<details>
<summary><strong>📋 How to Enable Developer Mode</strong></summary>

#### Windows 11

```
Settings → System → For developers → Developer Mode
```

#### Windows 10

```
Settings → Update & Security → For developers → Developer Mode
```

#### Windows 7/8

```
Upgrade to Windows 10+ first 😉
```

</details>

### 📥 Installation

> [!NOTE]
> Polymerium is currently in active development. Features and UI may change between releases.

**📥 Download** → **📂 Extract** → **🚀 Run** → **⚙️ Setup**

1. **Download** the latest release from [GitHub Releases](https://github.com/d3ara1n/Polymerium/releases)
2. **Run** installer `Polymerium-[arch]-Setup.exe`
3. **Follow** the setup wizard to configure your first instance

### 🚀 Quick Start

**🎮 Create** → **📦 Add Content** → **🔧 Deploy** → **▶️ Play**

1. **Create Instance**: Define your Minecraft version and mod loader
2. **Add Content**: Browse and install mods from CurseForge or Modrinth
3. **Deploy**: Let Polymerium build your game files
4. **Play**: Launch directly or export as a modpack

---

## 🏗️ Architecture Overview

| 🛠️ Technology           | 📋 Purpose                              | 🔗 Integration       |
|--------------------------|-----------------------------------------|----------------------|
| **.NET 9.0**             | Latest runtime with C# preview features | Core platform        |
| **Avalonia 11**          | Cross-platform XAML UI framework        | Presentation layer   |
| **MVVM Pattern**         | Clean separation of concerns            | Architecture pattern |
| **Dependency Injection** | Modular, testable architecture          | Service management   |
| **Reactive Extensions**  | Responsive data handling                | Data flow            |

<details>
<summary><strong>📁 Project Structure</strong></summary>

```
Polymerium/
├── 🎨 src/Polymerium.App/     # UI application layer
├── ⚙️ src/Trident.Core/ # Core business engine
├── 🔗 submodules/             # Shared components
├── 📚 docs/                   # Documentation
├── 🛠️ .kiro/steering/         # Development guidelines
└── 📦 Releases/               # Build artifacts
```

</details>

---

## Development

### 🔨 Building from Source

```bash
# Clone with submodules
git clone --recursive https://github.com/d3ara1n/Polymerium.git
cd Polymerium

# Build the solution
dotnet build

# Run in development mode
./Development.ps1
```

<details>
<summary><strong>🛠️ Development Commands</strong></summary>

```powershell
# Development mode
./Development.ps1

# Production mode
./Production.ps1

# Build and publish
./Publish.ps1

# Get version info
dotnet gitversion

# Generate changelog
git cliff
```

</details>

### 🤝 Contributing

We welcome contributions! Please ensure your code follows the established patterns:

| Aspect                      | Requirement                                    |
|-----------------------------|------------------------------------------------|
| 🏗️ **Architecture**        | MVVM pattern with clear separation of concerns |
| 💉 **Dependency Injection** | Use constructor injection throughout           |
| 🔥 **Modern C#**            | Leverage latest language features and patterns |
| ✨ **Code Style**            | Follow the .editorconfig guidelines            |

> [!TIP]
> Check out our [steering documents](.kiro/steering/) for detailed project guidelines and architecture patterns.

---

## 🖥️ Platform Support

| Platform                                                                                                   | Status             | Notes                                      |
|------------------------------------------------------------------------------------------------------------|--------------------|--------------------------------------------|
| ![Windows](https://img.shields.io/badge/Windows-10+-0078D6?style=flat-square&logo=windows&logoColor=white) | ✅ **Stable**       | Primary platform with full feature support |
| ![Linux](https://img.shields.io/badge/Linux-WIP-FCC624?style=flat-square&logo=linux&logoColor=black)       | 🚧 **In Progress** | Core functionality working                 |
| ![macOS](https://img.shields.io/badge/macOS-Planned-000000?style=flat-square&logo=apple&logoColor=white)   | 📋 **Planned**     | Future release target                      |

---

## Privacy & Security

Polymerium respects your privacy:

- **No Telemetry**: Zero data collection or tracking
- **Local Storage**: All data stays on your machine
- **Minimal Footprint**: Clean uninstall leaves no traces
- **Open Source**: Transparent, auditable codebase

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 📊 Project Statistics

<div align="center">

[![Star History Chart](https://api.star-history.com/svg?repos=d3ara1n/Polymerium&type=Date)](https://www.star-history.com/#d3ara1n/Polymerium&Date)

![Repobeats Analytics](https://repobeats.axiom.co/api/embed/594b206d199e6aae83226e6b7b834f6896322858.svg "Repobeats analytics image")

</div>

## 📚 References & Acknowledgments

<details>
<summary><strong>🔗 Technical References</strong></summary>

- [Inside a Minecraft Launcher](https://ryanccn.dev/posts/inside-a-minecraft-launcher) - Game launch process and
  Fabric/Quilt deployment
- [Tutorial: Writing a Launcher](https://minecraft.fandom.com/zh/wiki/%E6%95%99%E7%A8%8B/%E7%BC%96%E5%86%99%E5%90%AF%E5%8A%A8%E5%99%A8) -
  Game launch process guide
- [ForgeWrapper](https://github.com/ZekerZhayard/ForgeWrapper) - Forge integration reference
- [Microsoft Authentication Scheme](https://wiki.vg/Microsoft_Authentication_Scheme) - Authentication implementation

</details>

### 🙏 Special Thanks

- **Minecraft Community** - For the incredible modding ecosystem
- **Avalonia Team** - For the excellent cross-platform UI framework
- **API Providers** - CurseForge and Modrinth for their public APIs
- **Contributors** - Everyone who helps make Polymerium better

## 📄 License

<div align="center">

[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fd3ara1n%2FPolymerium.svg?type=large&issueType=license)](https://app.fossa.com/projects/git%2Bgithub.com%2Fd3ara1n%2FPolymerium?ref=badge_large&issueType=license)

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE.txt) file for details.

</div>

---

<div align="center">

**Polymerium: Rethinking Minecraft instance management for the modern era** ✨

Made with ❤️ by the Polymerium team

</div>

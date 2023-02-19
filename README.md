<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://github.com/d3ara1n/Polymerium">
    <img src="src/Polymerium.App/Assets/Logo.png" alt="Logo" width="80" height="80">
  </a>

<h3 align="center">Polymerium</h3>

  <p align="center">
    为 Minecraft 准备的游戏实例管理器
    <br />
    <a href="https://github.com/d3ara1n/Polymerium/wiki"><strong>查看文档 »</strong></a>
    <br />
    <br />
    <a href="https://github.com/d3ara1n/Polymerium/issues">反馈</a>
    ·
    <a href="https://github.com/d3ara1n/Polymerium/discussions">讨论</a>
  </p>
</div>

<!-- PROJECT SHIELDS -->
[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]

<!-- ABOUT THE PROJECT -->
## 关于

[![Screenshot][product-screenshot]](#关于)

**这是个 WIP 早期项目，大部分功能还没写完或仅存在于设计，开发进度请参考 Roadmap。**

### 理念

正如其缝合的名字一样，Polymerium 的主要目标是整合 Minecraft 的游戏资源，而非单单启动游戏。其使用与启动器完全不同的思路来管理游戏资源：创建实例元数据，使用还原引擎将游戏本地文件还原到元数据所描述的状态；Polymerium 不维护游戏文件，只维护实例元数据。有关于 Polymerium 的模式请参阅还没开始写的文档。

### Why another launcher?

这不是 *launcher*，这也不是压缩毛巾，这是 Polymerium —— *游戏实例管理器*。
初衷是在用 PrismLauncher 的时候遇到一些问题并想出一些改进的的方法，不过在写代码、与 forge installer 斗智斗勇的过程中已经忘记哪些改进了（囧。现在要回答这个问题的话，那么答案是：没有为什么，小孩子不懂事写着玩的。

### 跨平台

设计之初是跨平台的，所有文件都放在家目录和用户配置目录而不是 Windows 那逆天难找的 AppData。不过现实问题是 MAUI 没涉足 Linux Desktop，Avalonia 则看了几个对应的程序，动画缺失或卡顿（出于对个人作者的尊重，所以我把锅甩给框架本身），遂选择不跨平台从创建到现在多年积累三千 open issues和 19 年至今未处理某 issue 的 WinUI3。

### 吐槽

WinUI 3 目前还在早期阶段，一路下来难用，性能差不说，遇到的bug还不少，总之不建议入坑。

### 使用以下技术栈和工具构建

* [![C#][CSharp]][CSharp-url]
* [![dotnet][DotNet]][DotNet-url]
* [![WinUI3][WinUI]][WinUI-url]
* [![WindowsAppSDK][WindowsAppSDK]][WindowsAppSDK-url]
* [![Rider][Rider]][Rider-url]
* [![VisualStudio][VisualStudio]][VisualStudio-url]
* [![VisualStudioCode][VSCode]][VSCode-url]

<!-- GETTING STARTED -->
## 入门

目前~~不~~入门。

### 安装

在 [Release]("https://github.com/d3ara1n/Polymerium/releases") 中下载证书文件(.cer)和程序包(.msix)。

1. 双击安装证书
2. 在 Windows 设置，`隐私和安全性` 中的 `开发者选项` 打开 `开发者模式`
3. 双击程序包进入安装环节

### 配置

开箱即用。

### 添加实例

目前仅支持创建原版实例（并在随后的实例配置页面添加 modloader ）或导入 modrinth 整合包。

#### 导入 Modrinth 整合包

在 modrinth 下载整合包文件，拖动到导入页面的 DragDrop 面板，根据向导添加。

<!-- ROADMAP -->
## Roadmap

* [x] 创建项目文件夹
* [ ] 实例管理
  * [x] 从空模板创建
  * [x] 删除
  * [ ] 解锁（转换为 untagged 实例）
  * [ ] 导入/导出
    * [x] 导入预览对话框
    * [ ] Polypack
    * [ ] CurseForge
    * [x] Modrinth
    * [ ] MultiMC
* [ ] 启动游戏
  * [x] Polylock 文件
  * [ ] 可选参数
    * [ ] 自定义 JVM 参数
    * [ ] 预定义的优化 JVM 参数
  * [ ] 先决条件检查
    * [x] Java 版本自动选择和兼容性检查
  * [ ] 行星搅拌机
    * [x] Fire-and-forget 模式
    * [ ] Managed 模式
      * [ ] 订阅日志
        * [ ] 保存到文件
      * [ ] 进程管理
  * [x] 参数解析
* [ ] 账号管理
  * [x] 管理模型
    * [x] 添加
    * [x] 移除
  * [x] 离线模式
  * [x] Microsoft 账号登录
    * [ ] 开始游戏前验证账号有效性或刷新
  * [ ] 离线 Fallback 模式代替离线模式
* [ ] 还原引擎
  * [x] 基于 Stage 模型
  * [ ] 组件安装
    * [x] 香草
    * [ ] Forge
    * [x] Fabric
    * [x] Quilt
  * [ ] 附件安装
* [ ] 资源附件
  * [ ] 资源解析引擎
    * [x] {var} 参数
    * [x] {*vars} 参数
    * [ ] Url 类型转换
  * [ ] 内置解析器
    * [ ] 整合包
      * [ ] Modrinth
      * [ ] CurseForge
    * [ ] 模组
      * [ ] 本地仓库
      * [ ] Modrinth
      * [ ] CurseForge
    * [ ] 资源包
      * [ ] 本地仓库
      * [ ] Modrinth
      * [ ] CurseForge
    * [x] 文件附件
      * [x] 本地仓库
      * [x] 远程资源
* [ ] 搜索
  * [ ] 搜索中心
  * [ ] SearchBar
    * [x] 托管的实例搜索
    * [ ] 搜索中心联动
  * [ ] 互联网资源搜索
* [ ] 软件设置
* [ ] 游戏实例设置
  * [ ] 元数据编辑
  * [ ] 私有配置页面
* [ ] 本地化

更多细节请在 [Issues](https://github.com/d3ara1n/Polymerium/issues) 中查询。

<!-- REFERENCES -->
## 外部资料和参考

* 游戏启动流程、Fabric/Quilt 部署：[Inside a Minecraft Launcher](https://ryanccn.dev/posts/inside-a-minecraft-launcher/)
* 游戏启动流程：[教程/编写启动器](https://minecraft.fandom.com/zh/wiki/%E6%95%99%E7%A8%8B/%E7%BC%96%E5%86%99%E5%90%AF%E5%8A%A8%E5%99%A8?variant=zh)
* Forge：木得参考，也木得写完
* 微软验证：[Microsoft Authentication Scheme](https://wiki.vg/Microsoft_Authentication_Scheme)
* 微软验证实现：[Cml.Core.Auth.Microsoft](https://github.com/CmlLib/CmlLib.Core.Auth.Microsoft)
* Forge 版本和下载：[BMCLApi](https://bmclapidoc.bangbang93.com/)

十分感谢以上作者和所著文章。

<!-- LICENSE -->
## License

Distributed under the MIT License. See `LICENSE.txt` for more information.

## Stats

![Alt](https://repobeats.axiom.co/api/embed/594b206d199e6aae83226e6b7b834f6896322858.svg "Repobeats analytics image")

<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[contributors-shield]: https://img.shields.io/github/contributors/d3ara1n/Polymerium.svg?style=for-the-badge
[contributors-url]: https://github.com/d3ara1n/Polymerium/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/d3ara1n/Polymerium.svg?style=for-the-badge
[forks-url]: https://github.com/d3ara1n/Polymerium/network/members
[stars-shield]: https://img.shields.io/github/stars/d3ara1n/Polymerium.svg?style=for-the-badge
[stars-url]: https://github.com/d3ara1n/Polymerium/stargazers
[issues-shield]: https://img.shields.io/github/issues/d3ara1n/Polymerium.svg?style=for-the-badge
[issues-url]: https://github.com/d3ara1n/Polymerium/issues
[license-shield]: https://img.shields.io/github/license/d3ara1n/Polymerium.svg?style=for-the-badge
[license-url]: https://github.com/d3ara1n/Polymerium/blob/master/LICENSE.txt
[product-screenshot]: assets/images/Screenshot.gif
[CSharp]: https://img.shields.io/badge/C%23-11-239120?style=for-the-badge&logoColor=white
[CSharp-url]: https://learn.microsoft.com/en-us/dotnet/csharp/
[DotNet]: https://img.shields.io/badge/.NET-7-5C2D91?style=for-the-badge&logoColor=white
[DotNet-url]: https://dotnet.microsoft.com/
[WinUI]: https://img.shields.io/badge/WinUI-3-0F5197?style=for-the-badge&logoColor=white
[WinUI-url]: https://microsoft.github.io/microsoft-ui-xaml/
[WindowsAppSDK]: https://img.shields.io/badge/Windows%20App%20SDK-1.2-348753?style=for-the-badge&logoColor=white
[WindowsAppSDK-url]: https://github.com/microsoft/WindowsAppSDK
[Rider]: https://img.shields.io/badge/Rider-DE1369?style=for-the-badge&logo=Rider&logoColor=white
[Rider-url]: https://www.jetbrains.com/rider/
[VisualStudio]: https://img.shields.io/badge/Visual_Studio-5C2D91?style=for-the-badge&logo=visual%20studio&logoColor=white
[VisualStudio-url]: https://visualstudio.microsoft.com
[VSCode]: https://img.shields.io/badge/Visual_Studio_Code-0078D4?style=for-the-badge&logo=visual%20studio%20code&logoColor=white
[VSCode-url]: https://code.visualstudio.com/

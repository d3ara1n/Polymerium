# README 超链接识别与应用内跳转

关联：[POLY-138](https://d3ara1n.atlassian.net/browse/POLY-138)

## 做什么

整合包详情（ExhibitModpackToast）与包详情（ExhibitPackageModal）里的 README / changelog 渲染区，用户点击超链接时：若链接指向托管的资源仓库（Modrinth / CurseForge 等 `IRepository` 能识别的地址），则在应用内跳转到对应详情，而不是丢给系统浏览器。识别不出的链接维持现状（开浏览器 + 危险提示）。

## 想要的效果

- **包详情（ExhibitPackageModal）**：README（About 标签）和 changelog（Changelogs 标签）里的链接，识别到任意资源类型的包就打开该包的 ExhibitPackageModal，体验与「Dependencies 标签里点一个依赖包」一致；识别不到则开浏览器。
- **整合包详情（ExhibitModpackToast）**：README 里的链接，仅当识别为**整合包**时在应用内打开对应 ExhibitModpackToast；识别为普通包（mod 等）的链接**不开包页面**——toast 场景没有 instance 上下文，贸然打开带安装按钮的包详情不合适，这类链接维持浏览器回落。

## 调研得知的注意事项

基础设施两端均已就位：

- `MarkdownViewer`（Huskui.Avalonia.Markdown）有 `HyperlinkCommand`（StyledProperty<ICommand?>）。设了之后每个链接走 `Command + CommandParameter`（CommandParameter 是 `Uri?`，**仅绝对地址**，相对链接传 null）；不设则回落到 `HyperlinkButton.NavigateUri`（即直接开系统浏览器，也就是当前行为）。接入 HyperlinkCommand 不会丢失现有能力。
- `RepositoryAgent.RecognizeAsync(Uri)` 返回 `PackageIdentifier(Repository, Namespace, Identity, Version?)`，遍历各仓库尝试识别，没有任何仓库能识别时抛 `ResourceNotFoundException`。返回值里的 Version 在详情打开流程中不被消费（版本在 modal 内另选）。

两侧落地难度不同，接手时按当下代码临场决定：

- `DataService` 目前**没有**暴露 `RecognizeAsync`（它包装了 RepositoryAgent 却漏了这一个）。
- **包详情一侧基本是纯复用**：ExhibitPackageModal 已持有 `ViewPackageCommand` + `LinkExhibitCallback` + `DataService` + `Key`，链接识别后可走与「依赖点击」完全相同的路径，无需新增构造逻辑；且 `ViewPackageAsync` 构造 Filter 时 Kind 取自目标 project 自身，跨资源类型的链接也能正确打开。
- **整合包一侧不是纯复用**：`ExhibitModpackModel` 的构造 + `PopToast` 目前在 Landing / MarketplaceSearch / InstanceSetup（两处）共 **4 个地方逐字重复**，没有共享入口；ExhibitModpackToast 自身也缺 `OverlayService`。直接接入会产生第 5 份重复——是否先抽取共享入口、两半是否分阶段做，由实施时决定。

现状可复用的回落件：ExhibitPackageModal / ExhibitModpackToast 的 code-behind 各自已有 `NavigateUri` 命令 + `*_OpenPackageLinkDangerNotificationTitle` 危险提示资源（当前服务于项目 Reference 那类非 MarkdownViewer 的 HyperlinkButton），可作为「识别不到 → 开浏览器」回路的复用件。

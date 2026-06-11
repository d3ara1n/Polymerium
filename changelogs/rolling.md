## [Unreleased]

### ✨ Highlights ✨

- 添加对 CurseForge 新增的 CDN 下载需要 Api-Key 的需求的应对措施

### Fixed

- 修正 Authlib-Injector 账号的程序性配置方式，去掉了原先的非机制性（即补丁性）写法(#POLY-94)
- 添加对 CurseForge 新增的 CDN 下载需要 Api-Key 的需求的应对措施(#POLY-96)

### Added

-

### Changed

- 项目从 `Polymerium.App` 重命名为 `Polymerium.Avalonia`，避免 macOS Finder 误将源码目录识别为 `.app` bundle
- macOS bundle identifier 从 `com.Polymerium.Polymerium` 改为 `dev.dearain.Polymerium`
- macOS 应用图标使用 `Icon.icns`（应用图标）替代 `Icon.Installer.icns`（安装器图标）


### Removed

-

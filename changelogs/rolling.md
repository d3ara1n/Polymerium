## [Unreleased]

### ✨ Highlights ✨

- 修复 Sentry 收集的若干问题

### Fixed

- 修复自动更新服务未遵循设置中的自动检查更新开关的问题
- 修复导出包列表是的进度汇报异常
- 修复部分界面库升级导致的视觉错误
- 尝试通过添加异常判断与提示来修复 Sentry/POLYMERIUM-1B
- 尝试通过移除滚动条平滑动画来修复 Sentry/POLYMERIUM-19
- 通过移除生产环境的错误转储来修复 Sentry/POLYMERIUM-J
- 尝试通过重写后台任务的机制来修复 Sentry/POLYMERIUM-16
- 通过改变资源获取与释放的机制来修复潜在的资源泄露问题

### Added

- 实例页面的导航栏添加文字提示

### Changed

- 使用数字输入框优化部分数字输入场景

### Removed

- 移除 SmoothScroll.Avalonia 包并失去了滚动视图平滑滚动能力

# 分支保护策略

本项目采用严格的分支保护策略，确保代码质量和发布流程的可控性。

## 分支策略

### 🔒 **受保护分支**

- `main` - 生产分支，只接受来自 `develop` 的 PR 合并
- `release/**` - 发布分支，只接受来自 `develop` 的 PR 合并

### 🔓 **开发分支**

- `develop` - 主要开发分支，允许直接推送
- `feature/**` - 功能分支，合并到 `develop`

## GitHub 分支保护设置

建议在 GitHub 仓库设置中配置以下分支保护规则：

### Main 分支保护

```
分支名称模式: main
☑️ Restrict pushes that create files
☑️ Require a pull request before merging
  ☑️ Require approvals (建议至少 1 个)
  ☑️ Dismiss stale PR approvals when new commits are pushed
  ☑️ Require review from code owners
☑️ Require status checks to pass before merging
☑️ Require branches to be up to date before merging
☑️ Require conversation resolution before merging
☑️ Include administrators
```

### Release 分支保护

```
分支名称模式: release/**
☑️ Restrict pushes that create files
☑️ Require a pull request before merging
  ☑️ Require approvals (建议至少 1 个)
☑️ Require status checks to pass before merging
☑️ Include administrators
```

## 工作流程

### 日常开发

```bash
# 在 develop 分支开发
git checkout develop
git pull origin develop
git commit -m "Add feature +semver: minor"
git push origin develop  # ✅ 不触发 CI，节省资源
```

### 准备发布候选版本

```bash
# 创建 release 分支
git checkout develop
git checkout -b release/1.0.0
git push -u origin release/1.0.0

# 创建 PR: release/1.0.0 → release/1.0.0 (自合并)
# 合并后自动触发 CI，生成 1.0.0-rc.1 版本
```

### 正式发布

```bash
# 创建 PR: develop → main
# 合并后自动触发 CI，生成 1.0.0 正式版本
```

## CI/CD 触发条件

- ✅ **PR 合并到 main/release** → 触发完整构建和发布流程
- ✅ **手动触发** → 允许紧急发布
- ❌ **develop 分支推送** → 不触发 CI
- ❌ **直接推送到 main/release** → 被分支保护阻止

## 版本生成规则

- **main 分支合并** → 生成稳定版本 (如 `1.0.0`)
- **release 分支合并** → 生成候选版本 (如 `1.0.0-rc.1`)
- **develop 分支** → 本地构建时生成开发版本 (如 `1.1.0-alpha.1+5`)

这种策略确保了：

1. 🛡️ 生产分支的安全性
2. 💰 CI 资源的有效利用
3. 🔄 清晰的发布流程
4. 📦 自动化的版本管理

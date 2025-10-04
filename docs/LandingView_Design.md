# LandingView 设计说明

## 概述

LandingView 是 Polymerium 应用程序的主界面/欢迎页面，当用户打开应用且没有选中任何实例时显示。这个页面的设计目标是：

1. **欢迎用户** - 提供友好的第一印象
2. **引导操作** - 清晰地展示主要功能入口
3. **传达价值** - 突出 Polymerium 的核心特性
4. **保持一致** - 符合应用整体的设计风格

## 设计理念

### 视觉风格

- **现代简洁**：使用 Huskui.Avalonia 的现代化 UI 组件
- **品牌一致**：延续 Ghost（幽灵）主题，友好而有趣
- **层次清晰**：通过间距、大小和颜色建立视觉层次
- **响应式**：适应不同窗口大小

### 布局结构

```
┌─────────────────────────────────────────────────────┐
│                    Header Section                    │
│              [Ghost Icon with Shadow]                │
│            Welcome to Polymerium (32px)              │
│     Next-generation Minecraft instance manager       │
│                                                       │
├─────────────────────────────────────────────────────┤
│                  Main Content Area                   │
│  ┌───────────┐  ┌───────────┐  ┌───────────┐       │
│  │  Create   │  │  Browse   │  │  Import   │       │
│  │ Instance  │  │Marketplace│  │ Modpack   │       │
│  │           │  │           │  │           │       │
│  │  [Icon]   │  │  [Icon]   │  │  [Icon]   │       │
│  │   Text    │  │   Text    │  │   Text    │       │
│  └───────────┘  └───────────┘  └───────────┘       │
│                                                       │
├─────────────────────────────────────────────────────┤
│                   Footer Section                     │
│        [Feature Pills: Tags showing features]        │
│              [Info Bar with Getting Started]         │
└─────────────────────────────────────────────────────┘
```

## 组件详解

### 1. Header Section（顶部区域）

**Ghost Icon**

- 尺寸：96x96 像素
- 效果：添加阴影效果（DropShadowEffect）增加深度感
- 透明度：0.9，保持柔和感

**欢迎文字**

- 主标题："Welcome to Polymerium"
    - 字体大小：32px
    - 字重：SemiBold
    - 颜色：主要文本颜色
- 副标题："Next-generation Minecraft instance manager"
    - 字体大小：14px
    - 透明度：0.7
    - 颜色：次要文本颜色

### 2. Main Content - Quick Actions（主要内容区）

三个等宽的卡片，每个代表一个主要功能：

#### Card 1: Create Instance（创建实例）

- **图标**：SquarePlus（方形加号）
- **背景色**：AccentFillColorDefaultBrush（强调色）
- **功能**：引导用户创建新的 Minecraft 实例
- **导航**：跳转到 NewInstanceView

#### Card 2: Browse Marketplace（浏览市场）

- **图标**：Store（商店）
- **背景色**：SuccessFillColorDefaultBrush（成功色/绿色）
- **功能**：浏览 CurseForge 和 Modrinth 的模组和整合包
- **导航**：跳转到 MarketplacePortalView

#### Card 3: Import Modpack（导入整合包）

- **图标**：PackageOpen（打开的包裹）
- **背景色**：InfoFillColorDefaultBrush（信息色/蓝色）
- **功能**：从文件或 URL 导入整合包
- **导航**：跳转到 NewInstanceView

**卡片设计特点**：

- 使用 `Hoverable` 类，提供悬停反馈
- 图标容器：64x64 圆角矩形（12px 圆角）
- 图标：32x32 白色
- 标题：18px SemiBold
- 描述：12px，透明度 0.7，最大宽度 180px

### 3. Footer Section（底部区域）

**Feature Pills（特性标签）**

- 使用 `husk:Tag` 组件
- 展示 Polymerium 的核心特性：
    - 🎯 Smart Resource Management（智能资源管理）
    - 📦 Metadata-Driven（元数据驱动）
    - 🔗 Symlink Magic（符号链接魔法）
    - ⚡ Instant Switching（即时切换）
- 使用 Emoji 增加视觉趣味性

**Getting Started Info Bar**

- 类型：Info（信息提示）
- 不可关闭（IsClosable="False"）
- 最大宽度：600px
- 内容：引导用户从侧边栏选择实例或创建新实例

## 交互设计

### 用户流程

```
用户打开应用
    ↓
显示 LandingView
    ↓
用户看到三个主要选项
    ↓
┌─────────┬──────────────┬──────────┐
│创建实例  │ 浏览市场      │ 导入整合包│
└─────────┴──────────────┴──────────┘
    ↓           ↓              ↓
NewInstance  Marketplace   NewInstance
   View         Portal         View
                 View
```

### 命令绑定

在 `LandingViewModel` 中实现了三个 RelayCommand：

1. **CreateInstanceCommand**
    - 导航到 `NewInstanceView`
    - 允许用户创建全新的实例

2. **BrowseMarketplaceCommand**
    - 导航到 `MarketplacePortalView`
    - 浏览和搜索模组、整合包

3. **ImportModpackCommand**
    - 导航到 `NewInstanceView`
    - 可以在该页面导入整合包文件

## 设计原则

### 1. 以用户为中心

- 清晰的视觉层次引导用户注意力
- 简单直接的操作入口，降低学习成本
- 友好的文案和图标，减少认知负担

### 2. 品牌一致性

- 使用 Ghost 图标保持品牌识别
- 遵循 Huskui 设计系统的颜色和组件规范
- 与应用其他页面保持一致的视觉语言

### 3. 功能优先

- 突出最常用的三个功能
- 每个功能都有清晰的视觉标识（颜色+图标）
- 提供足够的上下文信息（描述文字）

### 4. 可扩展性

- 模块化的卡片设计，便于未来添加新功能
- 使用 Grid 布局，易于调整和响应式适配
- 特性标签可以根据版本更新轻松修改

## 技术实现

### XAML 结构

```xml
<husk:Page>
  <Grid MaxWidth="1200">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto" />   <!-- Header -->
      <RowDefinition Height="*" />      <!-- Main Content -->
      <RowDefinition Height="Auto" />   <!-- Footer -->
    </Grid.RowDefinitions>

    <!-- Header: Logo + Welcome Text -->
    <!-- Main: 3 Action Cards -->
    <!-- Footer: Feature Tags + Info Bar -->
  </Grid>
</husk:Page>
```

### ViewModel 模式

- 使用 MVVM 模式
- 通过 `NavigationService` 进行页面导航
- 使用 `RelayCommand` 处理用户交互
- 在 `OnInitializeAsync` 中清除导航历史，确保这是根页面

### 样式和主题

- 使用 Huskui 的预定义主题和颜色
- 利用动态资源（DynamicResource）支持主题切换
- 使用 `GhostButtonTheme` 实现无边框的卡片按钮效果

## 未来改进方向

1. **动画效果**
    - 添加页面进入动画
    - 卡片悬停时的微动画
    - 图标的呼吸效果

2. **个性化**
    - 根据用户使用习惯调整卡片顺序
    - 显示最近使用的实例快捷方式
    - 自定义欢迎消息

3. **信息展示**
    - 显示统计信息（实例数量、模组数量等）
    - 展示最新更新或公告
    - 添加快速教程或提示

4. **快捷操作**
    - 拖放文件导入整合包
    - 快速搜索框
    - 键盘快捷键支持

## 总结

这个 LandingView 设计充分考虑了 Polymerium 作为 Minecraft 实例管理器的特点：

- ✅ **清晰的功能入口**：三个主要功能一目了然
- ✅ **友好的视觉设计**：现代、简洁、有趣
- ✅ **符合品牌调性**：Ghost 主题，轻量化理念
- ✅ **良好的用户体验**：引导明确，操作简单
- ✅ **技术实现优雅**：MVVM 模式，可维护性强

这个设计将 Polymerium 从一个"空空如也"的页面转变为一个功能完整、引导清晰的应用入口，真正发挥了主界面应有的作用。


# 资源包功能实现说明

## 概述

本次实现完成了 InstanceAssetsView 页面中第二个 TabItem（资源包）的展示功能，参考了 AssetModHelper 的实现方式。

## 新增文件

### 1. Models

- **AssetResourcePackMetadataModel.cs** - 资源包元数据模型
    - `Description`: 资源包描述
    - `PackFormat`: 资源包格式版本号

- **AssetResourcePackModel.cs** - 资源包展示模型
    - 包含文件信息、图标、元数据等
    - 支持启用/禁用状态切换
    - 显示文件大小、修改时间等信息

### 2. Utilities

- **AssetResourcePackHelper.cs** - 资源包解析辅助类
    - `ParseMetadata()`: 从 zip 文件中解析 pack.mcmeta
    - `ExtractIcon()`: 从 zip 文件中提取 pack.png 图标
    - 支持解析 pack_format 和 description 字段

## 修改文件

### 1. ViewModels/InstanceAssetsViewModel.cs

新增功能：

- 资源包集合属性 `ResourcePacks`
- 选中的资源包 `SelectedResourcePack`
- 资源包搜索文本 `ResourcePackSearchText`
- `LoadResourcePacksAsync()`: 加载资源包列表
- `BuildResourcePackSearchFilter()`: 资源包搜索过滤器
- `ToggleResourcePackCommand`: 启用/禁用资源包
- `DeleteResourcePackCommand`: 删除资源包
- 使用 SourceCache 和 DynamicData 实现响应式过滤

### 2. Views/InstanceAssetsView.axaml

新增 UI：

- 完整的资源包 TabItem 实现
- 左侧：资源包列表
    - 搜索框
    - 资源包列表（显示图标、名称、格式、大小）
    - 状态指示器（启用/禁用/锁定）
- 右侧：详细信息面板
    - 资源包头部（大图标、名称、标签）
    - 操作按钮（启用/禁用、打开文件夹、删除）
    - 描述卡片
    - 详细信息卡片（文件名、格式、大小、修改时间、状态）
- 空状态提示

## 技术实现

### 资源包扫描

- 扫描 `resourcepacks` 目录下的 `.zip` 和 `.zip.disabled` 文件
- 使用 `AssetHelper.ScanNonSymlinks()` 扫描三个目录（build/import/persist）
- 自动识别导入的资源包（标记为锁定状态）

### 元数据解析

- 从 zip 文件中读取 `pack.mcmeta` 文件
- 解析 JSON 格式的元数据
- 提取 pack_format 和 description 字段
- 支持复杂的文本组件格式

### 图标提取

- 从 zip 文件中提取 `pack.png` 作为图标
- 如果没有图标，使用默认的 DirtImageBitmap

### 启用/禁用功能

- 通过重命名文件实现（添加/移除 `.disabled` 后缀）
- 与 Mod 的实现方式一致
- 锁定的资源包不允许切换状态

### 搜索过滤

- 支持按名称搜索
- 支持按描述内容搜索
- 实时响应式过滤

## UI 特性

1. **响应式设计**
    - 使用 DynamicData 实现数据绑定
    - 搜索框实时过滤
    - 状态变化自动更新

2. **视觉反馈**
    - 绿色圆点：已启用
    - 红色圆点：已禁用
    - 黄色圆点：已锁定（来自导入）

3. **用户体验**
    - 左右分栏布局，可调整大小
    - 空状态提示
    - 删除前确认对话框
    - 操作成功/失败通知

## 与 Mod 功能的一致性

本实现参考了 AssetModHelper 的设计模式：

- 相似的模型结构（Model + MetadataModel）
- 相同的辅助类模式（Helper 类）
- 一致的 UI 布局和交互
- 统一的命令实现方式
- 相同的过滤和搜索机制

## 使用说明

1. 资源包文件应放置在实例的 `resourcepacks` 目录下
2. 支持的文件格式：`.zip`
3. 禁用资源包会自动添加 `.disabled` 后缀
4. 导入的资源包会显示锁定标签，不可删除或切换状态
5. 点击资源包可查看详细信息
6. 使用搜索框可快速查找资源包


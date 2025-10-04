# LandingView 快速参考

## 🎯 一句话总结

将 Polymerium 的主界面从简单的"Boo!"占位页面重新设计为功能完整、美观实用的应用入口，包含三个主要功能卡片和特性展示。

## 📁 修改的文件

| 文件                                                  | 类型 | 说明                       |
|-----------------------------------------------------|----|--------------------------|
| `src/Polymerium.App/Views/LandingView.axaml`        | 修改 | 界面布局（从 13 行扩展到 211 行）    |
| `src/Polymerium.App/ViewModels/LandingViewModel.cs` | 修改 | 添加三个导航命令（从 20 行扩展到 44 行） |
| `docs/LandingView_Design.md`                        | 新建 | 详细设计文档                   |
| `docs/LandingView_Implementation_Summary.md`        | 新建 | 实现总结                     |
| `docs/LandingView_QuickReference.md`                | 新建 | 本文档                      |

## 🎨 界面布局

### Header（顶部）

- Ghost 图标（96x96，带阴影）
- 欢迎标题："Welcome to Polymerium"（32px）
- 副标题："Next-generation Minecraft instance manager"（14px）

### Main Content（中部）

三个等宽功能卡片：

| 卡片                 | 图标          | 颜色 | 功能    | 导航目标                  |
|--------------------|-------------|----|-------|-----------------------|
| Create Instance    | SquarePlus  | 蓝色 | 创建新实例 | NewInstanceView       |
| Browse Marketplace | Store       | 绿色 | 浏览市场  | MarketplacePortalView |
| Import Modpack     | PackageOpen | 橙色 | 导入整合包 | NewInstanceView       |

### Footer（底部）

- 特性标签：🎯 Smart Resource | 📦 Metadata-Driven | 🔗 Symlink Magic | ⚡ Instant Switching
- 提示信息：引导用户从侧边栏选择实例或创建新实例

## 💻 代码示例

### ViewModel 命令

```csharp
[RelayCommand]
private void CreateInstance()
{
    navigationService.Navigate<NewInstanceView>();
}

[RelayCommand]
private void BrowseMarketplace()
{
    navigationService.Navigate<MarketplacePortalView>();
}

[RelayCommand]
private void ImportModpack()
{
    navigationService.Navigate<NewInstanceView>();
}
```

### XAML 绑定

```xml
<Button Command="{Binding CreateInstanceCommand}">
    <!-- 按钮内容 -->
</Button>
```

## 🎯 设计要点

### 视觉

- ✅ 使用 Huskui Card 组件
- ✅ 颜色编码区分功能
- ✅ 图标 + 文字双重说明
- ✅ 阴影和透明度增加层次

### 交互

- ✅ 全卡片可点击
- ✅ Hoverable 悬停效果
- ✅ 清晰的视觉反馈
- ✅ 简洁的导航流程

### 技术

- ✅ MVVM 模式
- ✅ RelayCommand 命令
- ✅ NavigationService 导航
- ✅ 数据绑定

## 🔍 关键组件

### Huskui 组件

- `husk:Page` - 页面容器
- `husk:Card` - 卡片容器
- `husk:Tag` - 标签组件
- `husk:InfoBar` - 信息提示条

### 图标库

- `icons:PackIconLucide` - Lucide 图标集
- 使用的图标：Ghost, SquarePlus, Store, PackageOpen

### 布局

- `Grid` - 主布局容器（3 行）
- `StackPanel` - 垂直/水平堆叠
- `WrapPanel` - 自动换行布局

## 📊 数据流

```
用户点击卡片
    ↓
触发 Command
    ↓
ViewModel 处理
    ↓
调用 NavigationService
    ↓
导航到目标页面
```

## 🎨 颜色方案

| 元素      | 颜色资源                         | 用途      |
|---------|------------------------------|---------|
| 创建实例卡片  | AccentFillColorDefaultBrush  | 强调主要功能  |
| 浏览市场卡片  | SuccessFillColorDefaultBrush | 表示探索和发现 |
| 导入整合包卡片 | InfoFillColorDefaultBrush    | 表示信息和导入 |
| 主标题     | TextFillColorPrimaryBrush    | 主要文本    |
| 副标题     | TextFillColorSecondaryBrush  | 次要文本    |

## 🚀 测试建议

### 功能测试

- [ ] 点击"Create Instance"跳转到 NewInstanceView
- [ ] 点击"Browse Marketplace"跳转到 MarketplacePortalView
- [ ] 点击"Import Modpack"跳转到 NewInstanceView
- [ ] 页面初始化时清除导航历史

### 视觉测试

- [ ] Ghost 图标正确显示
- [ ] 阴影效果正常
- [ ] 卡片悬停效果正常
- [ ] 文字对齐和间距正确
- [ ] 响应式布局在不同窗口大小下正常

### 主题测试

- [ ] 亮色主题下显示正常
- [ ] 暗色主题下显示正常
- [ ] 颜色对比度足够
- [ ] 图标清晰可见

## 🔧 故障排除

### 常见问题

**Q: 命令没有触发？**
A: 检查 ViewModel 是否使用 `partial` 关键字，确保 RelayCommand 源生成器正常工作。

**Q: 导航失败？**
A: 确保 NavigationService 已正确注入，目标 View 已注册到 DI 容器。

**Q: 图标不显示？**
A: 检查 IconPacks.Avalonia.Lucide 包是否已引用，XAML 命名空间是否正确。

**Q: 样式不生效？**
A: 确保 Huskui.Avalonia 主题已在 App.axaml 中引用。

## 📝 维护建议

### 本地化

将硬编码文本移到资源文件：

```csharp
// 当前
Text="Welcome to Polymerium"

// 建议
Text="{x:Static lang:Resources.LandingView_WelcomeTitle}"
```

### 可配置性

考虑将卡片配置提取到数据模型：

```csharp
public record ActionCard(
    string Title,
    string Description,
    string Icon,
    string Color,
    ICommand Command
);
```

### 性能优化

- 使用虚拟化（如果卡片数量增加）
- 延迟加载图片资源
- 缓存导航参数

## 📚 相关资源

- [Avalonia UI 文档](https://docs.avaloniaui.net/)
- [Huskui.Avalonia](https://github.com/d3ara1n/Huskui.Avalonia)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Lucide Icons](https://lucide.dev/)

## ✅ 检查清单

在提交代码前，确保：

- [ ] 代码编译通过
- [ ] 所有命令正常工作
- [ ] 导航流程正确
- [ ] 视觉效果符合设计
- [ ] 响应式布局正常
- [ ] 主题切换正常
- [ ] 无控制台错误
- [ ] 代码格式化
- [ ] 注释清晰
- [ ] 文档更新

---

**最后更新**: 2025-10-04
**版本**: 1.0
**状态**: ✅ 完成


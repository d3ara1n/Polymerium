# Java Runtime 扫描与检测

为 Polymerium.App 的设置界面中添加扫描计算机中原装的 Java Runtime 并选择功能。

## 扫描

请在 `TridentCore.Core.Utilities.JavaHelper` 中 添加 ScanJavaRuntimes 的能力，该函数是多平台（win/linux/macos）的函数代理入口。通过判断当前平台进入底层实现，只对 Windows 和 macOS 做匹配，其他的都进入 `ScanJavaRuntimesNull()` 。

### Windows 平台

引入 `Microsoft.Win32.Registry` 库，使用注册表扫描几个常用的 Jre（或JDK） 位置并提供选择。

`ScanJavaRuntimesWindows()`

### Linux 平台

不提供该功能。

`ScanJavaRuntimesNull()` 无论如何都返回一个空集。

### macOs 平台

使用系统提供的 java_home 工具来实现定位。

`ScanJavaRuntimesMacOS()`

### 模型定义

期间用到的领域模型之类的都作为 `JavaHelper` 的嵌套公开类。

## 界面

位于 Polymerium.App。

### 设置界面

每个 Java Runtime Slot 的检测按钮的 IsVisible = {Platform Linux={x:False}, Default={x:True}} 以此隐藏 Linux 的检测入口。

其他平台点击改按钮会弹出检测与选择的 RuntimePickerDialog。

### 运行时选择对话框

这个 Dialog 使用新的设计方式，有别于其他 Dialog，它具有 ViewModel DialogModels/RuntimePickerDialogModel，关于让一个 Overlay 元素具有 ViewModel 可以参考同样具有 ViewModel 的另一个 Overlay 元素：WorkspaceDiffModal。

当拥有了 ViewModel 也就有了生命周期管理能力。可以通过这个实现初始化数据的加载（即扫描）。内部维护一个静态的变量来储存上次的扫描结果，如果初始化时该集合为 null，即从未扫描，那么界面就显示扫描中的页面。

#### 界面设计

使用 `PlaceholderContainer` 实现不同状态的显示，通过将值绑定到Runtime候选的集合，如果是 null，此时显示 Placeholder 内容，即等待提示等界面元素。当 Runtime候选集合被赋值，则使用 EmptyContainer 来展示，当集合为空，就显示空提示内容，集合非空则提供选择引导用户选择一个候选项并将其绑定到 Result（关于 Result 的用法你可以查看 Dialog 这个控件的源码）。

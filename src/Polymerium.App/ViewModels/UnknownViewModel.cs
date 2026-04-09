using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Facilities;
using Polymerium.App.Modals;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels;

public partial class UnknownViewModel(
    ViewBag bag,
    NotificationService notificationService,
    OverlayService overlayService,
    ConfigurationService configurationService
) : ViewModelBase
{
    public string Title { get; } = $"User's Unknown Playground({bag.Parameter ?? "None"})";

    #region Overrides

    protected override async Task OnInitializeAsync()
    {
        if (Application.Current is { PlatformSettings: not null })
        {
            var accent1 = Application.Current.PlatformSettings.GetColorValues().AccentColor1;
            var accent2 = Application.Current.PlatformSettings.GetColorValues().AccentColor2;
            var accent3 = Application.Current.PlatformSettings.GetColorValues().AccentColor3;

            Accent1Brush = new SolidColorBrush(accent1);
        }

        NotificationActions.AddRange([
            new("Information", ShowInformationCommand),
            new("Success", ShowSuccessCommand),
            new("Warning", ShowWarningCommand),
            new("Danger", ShowDangerCommand),
        ]);

        await Task.Delay(TimeSpan.FromSeconds(7), PageToken);
    }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial IBrush Accent1Brush { get; set; } = Brushes.Black;

    [ObservableProperty]
    public partial IBrush Accent2Brush { get; set; } = Brushes.Black;

    [ObservableProperty]
    public partial IBrush Accent3Brush { get; set; } = Brushes.Black;

    [ObservableProperty]
    public partial AvaloniaList<GrowlAction> NotificationActions { get; set; } = [];

    [ObservableProperty]
    public partial string MarkdownContent { get; set; } = GetSampleMarkdown();

    #endregion

    #region Helpers

    private static string GetSampleMarkdown() =>
        """
# Markdown 测试文档

这是一个用于测试 Markdown 渲染的示例文档，包含常用样式。

## 0. 二级标题

### 0.0 三级标题

#### 0.0.0 四级标题

##### 0.0.0.0 五级标题

###### 0.0.0.0.0 六级标题

## 1. 文本样式

**粗体文本** 和 *斜体文本* 以及 ***粗斜体文本***

~~删除线文本~~

`行内代码`

## 2. 列表

### 无序列表
- 第一项
- 第二项
  - 嵌套项 1
  - 嵌套项 2
- 第三项

### 有序列表
1. 第一步
2. 第二步
3. 第三步

## 3. 链接和图片

[访问 GitHub](https://github.com)

## 4. 引用

> 这是一段引用文本
>
> 可以包含多行

## 5. 代码块

```csharp
public class HelloWorld
{
    public void SayHello()
    {
        Console.WriteLine("Hello, World!");
    }
}
```

## 6. 表格

| 名称 | 年龄 | 职业 |
|------|------|------|
| 张三 | 25 | 工程师 |
| 李四 | 30 | 设计师 |
| 王五 | 28 | 产品经理 |

## 7. 分隔线

---

## 8. 任务列表

- [x] 已完成任务
- [ ] 未完成任务
- [ ] 另一个未完成任务

## **9. 混合内容**

这是一个包含 **粗体**、*斜体*、`代码` 和 [链接](https://example.com) 的段落。

> 引用中也可以包含 **粗体** 和 *斜体*。

## 10. 特殊字符

© 2024 测试文档 &reg; &trade; &lt; &gt; &amp;

---

*文档结束*
""";

    #endregion

    #region Commands

    [RelayCommand]
    private void ShowInformation() =>
        notificationService.PopMessage(
            "Hello Hi~Hello Hi~Hello Hi~Hello Hi~Hello Hi~Hello Hi~Hello Hi~Hello Hi~Hello Hi~Hello Hi~Hello Hi~Hello Hi~Hello Hi~Hello Hi~Hello Hi~Hello Hi~Hello Hi~",
            "Hi there!"
        );

    [RelayCommand]
    private void ShowSuccess() =>
        notificationService.PopMessage("Hello", "Hi there!", GrowlLevel.Success);

    [RelayCommand]
    private void ShowWarning() =>
        notificationService.PopMessage("Hello", "Hi there!", GrowlLevel.Warning);

    [RelayCommand]
    private void ShowDanger()
    {
        notificationService.PopProgress(
            "Hello",
            "Hi there!",
            GrowlLevel.Danger,
            actions: new GrowlAction("Info", new RelayCommand(ShowInformation))
        );
    }

    [RelayCommand]
    private void ShowToast()
    {
        PopToast();
        return;

        void PopToast()
        {
            var pop = new Button { Content = "POP" };
            pop.Click += (_, __) => PopToast();
            overlayService.PopToast(
                new()
                {
                    Header = $"A VERY LONG TOAST TITLE {Random.Shared.Next(1000, 9999)}",
                    Content = new StackPanel
                    {
                        Spacing = 8d,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "ALIVE OR DEAD VERY LONG MESSAGE THAT DONT TRIM",
                            },
                            new TextBox(),
                            pop,
                        },
                    },
                }
            );
        }
    }

    [RelayCommand]
    private void ShowDrawer()
    {
        PopDrawer();
        return;

        void PopDrawer()
        {
            var drawer = new Sidebar();
            var pop = new Button { Content = "POP" };
            var dismiss = new Button { Content = "DISMISS" };
            pop.Click += (_, __) => PopDrawer();
            dismiss.Click += (_, __) => drawer.Dismiss();
            drawer.Content = new StackPanel
            {
                Spacing = 8d,
                Children =
                {
                    new TextBox { Text = $"DRAWER {Random.Shared.Next(1000, 9999)}" },
                    pop,
                    dismiss,
                },
            };
            overlayService.PopSidebar(drawer);
        }
    }

    [RelayCommand]
    private void ShowModal()
    {
        PopModal();
        return;

        void PopModal()
        {
            var modal = new Modal();
            var pop = new Button { Content = "POP" };
            var dismiss = new Button { Content = "DISMISS" };
            pop.Click += (_, __) => PopModal();
            dismiss.Click += (_, __) => modal.Dismiss();
            modal.Content = new StackPanel
            {
                Spacing = 8d,
                Children =
                {
                    new TextBox { Text = $"MODAL {Random.Shared.Next(1000, 9999)}" },
                    pop,
                    dismiss,
                },
            };
            overlayService.PopModal(modal);
        }
    }

    [RelayCommand]
    private void ShowDialog()
    {
        PopDialog();
        return;

        void PopDialog()
        {
            var pop = new Button { Content = "POP" };
            pop.Click += (_, __) => PopDialog();
            overlayService.PopDialog(
                new()
                {
                    IsPrimaryButtonVisible = true,
                    Title = $"DIALOG {Random.Shared.Next(1000, 9999)}",
                    Message = "ALIVE OR DEAD VERY LONG MESSAGE THAT DONT TRIM",
                    Result = Program.MagicWords,
                    Content = new StackPanel { Spacing = 8d, Children = { new TextBox(), pop } },
                }
            );
        }
    }

    [RelayCommand]
    private void Crash() => throw new NotImplementedException("The sun is leaking...");

    [RelayCommand]
    private void Debug() =>
        overlayService.PopModal(
            new ProfileRuleModal
            {
                Rule = new(new() { Selector = new() }),
                OverlayService = overlayService,
                Packages = [],
            }
        );

    [RelayCommand]
    private void ShowIntro() =>
        overlayService.PopModal(
            new OobeModal
            {
                ConfigurationService = configurationService,
                OverlayService = overlayService,
                NotificationService = notificationService,
            }
        );

    [RelayCommand]
    private void ShowDiagnosis() =>
        overlayService.PopModal(
            new GameCrashReportModal
            {
                Report = new()
                {
                    CrashReportPath =
                        "C:\\Users\\HuskyT\\Desktop\\crash-2023-09-19_17.17.57-client.txt",
                    GameDirectory = "C:\\Users\\HuskyT\\AppData\\Roaming\\.minecraft",
                    InstanceKey = "cherry_picks",
                    InstanceName = "Cherry Picks",
                    LaunchTime = DateTimeOffset.Now.AddHours(-1),
                    CrashTime = DateTimeOffset.Now,
                    ExceptionMessage = "I USED TO RULE THE WORLD",
                    MinecraftVersion = "1.19.2",
                    LoaderLabel = "Fabric 0.14.12",
                    OperatingSystem = "Windows 10",
                    InstalledMemory =
                        $"{double.Round((double)GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024 / 1024)} GB",
                    JavaVersion = "17",
                    JavaPath = "C:\\Program Files\\Java\\jdk-17.0.8",
                    AllocatedMemory = "8 GB",
                    LogFilePath =
                        "C:\\Users\\HuskyT\\AppData\\Roaming\\.minecraft\\logs\\latest.log",
                    LastLogLines = "ALIVE OR DEAD VERY LONG LOG THAT DONT TRIM",
                    ModCount = 10,
                    CommandLine = "java -jar fabric-loader-0.14.12.jar",
                    ExitCode = 1,
                },
            }
        );

    #endregion
}

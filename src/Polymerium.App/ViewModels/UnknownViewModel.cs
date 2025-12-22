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
    ConfigurationService configurationService) : ViewModelBase
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
            new("Danger", ShowDangerCommand)
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

    #endregion

    #region Commands

    [RelayCommand]
    private void ShowInformation() => notificationService.PopMessage("Hello", "Hi there!");

    [RelayCommand]
    private void ShowSuccess() => notificationService.PopMessage("Hello", "Hi there!", GrowlLevel.Success);

    [RelayCommand]
    private void ShowWarning() => notificationService.PopMessage("Hello", "Hi there!", GrowlLevel.Warning);

    [RelayCommand]
    private void ShowDanger() => notificationService.PopMessage("Hello", "Hi there!", GrowlLevel.Danger);

    [RelayCommand]
    private void ShowToast()
    {
        PopToast();
        return;

        void PopToast()
        {
            var pop = new Button { Content = "POP" };
            pop.Click += (_, __) => PopToast();
            overlayService.PopToast(new()
            {
                Header = $"A VERY LONG TOAST TITLE {Random.Shared.Next(1000, 9999)}",
                Content = new StackPanel
                {
                    Spacing = 8d,
                    Children =
                    {
                        new TextBlock
                        {
                            Text =
                                "ALIVE OR DEAD VERY LONG MESSAGE THAT DONT TRIM"
                        },
                        new TextBox(),
                        pop
                    }
                }
            });
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
                Children = { new TextBox { Text = $"DRAWER {Random.Shared.Next(1000, 9999)}" }, pop, dismiss }
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
                Children = { new TextBox { Text = $"MODAL {Random.Shared.Next(1000, 9999)}" }, pop, dismiss }
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
            overlayService.PopDialog(new()
            {
                Title = $"DIALOG {Random.Shared.Next(1000, 9999)}",
                Message = "ALIVE OR DEAD VERY LONG MESSAGE THAT DONT TRIM",
                Content = new StackPanel
                {
                    Spacing = 8d, Children = { new TextBox(), pop }
                }
            });
        }
    }

    [RelayCommand]
    private void Debug() => throw new NotImplementedException("The sun is leaking...");

    [RelayCommand]
    private void ShowIntro()
    {
        overlayService.PopModal(new OobeModal
        {
            ConfigurationService = configurationService,
            OverlayService = overlayService,
            NotificationService = notificationService
        });
    }

    [RelayCommand]
    private void ShowDiagnosis()
    {
        overlayService.PopModal(new GameCrashReportModal
        {
            Report = new()
            {
                CrashReportPath =
                    "C:\\Users\\HuskyT\\Desktop\\crash-2023-09-19_17.17.57-client.txt",
                GameDirectory = "C:\\Users\\HuskyT\\AppData\\Roaming\\.minecraft",
                InstanceKey = "HuskyT",
                InstanceName = "HuskyT",
                LaunchTime = DateTimeOffset.Now.AddHours(-1),
                CrashTime = DateTimeOffset.Now,
                ExceptionMessage = "I USED TO RULE THE WORLD",
                MinecraftVersion = "1.19.2",
                LoaderLabel = "Fabric 0.14.12",
                OperatingSystem = "Windows 10",
                JavaVersion = "17",
                JavaPath = "C:\\Program Files\\Java\\jdk-17.0.8",
                AllocatedMemory = "8GB",
                LogFilePath =
                    "C:\\Users\\HuskyT\\AppData\\Roaming\\.minecraft\\logs\\latest.log",
                LastLogLines = "ALIVE OR DEAD VERY LONG LOG THAT DONT TRIM",
                ModCount = 10,
                CommandLine = "java -jar fabric-loader-0.14.12.jar",
                ExitCode = 1
            }
        });
    }

    #endregion
}

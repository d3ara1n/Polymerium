﻿using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia;
using Huskui.Avalonia.Models;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Velopack;

namespace Polymerium.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    public SettingsViewModel(
        ConfigurationService configurationService,
        OverlayService overlayService,
        NavigationService navigationService,
        NotificationService notificationService,
        UpdateManager updateManager)
    {
        OverlayService = overlayService;
        _configurationService = configurationService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        _updateManager = updateManager;

        SuperPowerActivated = configurationService.Value.ApplicationSuperPowerActivated;
        TitleBarVisibility = configurationService.Value.ApplicationTitleBarVisibility;
        SidebarPlacement = configurationService.Value.ApplicationLeftPanelMode ? 0 : 1;
        AccentColor = configurationService.Value.ApplicationStyleAccent;
        CornerStyle = configurationService.Value.ApplicationStyleCorner;
        BackgroundMode = configurationService.Value.ApplicationStyleBackground;
        DarkMode = configurationService.Value.ApplicationStyleThemeVariant;
        Language = Languages.FirstOrDefault(x => x.Id == configurationService.Value.ApplicationLanguage)
                ?? Languages.First();
        JavaHome8 = configurationService.Value.RuntimeJavaHome8 != string.Empty
                        ? configurationService.Value.RuntimeJavaHome8
                        : null;
        JavaHome11 = configurationService.Value.RuntimeJavaHome11 != string.Empty
                         ? configurationService.Value.RuntimeJavaHome11
                         : null;
        JavaHome17 = configurationService.Value.RuntimeJavaHome17 != string.Empty
                         ? configurationService.Value.RuntimeJavaHome17
                         : null;
        JavaHome21 = configurationService.Value.RuntimeJavaHome21 != string.Empty
                         ? configurationService.Value.RuntimeJavaHome21
                         : null;
        JavaMaxMemory = configurationService.Value.GameJavaMaxMemory;
        JavaAdditionalArguments = configurationService.Value.GameJavaAdditionalArguments;
        WindowInitialWidth = configurationService.Value.GameWindowInitialWidth;
        WindowInitialHeight = configurationService.Value.GameWindowInitialHeight;
    }

    #region Service Export

    public OverlayService OverlayService { get; }

    #endregion

    #region Injected

    private readonly ConfigurationService _configurationService;
    private readonly NavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private readonly UpdateManager _updateManager;

    #endregion

    #region Commands

    [RelayCommand]
    private async Task PickFile(TextBox? box)
    {
        if (box != null)
        {
            var path = await OverlayService.RequestFileAsync("Pick a file like /bin/java.exe or /bin/javaw.exe",
                                                             "Select a Java executable");
            if (path != null && File.Exists(path))
            {
                var dir = Path.GetDirectoryName(Path.GetDirectoryName(path));
                if (dir != null)
                    box.Text = dir;
            }
        }
    }

    [RelayCommand]
    private void Navigate(Type? view)
    {
        if (view != null)
            _navigationService.Navigate(view);
    }

    private bool CanCheckUpdate() => UpdateState != AppUpdateState.Unavailable;

    [RelayCommand(CanExecute = nameof(CanCheckUpdate))]
    private async Task CheckUpdatesAsync()
    {
        try
        {
            var result = await _updateManager.CheckForUpdatesAsync();
            if (result != null)
            {
                UpdateTarget = new AppUpdateModel(result);
                UpdateState = AppUpdateState.Found;
            }
            else
            {
                UpdateState = AppUpdateState.Latest;
            }
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex, "Failed to check updates");
        }
    }

    private bool CanApplyUpdate() => UpdateTarget != null;

    [RelayCommand(CanExecute = nameof(CanApplyUpdate))]
    private async Task ApplyUpdateAsync(AppUpdateModel? model)
    {
        if (model == null)
            return;

        var progress = _notificationService.PopProgress("Downloading...", "Apply the update");
        try
        {
            // 即使处置了也没事，内部会处理空引用
            // ReSharper disable once AccessToDisposedClosure
            void Report(int value) => Dispatcher.UIThread.Post(() => progress.Report(value));
            await _updateManager.DownloadUpdatesAsync(model.Update, Report);
            _notificationService.PopMessage("Restart required to take effect",
                                            "Apply the update",
                                            actions:
                                            [
                                                new NotificationAction("Restart", RestartAndUpdateCommand, model)
                                            ]);
        }
        catch (Exception ex)
        {
            progress.Dispose();
            _notificationService.PopMessage(ex, "Failed to download update");
        }

        progress.Dispose();
    }

    private bool CanRestartAndUpdate(AppUpdateModel? model) => model is not null;

    [RelayCommand(CanExecute = nameof(CanRestartAndUpdate))]
    private void RestartAndUpdate(AppUpdateModel? model)
    {
        if (model != null)
            _updateManager.ApplyUpdatesAndRestart(model.Update);
    }

    #endregion

    #region Updates

    public string VersionString { get; } = typeof(Program)
                                          .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                         ?.InformationalVersion.Split("+")[0]
                                        ?? "Unknown";

    [ObservableProperty]
    public partial AppUpdateState UpdateState { get; set; } =
        Program.Debug ? AppUpdateState.Unavailable : AppUpdateState.Idle;

    [ObservableProperty]
    public partial AppUpdateModel? UpdateTarget { get; set; }

    #endregion

    #region SuperPowerActivated

    [ObservableProperty]

    public partial bool SuperPowerActivated { get; set; }

    partial void OnSuperPowerActivatedChanged(bool value) =>
        _configurationService.Value.ApplicationSuperPowerActivated = value;

    #endregion

    #region TitleBarVisibility

    [ObservableProperty]
    public partial bool TitleBarVisibility { get; set; }

    partial void OnTitleBarVisibilityChanged(bool value)
    {
        _configurationService.Value.ApplicationTitleBarVisibility = value;
        MainWindow.Instance.IsTitleBarVisible = value;
    }

    #endregion

    #region SidebarPlacement

    [ObservableProperty]
    public partial int SidebarPlacement { get; set; }

    partial void OnSidebarPlacementChanged(int value)
    {
        var rv = value == 0;
        _configurationService.Value.ApplicationLeftPanelMode = rv;
        MainWindow.Instance.IsLeftPanelMode = rv;
    }

    #endregion

    #region AccentColor

    [ObservableProperty]
    public partial AccentColor AccentColor { get; set; }

    partial void OnAccentColorChanged(AccentColor value)
    {
        _configurationService.Value.ApplicationStyleAccent = value;
        MainWindow.Instance.SetColorVariant(value);
    }

    public AccentColor[] AccentColors { get; } = Enum.GetValues<AccentColor>();

    #endregion

    #region CornerStyle

    [ObservableProperty]
    public partial CornerStyle CornerStyle { get; set; }

    partial void OnCornerStyleChanged(CornerStyle value)
    {
        _configurationService.Value.ApplicationStyleCorner = value;
        MainWindow.Instance.SetCornerStyle(value);
    }

    public CornerStyle[] CornerStyles { get; } = Enum.GetValues<CornerStyle>();

    #endregion

    #region BackgroundMode

    [ObservableProperty]
    public partial int BackgroundMode { get; set; }

    partial void OnBackgroundModeChanged(int value)
    {
        _configurationService.Value.ApplicationStyleBackground = value;
        MainWindow.Instance.SetTransparencyLevelHintByIndex(value);
    }

    #endregion

    #region DarkMode

    [ObservableProperty]
    public partial int DarkMode { get; set; }

    partial void OnDarkModeChanged(int value)
    {
        _configurationService.Value.ApplicationStyleThemeVariant = value;
        MainWindow.Instance.SetThemeVariantByIndex(value);
    }

    #endregion

    #region Language

    private static string[] SupportedLanguages { get; } = ["en-US", "zh-Hans"];

    public LanguageModel[] Languages { get; } = SupportedLanguages
                                               .Select(CultureInfo.GetCultureInfo)
                                               .Select(x => new LanguageModel(x))
                                               .ToArray();

    [ObservableProperty]
    public partial LanguageModel Language { get; set; }

    partial void OnLanguageChanged(LanguageModel value) => _configurationService.Value.ApplicationLanguage = value.Id;

    #endregion

    #region JavaHome

    [ObservableProperty]
    public partial string? JavaHome8 { get; set; }

    partial void OnJavaHome8Changed(string? value) =>
        _configurationService.Value.RuntimeJavaHome8 = value ?? string.Empty;

    [ObservableProperty]
    public partial string? JavaHome11 { get; set; }

    partial void OnJavaHome11Changed(string? value) =>
        _configurationService.Value.RuntimeJavaHome11 = value ?? string.Empty;

    [ObservableProperty]
    public partial string? JavaHome17 { get; set; }

    partial void OnJavaHome17Changed(string? value) =>
        _configurationService.Value.RuntimeJavaHome17 = value ?? string.Empty;

    [ObservableProperty]
    public partial string? JavaHome21 { get; set; }

    partial void OnJavaHome21Changed(string? value) =>
        _configurationService.Value.RuntimeJavaHome21 = value ?? string.Empty;

    #endregion

    #region Java Max Memory

    [ObservableProperty]
    public partial uint JavaMaxMemory { get; set; }

    partial void OnJavaMaxMemoryChanged(uint value) => _configurationService.Value.GameJavaMaxMemory = value;

    #endregion

    #region Java Additional Arguments

    [ObservableProperty]
    public partial string JavaAdditionalArguments { get; set; }

    partial void OnJavaAdditionalArgumentsChanged(string value) =>
        _configurationService.Value.GameJavaAdditionalArguments = value;

    #endregion

    #region Window Initial Width

    [ObservableProperty]
    public partial uint WindowInitialWidth { get; set; }

    partial void OnWindowInitialWidthChanged(uint value) => _configurationService.Value.GameWindowInitialWidth = value;

    #endregion

    #region Window Initial Height

    [ObservableProperty]
    public partial uint WindowInitialHeight { get; set; }

    partial void OnWindowInitialHeightChanged(uint value) =>
        _configurationService.Value.GameWindowInitialHeight = value;

    #endregion
}
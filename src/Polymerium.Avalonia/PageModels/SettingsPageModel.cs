using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia;
using Huskui.Avalonia.Models;
using Microsoft.Extensions.Logging;
using Polymerium.Avalonia.Dialogs;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Modals;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using TridentCore.Abstractions;
using Velopack;

namespace Polymerium.Avalonia.PageModels;

public partial class SettingsPageModel : ViewModelBase
{
    public SettingsPageModel(
        ILogger<SettingsPageModel> logger,
        ConfigurationService configurationService,
        OverlayService overlayService,
        NavigationService navigationService,
        NotificationService notificationService,
        PersistenceService persistenceService,
        UpdateService updateService,
        UpdateManager updateManager,
        GarbageCollector garbageCollector,
        ThemeService themeService,
        FontService fontService)
    {
        _logger = logger;
        OverlayService = overlayService;
        _configurationService = configurationService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        _persistenceService = persistenceService;
        UpdateService = updateService;
        _updateManager = updateManager;
        _garbageCollector = garbageCollector;
        _themeService = themeService;
        _fontService = fontService;

        SuperPowerActivated = configurationService.Value.ApplicationSuperPowerActivated;
        TitleBarVisibility = configurationService.Value.ApplicationTitleBarVisibility;
        SidebarPlacement = configurationService.Value.ApplicationLeftPanelMode ? 0 : 1;
        AccentColor = configurationService.Value.ApplicationStyleAccent;
        CornerStyle = configurationService.Value.ApplicationStyleCorner;
        BackgroundMode =
            BackgroundStyles.FirstOrDefault(x => x.Index == configurationService.Value.ApplicationStyleBackground)
         ?? BackgroundStyles.First();
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
        JavaHome25 = configurationService.Value.RuntimeJavaHome25 != string.Empty
                         ? configurationService.Value.RuntimeJavaHome25
                         : null;
        JavaMaxMemory = configurationService.Value.GameJavaMaxMemory;
        JavaAdditionalArguments = configurationService.Value.GameJavaAdditionalArguments;
        CommandWrapper = configurationService.Value.GameCommandWrapper;
        WindowInitialWidth = configurationService.Value.GameWindowInitialWidth;
        WindowInitialHeight = configurationService.Value.GameWindowInitialHeight;
        AutoCheckUpdates = configurationService.Value.UpdateAutoCheck;
        UpdateSource = configurationService.Value.UpdateSource;
        MirrorChyanCdk = configurationService.Value.UpdateMirrorChyanCdk;

        ProxyMode = TryConvertEnum<ProxyMode>(configurationService.Value.NetworkProxyMode);
        ProxyProtocol = TryConvertEnum<ProxyProtocol>(configurationService.Value.NetworkProxyProtocol);
        ProxyEnabled = configurationService.Value.NetworkProxyEnabled;
        ProxyAddress = configurationService.Value.NetworkProxyAddress;
        ProxyPort = configurationService.Value.NetworkProxyPort;
        ProxyUsername = configurationService.Value.NetworkProxyUsername;
        ProxyPassword = configurationService.Value.NetworkProxyPassword;

        MainFontSelection = _fontService.Main;
        CodeFontSelection = _fontService.Code;
        LogFontSelection = _fontService.Log;

        CrashReportingEnabled = !File.Exists(PathDef.Default.FileOfTelemetrySwitch());

        UpdateProxyStatusText();
        SyncUpdateState();
    }

    #region Service Export

    public OverlayService OverlayService { get; }
    public UpdateService UpdateService { get; }

    #endregion

    #region Injected

    private readonly ConfigurationService _configurationService;
    private readonly NavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private readonly PersistenceService _persistenceService;
    private readonly UpdateManager _updateManager;
    private readonly GarbageCollector _garbageCollector;
    private readonly ThemeService _themeService;
    private readonly FontService _fontService;
    private readonly ILogger _logger;

    #endregion

    #region Commands

    [RelayCommand]
    private void Navigate(Type? view)
    {
        if (view != null)
        {
            _navigationService.Navigate(view);
        }
    }

    private bool CanCheckUpdate() => UpdateService.CanCheckUpdate;

    [RelayCommand(CanExecute = nameof(CanCheckUpdate))]
    private async Task CheckUpdatesAsync()
    {
        try
        {
            await UpdateService.CheckUpdateAsync();
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex,
                                            LanguageManager.Instance.SettingsPage_CheckUpdatesDangerNotificationTitle
                                                           .Current());
        }

        SyncUpdateState();
        CheckUpdatesCommand.NotifyCanExecuteChanged();
        ViewReleaseCommand.NotifyCanExecuteChanged();
    }

    private bool CanViewRelease(AppUpdateModel? model) => model != null;

    [RelayCommand(CanExecute = nameof(CanViewRelease))]
    private void ViewRelease(AppUpdateModel? model)
    {
        if (model == null)
        {
            return;
        }

        OverlayService.PopModal(new AppUpdateModal
        {
            Model = model,
            UpdateManager = _updateManager,
            NotificationService = _notificationService
        });
    }

    [RelayCommand]
    private async Task ClearStatisticsAsync()
    {
        var confirmed =
            await OverlayService.RequestStrongConfirmationAsync(LanguageManager.Instance
                                                                   .SettingsPage_ClearStatisticsConfirmationMessage
                                                                   .Current(),
                                                                LanguageManager.Instance
                                                                   .SettingsPage_ClearStatisticsConfirmationTitle
                                                                   .Current());
        if (confirmed)
        {
            _persistenceService.ClearAllActivities();
        }
    }

    [RelayCommand]
    private async Task ClearRecordsAsync()
    {
        var confirmed =
            await OverlayService.RequestStrongConfirmationAsync(LanguageManager.Instance
                                                                   .SettingsPage_ClearRecordsConfirmationMessage
                                                                   .Current(),
                                                                LanguageManager.Instance
                                                                   .SettingsPage_ClearRecordsConfirmationTitle
                                                                   .Current());
        if (confirmed)
        {
            _persistenceService.ClearAllActions();
        }
    }

    [RelayCommand]
    private async Task GarbageCollectAsync()
    {
        var confirmed =
            await OverlayService.RequestConfirmationAsync(LanguageManager.Instance
                                                                         .SettingsPage_GarbageCollectConfirmationMessage
                                                                         .Current(),
                                                          LanguageManager.Instance
                                                                         .SettingsPage_GarbageCollectConfirmationTitle
                                                                         .Current());
        if (!confirmed)
        {
            return;
        }

        var progress = new ProgressModal
        {
            Title = LanguageManager.Instance.SettingsPage_GarbageCollectProgressTitle.Current(),
            StatusText = LanguageManager.Instance.SettingsPage_GarbageCollectProgressScanningText.Current(),
            IsIndeterminate = true
        };
        OverlayService.PopModal(progress);

        var reporter = new Progress<double?>(v =>
        {
            if (v.HasValue)
            {
                progress.IsIndeterminate = false;
                progress.StatusText = LanguageManager.Instance.SettingsPage_GarbageCollectProgressCleaningText
                                                     .Current();
                progress.ProgressValue = (int)(v.Value * 100);
            }
            else
            {
                progress.IsIndeterminate = true;
            }
        });

        try
        {
            await Task.Run(() => _garbageCollector.Execute(reporter));
            progress.Dismiss();
            _notificationService.PopMessage(LanguageManager.Instance.SettingsPage_GarbageCollectSuccessMessage
                                                           .Current(),
                                            LanguageManager.Instance.SettingsPage_GarbageCollectSuccessTitle.Current(),
                                            GrowlLevel.Success);
        }
        catch (Exception ex)
        {
            progress.Dismiss();
            _notificationService.PopMessage(ex,
                                            LanguageManager.Instance.SettingsPage_GarbageCollectDangerTitle.Current());
        }
    }

    [RelayCommand]
    private void OpenMigrate() => OverlayService.PopModal<MigrateModal>();

    #endregion

    #region Updates

    [ObservableProperty]
    public partial AppUpdateState UpdateState { get; set; }

    [ObservableProperty]
    public partial AppUpdateModel? UpdateTarget { get; set; }

    [ObservableProperty]
    public partial bool AutoCheckUpdates { get; set; }

    partial void OnAutoCheckUpdatesChanged(bool value) => _configurationService.Value.UpdateAutoCheck = value;

    [ObservableProperty]
    public partial int UpdateSource { get; set; }

    partial void OnUpdateSourceChanged(int value) => _configurationService.Value.UpdateSource = value;

    [ObservableProperty]
    public partial string MirrorChyanCdk { get; set; }

    partial void OnMirrorChyanCdkChanged(string value) => _configurationService.Value.UpdateMirrorChyanCdk = value;

    #endregion

    #region Privacy

    [ObservableProperty]
    public partial bool CrashReportingEnabled { get; set; }

    partial void OnCrashReportingEnabledChanged(bool value)
    {
        var exist = File.Exists(PathDef.Default.FileOfTelemetrySwitch());
        try
        {
            switch (value)
            {
                case true when exist:
                    File.Delete(PathDef.Default.FileOfTelemetrySwitch());
                    break;
                case false when !exist:
                {
                    var dir = Path.GetDirectoryName(PathDef.Default.FileOfTelemetrySwitch());
                    if (dir != null && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    File.WriteAllText(PathDef.Default.FileOfTelemetrySwitch(), Program.MagicWords);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Switching NO_TELEMETRY failed");
        }
    }

    #endregion

    #region Other

    private void SyncUpdateState()
    {
        UpdateState = UpdateService.UpdateState;
        UpdateTarget = UpdateService.CurrentUpdate;
    }

    private T TryConvertEnum<T>(int value, T orDefault = default) where T : struct, Enum
    {
        if (Enum.IsDefined(typeof(T), value))
        {
            return (T)(object)value;
        }

        return orDefault;
    }

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

    partial void OnTitleBarVisibilityChanged(bool value) => _themeService.TitleBarVisible = value;

    #endregion

    #region SidebarPlacement

    [ObservableProperty]
    public partial int SidebarPlacement { get; set; }

    partial void OnSidebarPlacementChanged(int value)
    {
        var rv = value == 0;
        _themeService.LeftPanelMode = rv;
    }

    #endregion

    #region AccentColor

    [ObservableProperty]
    public partial AccentColor AccentColor { get; set; }

    partial void OnAccentColorChanged(AccentColor value) => _themeService.Accent = value;

    public AccentColor[] AccentColors { get; } = Enum.GetValues<AccentColor>();

    #endregion

    #region CornerStyle

    [ObservableProperty]
    public partial CornerStyle CornerStyle { get; set; }

    partial void OnCornerStyleChanged(CornerStyle value) => _themeService.Corner = value;

    public CornerStyle[] CornerStyles { get; } = Enum.GetValues<CornerStyle>();

    #endregion

    #region BackgroundMode

    [ObservableProperty]
    public partial BackgroundStyleModel BackgroundMode { get; set; }

    partial void OnBackgroundModeChanged(BackgroundStyleModel value) => _themeService.TransparencyIndex = value.Index;

    public BackgroundStyleModel[] BackgroundStyles { get; } =
    [
        new(0, "SettingsPage_BackgroundStyleAutoText"),
        new(1, "SettingsPage_BackgroundStyleMicaText", "Windows 11+"),
        new(2, "SettingsPage_BackgroundStyleAcrylicText", "Windows 10+/macOS"),
        new(3, "SettingsPage_BackgroundStyleBlurText", "Linux"),
        new(4, "SettingsPage_BackgroundStyleNoneText")
    ];

    #endregion

    #region DarkMode

    [ObservableProperty]
    public partial int DarkMode { get; set; }

    partial void OnDarkModeChanged(int value) => _themeService.ThemeVariantIndex = value;

    #endregion

    #region Language

    public LanguageModel[] Languages { get; } =
    [
        .. Configuration.SupportedLanguages.Select(CultureInfo.GetCultureInfo).Select(x => new LanguageModel(x))
    ];

    [ObservableProperty]
    public partial LanguageModel Language { get; set; }

    partial void OnLanguageChanged(LanguageModel value)
    {
        _configurationService.Value.ApplicationLanguage = value.Id;
        LanguageManager.Instance.UpdateCulture(CultureInfo.GetCultureInfo(value.Id));
    }

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

    [ObservableProperty]
    public partial string? JavaHome25 { get; set; }

    partial void OnJavaHome25Changed(string? value) =>
        _configurationService.Value.RuntimeJavaHome25 = value ?? string.Empty;

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

    #region Command Wrapper

    [ObservableProperty]
    public partial string CommandWrapper { get; set; }

    partial void OnCommandWrapperChanged(string value) => _configurationService.Value.GameCommandWrapper = value;

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

    #region Proxy Settings

    [ObservableProperty]
    public partial ProxyMode ProxyMode { get; set; }

    partial void OnProxyModeChanged(ProxyMode value)
    {
        _configurationService.Value.NetworkProxyMode = (int)value;
        UpdateProxyStatusText();
    }

    [ObservableProperty]
    public partial ProxyProtocol ProxyProtocol { get; set; }

    partial void OnProxyProtocolChanged(ProxyProtocol value)
    {
        _configurationService.Value.NetworkProxyProtocol = (int)value;
        UpdateProxyStatusText();
    }

    [ObservableProperty]
    public partial bool ProxyEnabled { get; set; }

    partial void OnProxyEnabledChanged(bool value) => _configurationService.Value.NetworkProxyEnabled = value;

    [ObservableProperty]
    public partial string ProxyAddress { get; set; }

    partial void OnProxyAddressChanged(string value)
    {
        _configurationService.Value.NetworkProxyAddress = value;
        UpdateProxyStatusText();
    }

    [ObservableProperty]
    public partial uint ProxyPort { get; set; }

    partial void OnProxyPortChanged(uint value)
    {
        _configurationService.Value.NetworkProxyPort = value;
        UpdateProxyStatusText();
    }

    [ObservableProperty]
    public partial string ProxyUsername { get; set; }

    partial void OnProxyUsernameChanged(string value) => _configurationService.Value.NetworkProxyUsername = value;

    [ObservableProperty]
    public partial string ProxyPassword { get; set; }

    partial void OnProxyPasswordChanged(string value) => _configurationService.Value.NetworkProxyPassword = value;

    [ObservableProperty]
    public partial string ProxyStatusText { get; set; } = string.Empty;

    private void UpdateProxyStatusText() =>
        ProxyStatusText = ProxyMode switch
        {
            ProxyMode.Auto => LanguageManager.Instance.SettingsPage_ProxyStatusAutoText.Current(),
            ProxyMode.Disabled => LanguageManager.Instance.SettingsPage_ProxyStatusDisabledText.Current(),
            ProxyMode.Manual => LanguageManager
                               .Instance.SettingsPage_ProxyStatusManualText.Current()
                               .Replace("{0}", ProxyProtocol.ToString().ToLower())
                               .Replace("{1}", ProxyAddress)
                               .Replace("{2}", ProxyPort.ToString()),
            _ => string.Empty
        };

    [RelayCommand]
    private async Task OpenProxySettingsAsync()
    {
        var currentSettings = new ProxySettingsModel
        {
            Mode = ProxyMode,
            Protocol = ProxyProtocol,
            Address = ProxyAddress,
            Port = ProxyPort,
            Username = ProxyUsername,
            Password = ProxyPassword
        };

        var dialog = new ProxySettingsDialog();
        dialog.Initialize(currentSettings);

        if (await OverlayService.PopDialogAsync(dialog) && dialog.Result is ProxySettingsModel newSettings)
        {
            ProxyMode = newSettings.Mode;
            ProxyProtocol = newSettings.Protocol;
            ProxyAddress = newSettings.Address;
            ProxyPort = newSettings.Port;
            ProxyUsername = newSettings.Username;
            ProxyPassword = newSettings.Password;
        }
    }

    #endregion

    #region Font Settings

    [ObservableProperty]
    public partial FontModelBase MainFontSelection { get; set; }

    [ObservableProperty]
    public partial FontModelBase CodeFontSelection { get; set; }

    [ObservableProperty]
    public partial FontModelBase LogFontSelection { get; set; }

    [RelayCommand]
    private async Task OpenMainFontPickerAsync()
    {
        var dialog = new FontPickerDialog();
        dialog.Initialize(MainFontSelection, FontService.MainFallback);
        if (await OverlayService.PopDialogAsync(dialog) && dialog.Result is FontModelBase selection)
        {
            MainFontSelection = selection;
            _fontService.SetMain(selection);
        }
    }

    [RelayCommand]
    private async Task OpenCodeFontPickerAsync()
    {
        var dialog = new FontPickerDialog();
        dialog.Initialize(CodeFontSelection, FontService.CodeFallback);
        if (await OverlayService.PopDialogAsync(dialog) && dialog.Result is FontModelBase selection)
        {
            CodeFontSelection = selection;
            _fontService.SetCode(selection);
        }
    }

    [RelayCommand]
    private async Task OpenLogFontPickerAsync()
    {
        var dialog = new FontPickerDialog();
        dialog.Initialize(LogFontSelection, FontService.LogFallback);
        if (await OverlayService.PopDialogAsync(dialog) && dialog.Result is FontModelBase selection)
        {
            LogFontSelection = selection;
            _fontService.SetLog(selection);
        }
    }

    #endregion
}

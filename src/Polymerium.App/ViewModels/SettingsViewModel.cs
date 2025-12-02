using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia;
using Polymerium.App.Dialogs;
using Polymerium.App.Facilities;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Properties;
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
        JavaMaxMemory = configurationService.Value.GameJavaMaxMemory;
        JavaAdditionalArguments = configurationService.Value.GameJavaAdditionalArguments;
        WindowInitialWidth = configurationService.Value.GameWindowInitialWidth;
        WindowInitialHeight = configurationService.Value.GameWindowInitialHeight;

        // Initialize proxy settings
        ProxyMode = (ProxyMode)configurationService.Value.NetworkProxyMode;
        ProxyProtocol = (ProxyProtocol)configurationService.Value.NetworkProxyProtocol;
        ProxyEnabled = configurationService.Value.NetworkProxyEnabled;
        ProxyAddress = configurationService.Value.NetworkProxyAddress;
        ProxyPort = configurationService.Value.NetworkProxyPort;
        ProxyUsername = configurationService.Value.NetworkProxyUsername;
        ProxyPassword = configurationService.Value.NetworkProxyPassword;

        // Initialize proxy status text
        UpdateProxyStatusText();
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
                {
                    box.Text = dir;
                }
            }
        }
    }

    [RelayCommand]
    private void Navigate(Type? view)
    {
        if (view != null)
        {
            _navigationService.Navigate(view);
        }
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
                UpdateTarget = new(result);
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

    #endregion

    #region Updates

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
    public partial BackgroundStyleModel BackgroundMode { get; set; }

    partial void OnBackgroundModeChanged(BackgroundStyleModel value)
    {
        _configurationService.Value.ApplicationStyleBackground = value.Index;
        MainWindow.Instance.SetTransparencyLevelHintByIndex(value.Index);
    }

    public BackgroundStyleModel[] BackgroundStyles { get; } =
    [
        new(0, Resources.SettingsView_BackgroundStyleAutoText),
        new(1, Resources.SettingsView_BackgroundStyleMicaText, "Windows 11+"),
        new(2, Resources.SettingsView_BackgroundStyleAcrylicText, "Windows 10+"),
        new(3, Resources.SettingsView_BackgroundStyleBlurText, "macOS/Linux"),
        new(4, Resources.SettingsView_BackgroundStyleNoneText)
    ];

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

    public LanguageModel[] Languages { get; } =
    [
        .. SupportedLanguages.Select(CultureInfo.GetCultureInfo).Select(x => new LanguageModel(x))
    ];

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

    private void UpdateProxyStatusText()
    {
        ProxyStatusText = ProxyMode switch
        {
            ProxyMode.Auto => Resources.SettingsView_ProxyStatusAutoText,
            ProxyMode.Disabled => Resources.SettingsView_ProxyStatusDisabledText,
            ProxyMode.Manual => Resources
                               .SettingsView_ProxyStatusManualText.Replace("{0}", ProxyProtocol.ToString().ToLower())
                               .Replace("{1}", ProxyAddress)
                               .Replace("{2}", ProxyPort.ToString()),
            _ => string.Empty
        };
    }

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
            // Apply the new settings
            ProxyMode = newSettings.Mode;
            ProxyProtocol = newSettings.Protocol;
            ProxyAddress = newSettings.Address;
            ProxyPort = newSettings.Port;
            ProxyUsername = newSettings.Username;
            ProxyPassword = newSettings.Password;
        }
    }

    #endregion
}

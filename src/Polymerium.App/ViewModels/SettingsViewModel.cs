using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    public SettingsViewModel(ConfigurationService configurationService, OverlayService overlayService)
    {
        _configurationService = configurationService;
        _overlayService = overlayService;


        SuperPowerActivated = configurationService.Value.ApplicationSuperPowerActivated;
        SidebarPlacement = configurationService.Value.ApplicationLeftPanelMode ? 0 : 1;
        BackgroundMode = configurationService.Value.ApplicationStyleBackground;
        DarkMode = configurationService.Value.ApplicationStyleThemeVariant;
        Language = Languages.FirstOrDefault(x => x.Id == configurationService.Value.ApplicationLanguage)
                ?? Languages.First();
        JavaHome8 = MayKnowYourJavaInstallation(configurationService.Value.RuntimeJavaHome8);
        JavaHome11 = MayKnowYourJavaInstallation(configurationService.Value.RuntimeJavaHome11);
        JavaHome16 = MayKnowYourJavaInstallation(configurationService.Value.RuntimeJavaHome16);
        JavaHome17 = MayKnowYourJavaInstallation(configurationService.Value.RuntimeJavaHome17);
        JavaHome21 = MayKnowYourJavaInstallation(configurationService.Value.RuntimeJavaHome21);
        JavaMaxMemory = configurationService.Value.GameJavaMaxMemory;
        JavaAdditionalArguments = configurationService.Value.GameJavaAdditionalArguments;
        WindowInitialWidth = configurationService.Value.GameWindowInitialWidth;
        WindowInitialHeight = configurationService.Value.GameWindowInitialHeight;
    }

    private JavaInstallationModel? MayKnowYourJavaInstallation(string maybeHome) =>
        !string.IsNullOrEmpty(maybeHome) ? KnowYourJavaInstallation(maybeHome) : null;

    private JavaInstallationModel KnowYourJavaInstallation(string home)
    {
        var path = Path.Combine(home, "release");
        if (File.Exists(path))
            return new JavaInstallationModel(home, null, null, null);

        return new JavaInstallationModel(home, null, null, null);
    }

    #region Commands

    [RelayCommand]
    private async Task PickFile(TextBox? box)
    {
        if (box != null)
        {
            var path = await _overlayService.RequestFileAsync("Pick a file like /bin/java.exe or /bin/javaw.exe",
                                                              "Select a Java executable");
            if (path != null && File.Exists(path))
            {
                var dir = Path.GetDirectoryName(Path.GetDirectoryName(path));
                if (dir != null)
                    box.Text = dir;
            }
        }
    }

    #endregion

    #region Injected

    private readonly ConfigurationService _configurationService;
    private readonly OverlayService _overlayService;

    #endregion

    #region SuperPowerActivated

    [ObservableProperty]
    public partial bool SuperPowerActivated { get; set; }

    partial void OnSuperPowerActivatedChanged(bool value) =>
        _configurationService.Value.ApplicationSuperPowerActivated = value;

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

    public LanguageModel[] Languages { get; } = [new("en_US", "Chinglish"), new("zh_CN", "中国人")];

    [ObservableProperty]
    public partial LanguageModel Language { get; set; }

    partial void OnLanguageChanged(LanguageModel value) => _configurationService.Value.ApplicationLanguage = value.Id;

    #endregion

    #region JavaHome

    [ObservableProperty]
    public partial JavaInstallationModel? JavaHome8 { get; set; }

    partial void OnJavaHome8Changed(JavaInstallationModel? value) =>
        _configurationService.Value.RuntimeJavaHome8 = value?.Path ?? string.Empty;

    [ObservableProperty]
    public partial JavaInstallationModel? JavaHome11 { get; set; }

    partial void OnJavaHome11Changed(JavaInstallationModel? value) =>
        _configurationService.Value.RuntimeJavaHome11 = value?.Path ?? string.Empty;

    [ObservableProperty]
    public partial JavaInstallationModel? JavaHome16 { get; set; }

    partial void OnJavaHome16Changed(JavaInstallationModel? value) =>
        _configurationService.Value.RuntimeJavaHome16 = value?.Path ?? string.Empty;

    [ObservableProperty]
    public partial JavaInstallationModel? JavaHome17 { get; set; }

    partial void OnJavaHome17Changed(JavaInstallationModel? value) =>
        _configurationService.Value.RuntimeJavaHome17 = value?.Path ?? string.Empty;

    [ObservableProperty]
    public partial JavaInstallationModel? JavaHome21 { get; set; }

    partial void OnJavaHome21Changed(JavaInstallationModel? value) =>
        _configurationService.Value.RuntimeJavaHome21 = value?.Path ?? string.Empty;

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
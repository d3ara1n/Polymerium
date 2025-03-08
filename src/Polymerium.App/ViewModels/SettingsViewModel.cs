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
        Language = Languages.FirstOrDefault(x => x.Id == configurationService.Value.ApplicationLanguage)
                ?? Languages.First();
        JavaHome8 = configurationService.Value.RuntimeJavaHome8;
        JavaHome11 = configurationService.Value.RuntimeJavaHome11;
        JavaHome17 = configurationService.Value.RuntimeJavaHome17;
        JavaHome21 = configurationService.Value.RuntimeJavaHome21;
        JavaMaxMemory = configurationService.Value.GameJavaMaxMemory;
        JavaAdditionalArguments = configurationService.Value.GameJavaAdditionalArguments;
        WindowInitialWidth = configurationService.Value.GameWindowInitialWidth;
        WindowInitialHeight = configurationService.Value.GameWindowInitialHeight;
    }

    #region Injected

    private readonly ConfigurationService _configurationService;
    private readonly OverlayService _overlayService;

    #endregion

    #region Commands

    [RelayCommand]
    private void ClearText(TextBox? box)
    {
        box?.Clear();
    }

    [RelayCommand]
    private async Task PickFile(TextBox? box)
    {
        if (box != null)
        {
            var path = await _overlayService.RequestFileAsync("Pick a file like /bin/java.exe or /bin/javaw.exe",
                                                              "Select a Java executable");
            if (path != null && File.Exists(path))
            {
                var dir = Path.GetDirectoryName(path);
                if (dir != null)
                    box.Text = dir;
            }
        }
    }

    #endregion

    #region SuperPowerActivated

    [ObservableProperty]
    public partial bool SuperPowerActivated { get; set; }

    partial void OnSuperPowerActivatedChanged(bool value) =>
        _configurationService.Value.ApplicationSuperPowerActivated = value;

    #endregion

    #region Language

    public LanguageModel[] Languages { get; } = [new("en_US", "Chinglish"), new("zh_CN", "中国人")];

    [ObservableProperty]
    public partial LanguageModel Language { get; set; }

    partial void OnLanguageChanged(LanguageModel value) => _configurationService.Value.ApplicationLanguage = value.Id;

    #endregion

    #region JavaHome

    [ObservableProperty]
    public partial string JavaHome8 { get; set; }

    partial void OnJavaHome8Changed(string value) => _configurationService.Value.RuntimeJavaHome8 = value;

    [ObservableProperty]
    public partial string JavaHome11 { get; set; }

    partial void OnJavaHome11Changed(string value) => _configurationService.Value.RuntimeJavaHome11 = value;

    [ObservableProperty]
    public partial string JavaHome17 { get; set; }

    partial void OnJavaHome17Changed(string value) => _configurationService.Value.RuntimeJavaHome17 = value;

    [ObservableProperty]
    public partial string JavaHome21 { get; set; }

    partial void OnJavaHome21Changed(string value) => _configurationService.Value.RuntimeJavaHome21 = value;

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
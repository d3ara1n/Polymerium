using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia;
using Polymerium.App.Controls;
using Polymerium.App.Dialogs;
using Polymerium.App.Models;
using Polymerium.App.Services;

namespace Polymerium.App.Components;

public partial class OobeQuickSetup : OobeStep
{
    public static readonly DirectProperty<OobeQuickSetup, LanguageModel[]> LanguagesProperty =
        AvaloniaProperty.RegisterDirect<OobeQuickSetup, LanguageModel[]>(nameof(Languages), o => o.Languages);

    public static readonly DirectProperty<OobeQuickSetup, LanguageModel?> SelectedLanguageProperty =
        AvaloniaProperty.RegisterDirect<OobeQuickSetup, LanguageModel?>(nameof(SelectedLanguage),
                                                                        o => o.SelectedLanguage,
                                                                        (o, v) => o.SelectedLanguage = v);

    public static readonly DirectProperty<OobeQuickSetup, int> DarkModeIndexProperty =
        AvaloniaProperty.RegisterDirect<OobeQuickSetup, int>(nameof(DarkModeIndex),
                                                             o => o.DarkModeIndex,
                                                             (o, v) => o.DarkModeIndex = v);

    public static readonly DirectProperty<OobeQuickSetup, AccentColor[]> AccentColorsProperty =
        AvaloniaProperty.RegisterDirect<OobeQuickSetup, AccentColor[]>(nameof(AccentColors), o => o.AccentColors);

    public static readonly DirectProperty<OobeQuickSetup, AccentColor> SelectedAccentColorProperty =
        AvaloniaProperty.RegisterDirect<OobeQuickSetup, AccentColor>(nameof(SelectedAccentColor),
                                                                     o => o.SelectedAccentColor,
                                                                     (o, v) => o.SelectedAccentColor = v);

    public static readonly DirectProperty<OobeQuickSetup, string> ProxyStatusTextProperty =
        AvaloniaProperty.RegisterDirect<OobeQuickSetup, string>(nameof(ProxyStatusText), o => o.ProxyStatusText);

    public OobeQuickSetup() => InitializeComponent();

    public required ConfigurationService ConfigurationService { get; init; }
    public required OverlayService OverlayService { get; init; }

    public LanguageModel[] Languages { get; } =
    [
        .. Configuration.SupportedLanguages.Select(CultureInfo.GetCultureInfo).Select(x => new LanguageModel(x))
    ];

    public LanguageModel? SelectedLanguage
    {
        get;
        set
        {
            if (SetAndRaise(SelectedLanguageProperty, ref field, value) && value != null)
            {
                ConfigurationService.Value.ApplicationLanguage = value.Id;
            }
        }
    }

    public int DarkModeIndex
    {
        get;
        set
        {
            if (SetAndRaise(DarkModeIndexProperty, ref field, value))
            {
                ConfigurationService.Value.ApplicationStyleThemeVariant = value;
                MainWindow.Instance.SetThemeVariantByIndex(value);
            }
        }
    }

    public AccentColor[] AccentColors { get; } = Enum.GetValues<AccentColor>();

    public AccentColor SelectedAccentColor
    {
        get;
        set
        {
            if (SetAndRaise(SelectedAccentColorProperty, ref field, value))
            {
                ConfigurationService.Value.ApplicationStyleAccent = value;
                MainWindow.Instance.SetColorVariant(value);
            }
        }
    }

    public string ProxyStatusText
    {
        get;
        private set => SetAndRaise(ProxyStatusTextProperty, ref field, value);
    } = string.Empty;

    [RelayCommand]
    private async Task OpenProxySettingsAsync()
    {
        var config = ConfigurationService.Value;
        var settings = new ProxySettingsModel
        {
            Mode = (ProxyMode)config.NetworkProxyMode,
            Protocol = (ProxyProtocol)config.NetworkProxyProtocol,
            Address = config.NetworkProxyAddress,
            Port = config.NetworkProxyPort,
            Username = config.NetworkProxyUsername,
            Password = config.NetworkProxyPassword
        };

        var dialog = new ProxySettingsDialog();
        dialog.Initialize(settings);

        if (await OverlayService.PopDialogAsync(dialog) && dialog.Result is ProxySettingsModel result)
        {
            config.NetworkProxyMode = (int)result.Mode;
            config.NetworkProxyProtocol = (int)result.Protocol;
            config.NetworkProxyAddress = result.Address;
            config.NetworkProxyPort = result.Port;
            config.NetworkProxyUsername = result.Username;
            config.NetworkProxyPassword = result.Password;
            UpdateProxyStatusText();
        }
    }

    private void UpdateProxyStatusText()
    {
        var config = ConfigurationService.Value;
        var mode = (ProxyMode)config.NetworkProxyMode;
        ProxyStatusText = mode switch
        {
            ProxyMode.Auto => Properties.Resources.ProxySettingsDialog_ProxyMode_Auto,
            ProxyMode.Manual => $"{config.NetworkProxyAddress}:{config.NetworkProxyPort}",
            ProxyMode.Disabled => Properties.Resources.ProxySettingsDialog_ProxyMode_Disabled,
            _ => string.Empty
        };
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Load current settings
        var config = ConfigurationService.Value;

        SelectedLanguage = Languages.FirstOrDefault(x => x.Id == config.ApplicationLanguage) ?? Languages.First();
        DarkModeIndex = config.ApplicationStyleThemeVariant;
        SelectedAccentColor = config.ApplicationStyleAccent;
        UpdateProxyStatusText();
    }
}

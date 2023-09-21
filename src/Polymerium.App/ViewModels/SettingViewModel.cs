using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Polymerium.Abstractions.LaunchConfigurations;
using Polymerium.App.Configurations;
using Polymerium.App.Dialogs;
using Polymerium.App.Models;
using Polymerium.App.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Polymerium.App.ViewModels;

public class SettingViewModel : ObservableObject
{
    private readonly ConfigurationManager _configurationManager;
    private readonly JavaManager _javaManager;
    private readonly LocalizationService _localizationService;
    private readonly AppSettings _settings;

    private string additionalJvmArguments = string.Empty;

    private bool autoDetectJava;

    private bool forceImportOffline;
    private bool isSuperPowerActivated;

    private string javaHome = string.Empty;

    private string javaSummary = string.Empty;

    private uint jvmMaxMemory;

    private JavaInstallationModel? selectedJava;
    private SettingLanguageItemModel selectedLanguage = null!;

    private bool skipJavaVersionCheck;

    private uint windowHeight;

    private uint windowWidth;

    public SettingViewModel(
        ConfigurationManager configurationManager,
        JavaManager javaManager,
        AppSettings settings,
        LocalizationService localizationService
    )
    {
        _configurationManager = configurationManager;
        _javaManager = javaManager;
        _settings = settings;
        _localizationService = localizationService;
        OpenPickerAsyncCommand = new AsyncRelayCommand(OpenPickerAsync);
        Languages = new ObservableCollection<SettingLanguageItemModel>(
            localizationService
                .GetSupportedLanguages()
                .Select(x => new SettingLanguageItemModel(x.Item1, x.Item2))
        );
        ForceImportOffline = _settings.ForceImportOffline;
        SelectedLanguage = Languages.First(x => x.Key == _settings.LanguageKey);
        IsSuperPowerActivated = _settings.IsSuperPowerActivated;
        Global = configurationManager.Current.GameGlobals;
        AutoDetectJava = Global.AutoDetectJava ?? true;
        SkipJavaVersionCheck = Global.SkipJavaVersionCheck ?? false;
        JavaHome = Global.JavaHome ?? string.Empty;
        JvmMaxMemory = Global.JvmMaxMemory ?? 4096;
        AdditionalJvmArugments = Global.AdditionalJvmArguments ?? string.Empty;
        WindowWidth = Global.WindowWidth ?? 854;
        WindowHeight = Global.WindowHeight ?? 480;
    }

    public ICommand OpenPickerAsyncCommand { get; }

    public FileBasedLaunchConfiguration Global { get; }

    public ObservableCollection<SettingLanguageItemModel> Languages { get; }

    public SettingLanguageItemModel SelectedLanguage
    {
        get => selectedLanguage;
        set
        {
            SetProperty(ref selectedLanguage, value);
            _settings.LanguageKey = value.Key;
            _localizationService.SetLanguageByKey(value.Key);
        }
    }

    public bool ForceImportOffline
    {
        get => forceImportOffline;
        set
        {
            SetProperty(ref forceImportOffline, value);
            _settings.ForceImportOffline = value;
        }
    }

    public bool IsSuperPowerActivated
    {
        get => isSuperPowerActivated;
        set
        {
            SetProperty(ref isSuperPowerActivated, value);
            _settings.IsSuperPowerActivated = value;
        }
    }

    public string JavaSummary
    {
        get => javaSummary;
        set => SetProperty(ref javaSummary, value);
    }

    public string JavaHome
    {
        get => javaHome;
        set
        {
            if (SetProperty(ref javaHome, value))
            {
                Global.JavaHome = value;
                var option = _javaManager.VerifyJavaHome(value);
                if (option.TryUnwrap(out var model))
                    SelectedJava = model;
                else
                    JavaSummary = value;
            }
        }
    }

    public bool AutoDetectJava
    {
        get => autoDetectJava;
        set
        {
            SetProperty(ref autoDetectJava, value);
            Global.AutoDetectJava = value;
        }
    }

    public JavaInstallationModel? SelectedJava
    {
        get => selectedJava;
        set
        {
            if (SetProperty(ref selectedJava, value))
            {
                JavaHome = value!.HomePath;
                JavaSummary = value.Summary ?? string.Empty;
            }
        }
    }

    public bool SkipJavaVersionCheck
    {
        get => skipJavaVersionCheck;
        set
        {
            SetProperty(ref skipJavaVersionCheck, value);
            Global.SkipJavaVersionCheck = value;
        }
    }

    public uint JvmMaxMemory
    {
        get => jvmMaxMemory;
        set
        {
            if (SetProperty(ref jvmMaxMemory, value))
                Global.JvmMaxMemory = value;
        }
    }

    public string AdditionalJvmArugments
    {
        get => additionalJvmArguments;
        set
        {
            if (SetProperty(ref additionalJvmArguments, value))
                Global.AdditionalJvmArguments = value;
        }
    }

    public uint WindowHeight
    {
        get => windowHeight;
        set
        {
            if (SetProperty(ref windowHeight, value))
                Global.WindowHeight = value;
        }
    }

    public uint WindowWidth
    {
        get => windowWidth;
        set
        {
            if (SetProperty(ref windowWidth, value))
                Global.WindowWidth = value;
        }
    }

    public async Task OpenPickerAsync()
    {
        var dialog = new JavaPickerDialog
        {
            XamlRoot = App.Current.Window.Content.XamlRoot,
            JavaInstallations = _javaManager
                .QueryJavaInstallations()
                .Select(x =>
                {
                    var option = _javaManager.VerifyJavaHome(x);
                    if (option.TryUnwrap(out var model))
                        return model;
                    return null;
                })
                .Where(x => x != null)
                .Select(x => x!)
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            SelectedJava = dialog.SelectedJava;
    }
}

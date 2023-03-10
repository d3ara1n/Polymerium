using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Polymerium.Abstractions.LaunchConfigurations;
using Polymerium.App.Dialogs;
using Polymerium.App.Models;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels;

public class SettingViewModel : ObservableObject
{
    private readonly ConfigurationManager _configurationManager;
    private readonly JavaManager _javaManager;

    private bool autoDetectJava;

    private string javaHome = string.Empty;

    private string javaSummary = string.Empty;
    private uint jvmMaxMemory;

    private JavaInstallationModel? selectedJava;

    private bool skipJavaVersionCheck;

    private uint windowHeight;

    private uint windowWidth;

    public SettingViewModel(ConfigurationManager configurationManager, JavaManager javaManager)
    {
        _configurationManager = configurationManager;
        _javaManager = javaManager;
        OpenPickerAsyncCommand = new AsyncRelayCommand(OpenPickerAsync);
        Global = configurationManager.Current.GameGlobals;
        AutoDetectJava = Global.AutoDetectJava ?? true;
        SkipJavaVersionCheck = Global.SkipJavaVersionCheck ?? false;
        JavaHome = Global.JavaHome ?? string.Empty;
        JvmMaxMemory = Global.JvmMaxMemory ?? 4096;
        WindowWidth = Global.WindowWidth ?? 480;
        WindowHeight = Global.WindowHeight ?? 854;
    }

    public ICommand OpenPickerAsyncCommand { get; }

    public FileBasedLaunchConfiguration Global { get; }

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
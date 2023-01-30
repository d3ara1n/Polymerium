using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Abstractions.LaunchConfigurations;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels;

public class SettingViewModel : ObservableObject
{
    private readonly ConfigurationManager _configurationManager;

    private string javaPath;

    public SettingViewModel(ConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager;
        configurationManager.Current.GameGlobals ??= new FileBasedLaunchConfiguration();
        Global = configurationManager.Current.GameGlobals;
        javaPath = Global.JavaPath;
    }

    public FileBasedLaunchConfiguration Global { get; }

    public string JavaPath
    {
        get => javaPath;
        set
        {
            SetProperty(ref javaPath, value);
            Global.JavaPath = value;
        }
    }
}
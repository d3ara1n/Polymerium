using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Core.LaunchConfigurations;

namespace Polymerium.App.Models;

public class ConfigurationModel : ObservableObject
{
    public ConfigurationModel(CompoundLaunchConfiguration configuration)
    {
        Inner = configuration;
    }

    public CompoundLaunchConfiguration Inner { get; }

    public string JavaHome
    {
        get => Inner.JavaHome ?? "UNDEFINED";
        set
        {
            Inner.JavaHome = value;
            OnPropertyChanged();
        }
    }

    public bool AutoDetectJava
    {
        get => Inner.AutoDetectJava ?? true;
        set
        {
            Inner.AutoDetectJava = value;
            OnPropertyChanged();
        }
    }

    public bool SkipJavaVersionCheck
    {
        get => Inner.SkipJavaVersionCheck ?? false;
        set
        {
            Inner.SkipJavaVersionCheck = value;
            OnPropertyChanged();
        }
    }

    public uint JvmMaxMemory
    {
        get => Inner.JvmMaxMemory ?? 0;
        set
        {
            Inner.JvmMaxMemory = value;
            OnPropertyChanged();
        }
    }

    public string AdditionalJvmArguments
    {
        get => Inner.AdditionalJvmArguments ?? string.Empty;
        set
        {
            Inner.AdditionalJvmArguments = value;
            OnPropertyChanged();
        }
    }

    public uint WindowWidth
    {
        get => Inner.WindowWidth ?? 0;
        set
        {
            Inner.WindowWidth = value;
            OnPropertyChanged();
        }
    }

    public uint WindowHeight
    {
        get => Inner.WindowHeight ?? 0;
        set
        {
            Inner.WindowHeight = value;
            OnPropertyChanged();
        }
    }
}

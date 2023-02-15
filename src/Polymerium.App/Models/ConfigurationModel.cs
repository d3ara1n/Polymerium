using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Abstractions.LaunchConfigurations;

namespace Polymerium.App.Models;

public class ConfigurationModel : ObservableObject
{
    public ConfigurationModel(FileBasedLaunchConfiguration configuration)
    {
        Inner = configuration;
    }

    public FileBasedLaunchConfiguration Inner { get; }

    public string JavaHome
    {
        get => Inner.JavaHome;
        set
        {
            Inner.JavaHome = value;
            OnPropertyChanged();
        }
    }

    public bool? AutoDetectJava
    {
        get => Inner.AutoDetectJava;
        set
        {
            Inner.AutoDetectJava = value;
            OnPropertyChanged();
        }
    }

    public bool? SkipJavaVersionCheck
    {
        get => Inner.SkipJavaVersionCheck;
        set
        {
            Inner.SkipJavaVersionCheck = value;
            OnPropertyChanged();
        }
    }

    public uint? JvmMaxMemory
    {
        get => Inner.JvmMaxMemory;
        set
        {
            Inner.JvmMaxMemory = value;
            OnPropertyChanged();
        }
    }

    public uint? WindowWidth
    {
        get => Inner.WindowWidth;
        set
        {
            Inner.WindowWidth = value;
            OnPropertyChanged();
        }
    }

    public uint? WindowHeight
    {
        get => Inner.WindowHeight;
        set
        {
            Inner.WindowHeight = value;
            OnPropertyChanged();
        }
    }
}
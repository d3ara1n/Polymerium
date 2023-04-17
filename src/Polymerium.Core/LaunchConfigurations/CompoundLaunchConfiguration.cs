using Polymerium.Abstractions.LaunchConfigurations;

namespace Polymerium.Core.LaunchConfigurations;

public class CompoundLaunchConfiguration : LaunchConfigurationBase
{
    private readonly LaunchConfigurationBase _first;
    private readonly LaunchConfigurationBase _second;

    public CompoundLaunchConfiguration(
        LaunchConfigurationBase first,
        LaunchConfigurationBase second
    )
    {
        _first = first;
        _second = second;
    }

    public override string? JavaHome
    {
        get => _first.JavaHome ?? _second.JavaHome;
        set => _first.JavaHome = value;
    }

    public override bool? AutoDetectJava
    {
        get => _first.AutoDetectJava ?? _second.AutoDetectJava;
        set => _first.AutoDetectJava = value;
    }

    public override bool? SkipJavaVersionCheck
    {
        get => _first.SkipJavaVersionCheck ?? _second.SkipJavaVersionCheck;
        set => _first.SkipJavaVersionCheck = value;
    }

    public override string? AdditionalJvmArguments
    {
        get => _first.AdditionalJvmArguments ?? _second.AdditionalJvmArguments;
        set => _first.AdditionalJvmArguments = value;
    }

    public override uint? JvmMaxMemory
    {
        get => _first.JvmMaxMemory ?? _second.JvmMaxMemory;
        set => _first.JvmMaxMemory = value;
    }

    public override uint? WindowHeight
    {
        get => _first.WindowHeight ?? _second.WindowHeight;
        set => _first.WindowHeight = value;
    }

    public override uint? WindowWidth
    {
        get => _first.WindowWidth ?? _second.WindowWidth;
        set => _first.WindowWidth = value;
    }
}

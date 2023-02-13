using System;
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

    public override string JavaHome
    {
        get => _first.JavaHome ?? _second.JavaHome;
        set => throw new NotSupportedException();
    }

    public override bool? AutoDetectJava
    {
        get => _first.AutoDetectJava ?? _second.AutoDetectJava;
        set => throw new NotSupportedException();
    }

    public override bool? SkipJavaVersionCheck
    {
        get => _first.SkipJavaVersionCheck ?? _second.SkipJavaVersionCheck;
        set => throw new NotSupportedException();
    }

    public override uint? JvmMaxMemory
    {
        get => _first.JvmMaxMemory ?? _second.JvmMaxMemory;
        set => throw new NotSupportedException();
    }

    public override uint? WindowHeight
    {
        get => _first.WindowHeight ?? _second.WindowHeight;
        set => throw new NotSupportedException();
    }

    public override uint? WindowWidth
    {
        get => _first.WindowWidth ?? _second.WindowWidth;
        set => throw new NotSupportedException();
    }
}

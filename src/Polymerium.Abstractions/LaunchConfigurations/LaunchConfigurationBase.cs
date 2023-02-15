namespace Polymerium.Abstractions.LaunchConfigurations;

public abstract class LaunchConfigurationBase
{
    public abstract string JavaHome { get; set; }
    public abstract bool? AutoDetectJava { get; set; }
    public abstract bool? SkipJavaVersionCheck { get; set; }
    public abstract uint? JvmMaxMemory { get; set; }
    public abstract uint? WindowHeight { get; set; }
    public abstract uint? WindowWidth { get; set; }
}
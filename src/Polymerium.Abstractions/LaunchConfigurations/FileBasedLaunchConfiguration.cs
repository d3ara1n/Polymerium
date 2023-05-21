namespace Polymerium.Abstractions.LaunchConfigurations;

public class FileBasedLaunchConfiguration : LaunchConfigurationBase
{
    public override string? JavaHome { get; set; }
    public override bool? AutoDetectJava { get; set; }
    public override bool? SkipJavaVersionCheck { get; set; }
    public override string? AdditionalJvmArguments { get; set; }
    public override uint? JvmMaxMemory { get; set; }
    public override uint? WindowHeight { get; set; }
    public override uint? WindowWidth { get; set; }
}
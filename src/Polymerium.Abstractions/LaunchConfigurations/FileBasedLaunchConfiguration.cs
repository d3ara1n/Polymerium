namespace Polymerium.Abstractions.LaunchConfigurations;

public class FileBasedLaunchConfiguration : LaunchConfigurationBase
{
    public override string JavaPath { get; set; }
    public override uint MaxJvmMemory { get; set; }
    public override uint WindowHeight { get; set; }
    public override uint WindowWidth { get; set; }
}
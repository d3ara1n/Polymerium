namespace Polymerium.Abstractions.LaunchConfigurations;

public abstract class LaunchConfigurationBase
{
    public abstract string JavaPath { get; set; }
    public abstract uint MaxJvmMemory { get; set; }
    public abstract uint WindowHeight { get; set; }
    public abstract uint WindowWidth { get; set; }
}
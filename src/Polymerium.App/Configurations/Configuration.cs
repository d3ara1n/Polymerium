using Polymerium.Abstractions.LaunchConfigurations;

namespace Polymerium.App.Configurations;

public class Configuration
{
    public AppSettings Settings { get; set; }
    public string AccountShowcaseId { get; set; }
    public FileBasedLaunchConfiguration GameGlobals { get; set; }
}
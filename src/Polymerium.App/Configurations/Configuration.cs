using Polymerium.Abstractions.LaunchConfigurations;

namespace Polymerium.App.Configurations;

public class Configuration
{
    public Configuration(
        string? accountShowcaseId,
        FileBasedLaunchConfiguration gameGlobals
    )
    {
        AccountShowcaseId = accountShowcaseId;
        GameGlobals = gameGlobals;
    }

    public Configuration()
    {
        AccountShowcaseId = null;
        GameGlobals = new FileBasedLaunchConfiguration();
    }
    public string? AccountShowcaseId { get; set; }
    public FileBasedLaunchConfiguration GameGlobals { get; set; }
}
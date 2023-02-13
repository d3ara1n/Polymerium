using Polymerium.Abstractions.LaunchConfigurations;

namespace Polymerium.App.Configurations;

public class Configuration
{
    public Configuration(
        AppSettings settings,
        string? accountShowcaseId,
        FileBasedLaunchConfiguration gameGlobals
    )
    {
        Settings = settings;
        AccountShowcaseId = accountShowcaseId;
        GameGlobals = gameGlobals;
    }

    public Configuration()
    {
        Settings = new AppSettings();
        AccountShowcaseId = null;
        GameGlobals = new FileBasedLaunchConfiguration();
    }

    public AppSettings Settings { get; set; }
    public string? AccountShowcaseId { get; set; }
    public FileBasedLaunchConfiguration GameGlobals { get; set; }
}

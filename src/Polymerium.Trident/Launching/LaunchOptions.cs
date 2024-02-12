namespace Polymerium.Trident.Launching;

public class LaunchOptions(
    LaunchMode launchMode = LaunchMode.Managed,
    IDictionary<string, string>? crates = null,
    JavaHomeLocatorDelegate? javaHomeLocator = null)
{
    public LaunchMode Mode { get; set; } = launchMode;
    public IDictionary<string, string> Crates { get; } = crates ?? new Dictionary<string, string>();
    public JavaHomeLocatorDelegate JavaHomeLocator { get; set; } = _ => null;

    public static LaunchOptionsBuilder Builder()
    {
        return new LaunchOptionsBuilder();
    }
}
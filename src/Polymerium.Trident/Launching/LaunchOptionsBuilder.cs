using IBuilder;

namespace Polymerium.Trident.Launching;

public class LaunchOptionsBuilder : IBuilder<LaunchOptions>
{
    private readonly IDictionary<string, string> crates = new Dictionary<string, string>();
    private JavaHomeLocatorDelegate javaHomeLocator = _ => null;
    private LaunchMode launchMode = LaunchMode.Managed;

    public LaunchOptions Build()
    {
        var result = new LaunchOptions(launchMode, crates, javaHomeLocator);
        return result;
    }

    public LaunchOptionsBuilder SetMode(LaunchMode mode)
    {
        launchMode = mode;
        return this;
    }

    public LaunchOptionsBuilder AddCrate(string key, string value)
    {
        crates[key] = value;
        return this;
    }

    public LaunchOptionsBuilder SetJavaHomeLocator(JavaHomeLocatorDelegate locator)
    {
        javaHomeLocator = locator;
        return this;
    }
}
using System.Drawing;

namespace Polymerium.Trident.Launching;

public class LaunchOptions(LaunchMode launchMode = LaunchMode.Managed, Size? windowSize = null, uint maxMemory = 4096, string? additionalArguments = null, JavaHomeLocatorDelegate? javaHomeLocator = null)
{
    public LaunchMode Mode { get; set; } = launchMode;
    public uint MaxMemory { get; set; } = maxMemory;
    public Size WindowSize { get; set; } = windowSize ?? new(1270, 720);
    public string AdditionalArguments { get; set; } = additionalArguments ?? string.Empty;
    public JavaHomeLocatorDelegate JavaHomeLocator { get; set; } = javaHomeLocator ?? new JavaHomeLocatorDelegate(v => throw new JavaNotFoundException(v));

    public static LaunchOptionsBuilder Builder()
    {
        return new LaunchOptionsBuilder();
    }
}
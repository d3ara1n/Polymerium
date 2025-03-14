using Polymerium.Trident.Exceptions;
using Trident.Abstractions;

namespace Polymerium.Trident.Igniters;

public class LaunchOptions(
    LaunchMode launchMode = LaunchMode.Managed,
    (uint, uint)? windowSize = null,
    uint maxMemory = 4096,
    string? additionalArguments = null,
    JavaHomeLocatorDelegate? javaHomeLocator = null)
{
    public LaunchMode Mode { get; set; } = launchMode;
    public uint MaxMemory { get; set; } = maxMemory;
    public (uint, uint) WindowSize { get; set; } = windowSize ?? (1270, 720);
    public string AdditionalArguments { get; set; } = additionalArguments ?? string.Empty;

    public JavaHomeLocatorDelegate JavaHomeLocator { get; set; } =
        javaHomeLocator ?? (v => throw new JavaNotFoundException(v));

    public static LaunchOptionsBuilder Builder() => new();
}
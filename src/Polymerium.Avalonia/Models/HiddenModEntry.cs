namespace Polymerium.Avalonia.Models;

public sealed class HiddenModEntry
{
    public enum DuplicateKind { None, Inner, WithTopLevel }

    public required string ModId { get; init; }

    public required string Name { get; init; }

    public string? Version { get; init; }

    public ModLoaderKind? Loader { get; init; }

    public required string Host { get; init; }

    public DuplicateKind Duplicate { get; set; }
}

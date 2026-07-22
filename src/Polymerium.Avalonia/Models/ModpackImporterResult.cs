using TridentCore.Pref;

namespace Polymerium.Avalonia.Models;

public abstract record ModpackImporterResult
{
    private ModpackImporterResult() { }

    public sealed record File(string Path) : ModpackImporterResult;

    public sealed record Pref(PackageIdentifier Identifier) : ModpackImporterResult;

    public sealed record Uri(System.Uri Value) : ModpackImporterResult;
}

using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class ModpackExporterFormatModel : ModelBase
{
    #region Direct

    public required string Label { get; init; }
    public required string DisplayName { get; init; }
    public required bool SupportsOnline { get; init; }
    public required bool SupportsOffline { get; init; }
    public required bool SupportsAuthor { get; init; }
    public required bool SupportsVersion { get; init; }
    public required string CapabilityKey { get; init; }

    #endregion
}

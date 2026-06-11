using FluentIcons.Common;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class ModpackExporterFormatModel : ModelBase
{
    #region Direct

    public required string Label { get; init; }
    public required Symbol Icon { get; init; }
    public required bool SupportsOnline { get; init; }
    public required bool SupportsOffline { get; init; }

    #endregion
}

using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class ImportPathSegmentModel : ModelBase
{
    public required string Label { get; set; }

    public required string RelativePath { get; set; }

    public required bool IsCurrent { get; set; }
}

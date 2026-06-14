namespace Polymerium.Avalonia.Models;

public class DiffMarker
{
    public required double YRatio { get; init; }
    public required double HeightRatio { get; init; }
    public required DiffLineKind Kind { get; init; }
}

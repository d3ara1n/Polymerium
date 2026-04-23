namespace Polymerium.App.Models;

public class DiffLineModel
{
    public required string LeftText { get; init; }
    public required string RightText { get; init; }
    public required string LeftLineNumber { get; init; }
    public required string RightLineNumber { get; init; }
    public required DiffLineKind LeftKind { get; init; }
    public required DiffLineKind RightKind { get; init; }
}

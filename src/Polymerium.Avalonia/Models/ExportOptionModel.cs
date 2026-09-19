using FluentIcons.Common;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public abstract class ExportOptionModel : ModelBase
{
    public required string LabelKey { get; init; }
    public required Symbol Icon { get; init; }
}

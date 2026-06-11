using Polymerium.Avalonia.Facilities;
using TridentCore.Abstractions.Repositories.Resources;

namespace Polymerium.Avalonia.Models;

public class ResourceKindModel : ModelBase
{
    public required ResourceKind Value { get; init; }
    public required string Display { get; init; }
}

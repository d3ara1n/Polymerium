using Polymerium.App.Facilities;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Models;

public class ResourceKindModel : ModelBase
{
    public required ResourceKind Value { get; init; }
    public required string Display { get; init; }
}

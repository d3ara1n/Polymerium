using Polymerium.App.Facilities;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.Models;

public class SelectorTypeModel : ModelBase
{
    public required Profile.Rice.Rule.SelectorType Value { get; init; }
    public required string Display { get; init; }
}

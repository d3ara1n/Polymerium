using FluentIcons.Common;

namespace Polymerium.App.Models;

/// <summary>
///     Represents a feature item displayed in the OOBE features panel.
/// </summary>
public class OobeFeatureModel
{
    public required Symbol Icon { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
}

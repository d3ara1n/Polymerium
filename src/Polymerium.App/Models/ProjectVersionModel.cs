using Trident.Abstractions.Resources;

namespace Polymerium.App.Models;

public record ProjectVersionModel(Project.Version Inner, ProjectModel Root)
{
}
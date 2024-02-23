using System.Windows.Input;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models
{
    public record TrackedProjectVersionModel(Project.Version Inner, ProjectModel Root, ICommand UninstallCommand)
    {
    }
}
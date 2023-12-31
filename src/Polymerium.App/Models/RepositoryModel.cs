using Microsoft.UI.Xaml.Media;
using Trident.Abstractions.Repositories;

namespace Polymerium.App.Models
{
    public record RepositoryModel(IRepository Inner, Brush Background);
}

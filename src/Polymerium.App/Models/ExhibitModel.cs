using System.Windows.Input;
using Humanizer;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models;

public record ExhibitModel(Exhibit Inner, string RepositoryLabel, ICommand GotoDetailViewCommand)
{
    public string Thumbnail => Inner.Thumbnail?.AbsoluteUri ?? AssetPath.PLACEHOLDER_DEFAULT_DIRT;
    public string CreatedAt => Inner.CreatedAt.Humanize();
    public string UpdatedAt => Inner.UpdatedAt.Humanize();
    public string DownloadCount => ((int)Inner.DownloadCount).ToMetric(decimals: 2);
}
using System.Windows.Input;
using Humanizer;
using Polymerium.App.Extensions;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models;

public record ExhibitModel
{
    public Exhibit Inner { get; }
    public ICommand GotoDetailViewCommand { get; }
    public string Thumbnail => Inner.Thumbnail?.AbsoluteUri ?? AssetPath.PLACEHOLDER_DEFAULT_DIRT;
    public string CreatedAt => Inner.CreatedAt.Humanize();
    public string UpdatedAt => Inner.UpdatedAt.Humanize();
    public string DownloadCount => ((int)Inner.DownloadCount).ToMetric(decimals: 2);

    private bool hasAdded;
    public Bindable<ExhibitModel, bool> HasAdded { get; }

    public ExhibitModel(Exhibit inner, ICommand gotoDetailViewCommand)
    {
        Inner = inner;
        GotoDetailViewCommand = gotoDetailViewCommand;

        HasAdded = this.ToBindable(x => x.hasAdded, (x, v) => x.hasAdded = v);
    }
}
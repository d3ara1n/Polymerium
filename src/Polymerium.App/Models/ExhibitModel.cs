using Humanizer;
using System.Windows.Input;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models
{
    public record ExhibitModel(Exhibit Inner, IRepository Repository, ICommand GotoDetailViewCommand)
    {
        public string Thumbnail => Inner.Thumbnail?.AbsoluteUri ?? "/Assets/Placeholders/default_dirt.png";
        public string CreatedAt => Inner.CreatedAt.Humanize();
        public string UpdatedAt => Inner.UpdatedAt.Humanize();
        public string DownloadCount => ((int)Inner.DownloadCount).ToMetric(decimals: 2);
    }
}

using Humanizer;
using System.Windows.Input;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models
{
    public record ModpackModel(Exhibit Inner, ICommand GotoModpackViewCommand)
    {
        public string Thumbnail => Inner.Thumbnail?.AbsoluteUri ?? "/Assets/Placeholders/default_dirt.png";
        public string UpdatedAt => Inner.UpdatedAt.Humanize();
        public string DownloadCount => ((int)Inner.DownloadCount).ToMetric(decimals: 2);
    }
}

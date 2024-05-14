using Humanizer;
using Polymerium.Trident.Extensions;
using System.Windows.Input;
using Trident.Abstractions;

namespace Polymerium.App.Models
{
    public record RecentModel(string Key, Profile Inner, string? ThumbnailPath, ICommand GotoInstanceViewCommand)
    {
        public DateTimeOffset? PlayedAtRaw =>
            Inner.Records.ExtractDateTime(Profile.RecordData.TimelinePoint.TimelimeAction.Play);

        public string PlayedAt => PlayedAtRaw.HasValue ? PlayedAtRaw.Value.Humanize() : "Never";
        public string Thumbnail => ThumbnailPath ?? AssetPath.PLACEHOLDER_DEFAULT_DIRT;
    }
}
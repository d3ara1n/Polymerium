using System;
using System.Windows.Input;
using Humanizer;
using Polymerium.Trident.Extensions;
using Trident.Abstractions;

namespace Polymerium.App.Models;

public record RecentModel(string Key, Profile Inner, ICommand GotoInstanceViewCommand)
{
    public DateTimeOffset? PlayedAtRaw =>
        Inner.Records.ExtractDateTime(Profile.RecordData.TimelinePoint.TimelimeAction.Play);

    public string PlayedAt => PlayedAtRaw.HasValue ? PlayedAtRaw.Value.Humanize() : "Never";
    public string Thumbnail => Inner.Thumbnail?.AbsoluteUri ?? AssetPath.PLACEHOLDER_DEFAULT_DIRT;
}
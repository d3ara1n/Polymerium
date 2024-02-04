using System;
using System.Linq;
using Trident.Abstractions;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Extensions;

public static class ProfileExtensions
{
    public static string ExtractTypeDisplay(this Profile self)
    {
        var modloader = self.Metadata.Layers.SelectMany(x => x.Loaders)
            .FirstOrDefault(x => Loader.MODLOADER_NAME_MAPPINGS.Keys.Contains(x.Id));
        return modloader != null ? Loader.MODLOADER_NAME_MAPPINGS[modloader.Id] : "Vanilla";
    }

    public static DateTimeOffset? ExtractDateTime(this Profile self, Profile.RecordData.TimelinePoint.TimelimeAction action)
    {
        return self.Records.Timeline
            .Where(x => x.Action == action).MaxBy(x => x.EndTime)?.EndTime;
    }

    public static TimeSpan ExtractTimeSpan(this Profile self, Profile.RecordData.TimelinePoint.TimelimeAction action)
    {
        return self.Records.Timeline.Where(x => x.Action == action).Select(x => x.EndTime - x.BeginTime)
            .Aggregate(TimeSpan.Zero, (a, b) => a + b);
    }

    public static Filter ExtractFilter(this Profile self, ResourceKind? kind = null)
    {
        return new Filter(self.Metadata.Version,
            self.Metadata.Layers.SelectMany(x => x.Loaders)
                .FirstOrDefault(x => Loader.MODLOADER_NAME_MAPPINGS.ContainsKey(x.Id))?.Id, kind);
    }
}
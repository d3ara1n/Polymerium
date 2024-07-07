using Trident.Abstractions;

namespace Polymerium.Trident.Extensions;

public static class RecordDataExtensions
{
    public static DateTimeOffset? ExtractDateTime(this Profile.RecordData self,
        Profile.RecordData.TimelinePoint.TimelimeAction action) =>
        self.Timeline
            .Where(x => x.Action == action).MaxBy(x => x.EndTime)?.EndTime;

    public static TimeSpan ExtractTimeSpan(this Profile.RecordData self,
        Profile.RecordData.TimelinePoint.TimelimeAction action) =>
        self.Timeline.Where(x => x.Action == action).Select(x => x.EndTime - x.BeginTime)
            .Aggregate(TimeSpan.Zero, (a, b) => a + b);
}
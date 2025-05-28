namespace Trident.Abstractions.FileModels;

// 这里会事无巨细的尽可能记录数据，但详细部分仅用于分析，不会展示给用户
public class DataUser(TimeSpan totalPlayed, DateTimeOffset lastPlayed, IList<DataUser.Record>? records)
{
    #region ActionKind enum

    public enum ActionKind { Install, Update, Unlock, Reset, Rename, EditPackage, EditLoader }

    #endregion

    public const int FORMAT = 1;

    public TimeSpan TotalPlayed { get; set; } = totalPlayed;

    public DateTimeOffset LastPlayed { get; set; } = lastPlayed;

    public IList<Record> Records { get; set; } = records ?? [];

    #region Nested type: Record

    public record Record(ActionKind Kind, string? Old, string? New)
    {
        public DateTimeOffset At { get; set; } = DateTimeOffset.Now;
    }

    #endregion
}
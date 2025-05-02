namespace Trident.Abstractions.FileModels;

// 这里会事无巨细的尽可能记录数据，但详细部分仅用于分析，不会展示给用户
public class DataUser(TimeSpan totalPlayed, DateTimeOffset lastPlayed, IList<DataUser.RecordBase>? records)
{
    public const int FORMAT = 1;

    public TimeSpan TotalPlayed { get; set; } = totalPlayed;

    public DateTimeOffset LastPlayed { get; set; }

    public IList<RecordBase> Records { get; set; } = records ?? [];

    public abstract record RecordBase
    {
        public DateTimeOffset At { get; set; } = DateTimeOffset.Now;
    }

    public record InstallRecord(string? Source) : RecordBase;

    public record UpdateRecord(string Purl) : RecordBase;

    public record UnlockRecord : RecordBase;

    public record ResetRecord : RecordBase;

    public record RenameRecord(string NewName) : RecordBase;

    public record EditPackageRecord(IList<EditPackageRecord.Edit> Edits) : RecordBase
    {
        // (_,x) 新增
        // (x,_) 移除
        // (x,x) 更新
        public record Edit(string? OldPurl, string? NewPurl);
    }

    public record EditLoaderRecord(string? OldLurl, string? NewLurl) : RecordBase;
}
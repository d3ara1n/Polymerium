namespace Trident.Abstractions;


// 游戏本身需要传入 Metadata,IDictionary<string, object> Setup+Overrides, Account.Token
public record Profile(string Name, Uri? Thumbnail, string? Reference, Profile.RecordData Records, Metadata Metadata, IDictionary<string, object> Overrides, string? AccountId)
{
    public record RecordData(IList<RecordData.TimelinePoint> Timeline, string Note, IList<RecordData.Todo> Todos)
    {
        public record TimelinePoint(bool Success, string? Source, TimelinePoint.TimelimeAction Action, DateTimeOffset BeginTime, DateTimeOffset EndTime)
        {
            public enum TimelimeAction
            {
                Create,
                Update,
                Deploy,
                Play
            }
        }

        public record Todo(bool Completed, string Text);
    }
}
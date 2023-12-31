namespace Polymerium.Trident;

public record Entry(string Key, string Name, Uri? Thumbnail, string? Reference, bool IsLiked, Entry.RecordData Records)
{
    public record RecordData(string Note, IList<RecordData.TimelinePoint> Timeline, IList<RecordData.Todo> Todos)
    {
        public record TimelinePoint(bool Success, string Source, DateTimeOffset BeginTime, DateTimeOffset EndTime)
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

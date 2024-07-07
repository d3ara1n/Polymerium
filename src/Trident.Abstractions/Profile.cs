using Trident.Abstractions.Resources;

namespace Trident.Abstractions;

// 游戏本身需要传入 Metadata,IDictionary<string, object> Setup+Overrides, Account.Token
public record Profile(
    string Name,
    Attachment? Reference,
    Profile.RecordData Records,
    Metadata Metadata,
    IDictionary<string, object> Overrides,
    string? AccountId)
{
    public string Name { get; set; } = Name;
    public Attachment? Reference { get; set; } = Reference;
    public string? AccountId { get; set; } = AccountId;

    public record RecordData(IList<RecordData.TimelinePoint> Timeline, IList<RecordData.Todo> Todos, string Note)
    {
        public string Note { get; set; } = Note;

        public record TimelinePoint(
            bool Success,
            Attachment? Source,
            TimelinePoint.TimelimeAction Action,
            DateTimeOffset BeginTime,
            DateTimeOffset EndTime)
        {
            public enum TimelimeAction
            {
                Create,
                Update,
                Deploy,
                Play
            }
        }

        public record Todo(bool Completed, string Text)
        {
            public bool Completed { get; set; } = Completed;
            public string Text { get; set; } = Text;
        }
    }
}
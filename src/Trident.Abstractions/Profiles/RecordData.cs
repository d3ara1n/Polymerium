namespace Trident.Abstractions.Profiles;

public record RecordData(IList<TimelinePoint> Timeline, IList<Todo> Todos, string Note)
{
    public string Note { get; set; } = Note;
}
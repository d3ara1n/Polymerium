namespace Trident.Abstractions.Tasks;

public class TaskProgressUpdatedEventArgs(string key, TaskState state, uint? progress) : EventArgs
{
    public string Key { get; init; } = key;
    public TaskState State { get; init; } = state;
    public uint? Progress { get; init; } = progress;
}
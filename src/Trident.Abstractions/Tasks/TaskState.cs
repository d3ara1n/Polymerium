namespace Trident.Abstractions.Tasks;

public enum TaskState
{
    Idle,
    Running,
    Finished,
    Aborted,
    Faulted
}
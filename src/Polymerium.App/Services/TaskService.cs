using System.Collections.ObjectModel;
using Polymerium.App.Tasks;

namespace Polymerium.App.Services;

public class TaskService
{
    public ObservableCollection<TaskBase> Tasks { get; } = new();
}
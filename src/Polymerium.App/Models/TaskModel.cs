using System.Windows.Input;
using Microsoft.UI.Dispatching;
using Polymerium.App.Extensions;
using Polymerium.Trident.Tasks;
using Trident.Abstractions.Tasks;

namespace Polymerium.App.Models;

public record TaskModel
{
    private readonly DispatcherQueue _dispatcher;
    private bool isIndeterminate = true;
    private double progress;
    private TaskState state = TaskState.Idle;

    public TaskModel(TaskBase inner, DispatcherQueue dispatcher, ICommand abortCommand)
    {
        _dispatcher = dispatcher;
        Inner = inner;
        AbortCommand = abortCommand;

        Key = $"#{Inner.Key}";
        switch (inner)
        {
            case InstallModpackTask install:
                Title = "Install modpack";
                Subtitle = install.Version.Name;
                break;
            default:
                Title = inner.GetType().Name;
                Subtitle = "N/A";
                break;
        }

        CreatedAt = inner.CreatedAt.ToString("HH:mm");
        Progress = this.ToBindable(x => x.progress, (x, v) => x.progress = v);
        IsIndeterminate = this.ToBindable(x => x.isIndeterminate, (x, v) => x.isIndeterminate = v);
        State = this.ToBindable(x => x.state, (x, v) => x.state = v);

        inner.Subscribe(OnUpdate);
    }

    public string Key { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public TaskBase Inner { get; }
    public string CreatedAt { get; }
    public Bindable<TaskModel, double> Progress { get; }
    public Bindable<TaskModel, bool> IsIndeterminate { get; }
    public Bindable<TaskModel, TaskState> State { get; }
    public ICommand AbortCommand { get; }

    private void OnUpdate(TaskBase _, TaskProgressUpdatedEventArgs args)
    {
        _dispatcher.TryEnqueue(() =>
        {
            State.Value = args.State;
            if (args.Progress.HasValue)
            {
                IsIndeterminate.Value = false;
                Progress.Value = args.Progress.Value;
            }
            else
            {
                IsIndeterminate.Value = true;
                Progress.Value = 0;
            }
        });
    }
}
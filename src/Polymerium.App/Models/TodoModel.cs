using System.Windows.Input;
using Polymerium.App.Extensions;
using Trident.Abstractions.Profiles;

namespace Polymerium.App.Models;

public record TodoModel
{
    public TodoModel(Todo inner, ICommand deleteTodoCommand)
    {
        Inner = inner;
        DeleteTodoCommand = deleteTodoCommand;
        Completed = inner.ToBindable(x => x.Completed, (x, v) => x.Completed = v);
        Text = inner.ToBindable(x => x.Text, (x, v) => x.Text = v);
    }

    public Todo Inner { get; }
    public Bindable<Todo, bool> Completed { get; }
    public Bindable<Todo, string> Text { get; }

    public ICommand DeleteTodoCommand { get; }
}
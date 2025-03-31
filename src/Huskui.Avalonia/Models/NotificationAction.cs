using System.Windows.Input;

namespace Huskui.Avalonia.Models;

public class NotificationAction(string text, ICommand command, object? parameter = null)
{
    public NotificationAction() : this("_", null!)
    {
        // TODO: Set to DismissCommand
    }

    public string Text { get; set; } = text;
    public ICommand Command { get; set; } = command;
    public object? Parameter { get; set; } = parameter;
}
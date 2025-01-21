using System.Windows.Input;

namespace Huskui.Avalonia.Models;

public class NotificationAction
{
    public NotificationAction()
    {
        Text = string.Empty;
        // TODO: Set to DismissCommand
        Command = null!;
    }

    public NotificationAction(string text, ICommand command)
    {
        Text = text;
        Command = command;
    }

    public string Text { get; set; }
    public ICommand Command { get; set; }
}
using Microsoft.UI.Xaml.Controls;

namespace Polymerium.App.Models;

public class NotificationItem(InfoBarSeverity severity, string message)
{
    public InfoBarSeverity Severity { get; } = severity;
    public string Message { get; } = message;
}
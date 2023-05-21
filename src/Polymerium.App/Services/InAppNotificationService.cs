using Microsoft.UI.Xaml.Controls;

namespace Polymerium.App.Services;

public delegate void EnqueueNotificationItemHandler(
    string caption,
    string text,
    InfoBarSeverity severity
);

public class InAppNotificationService : INotificationService
{
    private EnqueueNotificationItemHandler? _handler;

    public void Enqueue(
        string caption,
        string text,
        InfoBarSeverity severity = InfoBarSeverity.Informational
    )
    {
        _handler?.Invoke(caption, text, severity);
    }

    public void Register(EnqueueNotificationItemHandler handler)
    {
        _handler = handler;
    }
}
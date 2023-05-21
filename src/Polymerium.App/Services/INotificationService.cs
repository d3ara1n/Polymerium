using Microsoft.UI.Xaml.Controls;

namespace Polymerium.App.Services;

public interface INotificationService
{
    void Enqueue(
        string caption,
        string text,
        InfoBarSeverity severity = InfoBarSeverity.Informational
    );
}
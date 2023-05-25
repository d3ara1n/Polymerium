using Microsoft.UI.Xaml.Controls;

namespace Polymerium.App.Models;

public class InAppNotificationItem
{
    public InAppNotificationItem(
        string caption,
        string text,
        InfoBarSeverity severity = InfoBarSeverity.Informational
    )
    {
        Caption = caption;
        Text = text;
        Severity = severity;
    }

    public string Caption { get; set; }

    public string Text { get; set; }

    public InfoBarSeverity Severity { get; set; }
}

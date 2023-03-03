namespace Polymerium.App.Models;

public class InAppNotificationItem
{
    public InAppNotificationItem(string text)
    {
        Text = text;
    }

    public string Text { get; set; }
}
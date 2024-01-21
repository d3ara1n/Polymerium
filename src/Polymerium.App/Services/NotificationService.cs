using System;

namespace Polymerium.App.Services;

public class NotificationService
{
    private Action<string>? handler;

    public void SetHandler(Action<string> action)
    {
        handler = action;
    }

    public void Enqueue(string text)
    {
        handler?.Invoke(text);
    }
}
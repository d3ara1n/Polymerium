namespace Polymerium.App.Services;

public delegate void EnqueueNotificationItemHandler(string text);

public class InAppNotificationService : INotificationService
{
    private EnqueueNotificationItemHandler? _handler;

    public void Enqueue(string text)
    {
        _handler?.Invoke(text);
    }

    public void Register(EnqueueNotificationItemHandler handler)
    {
        _handler = handler;
    }
}
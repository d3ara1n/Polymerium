namespace Polymerium.App.Services;

public interface INotificationService
{
    void Enqueue(string text);
}
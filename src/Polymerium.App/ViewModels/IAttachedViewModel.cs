namespace Polymerium.App.ViewModels;

public interface IAttachedViewModel
{
    public bool OnAttached(object? parameter) => true;

    public void OnDetached()
    {
    }
}
namespace Polymerium.App.ViewModels
{
    public interface IAttachedViewModel
    {
        public bool OnAttached(object? parameter)
        {
            return true;
        }

        public void OnDetached()
        {
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;

namespace Polymerium.App.ViewModels
{
    public abstract class ViewModelBase : ObservableObject, IAttachedViewModel
    {
        public virtual bool OnAttached(object? parameter)
        {
            return true;
        }

        public virtual void OnDetached()
        {
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;

namespace Polymerium.App.ViewModels
{
    public abstract class RecipientViewModelBase : ObservableRecipient, IAttachedViewModel
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
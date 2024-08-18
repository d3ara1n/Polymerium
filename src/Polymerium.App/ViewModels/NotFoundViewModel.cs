using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Services;
using System.Windows.Input;

namespace Polymerium.App.ViewModels;

public class NotFoundViewModel(NavigationService navigation) : ViewModelBase
{
    public ICommand GoBackCommand { get; } = new RelayCommand(navigation.GoBack, () => navigation.CanGoBack);
}
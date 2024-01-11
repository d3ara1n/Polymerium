using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Services;

namespace Polymerium.App.ViewModels;

public class DesktopViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;

    public DesktopViewModel(NavigationService navigation, ProfileManager profileManager)
    {
        GotoInstanceViewCommand = new RelayCommand<string>(GotoInstanceView);

        _navigation = navigation;
        Entries = profileManager.Managed.Select(x => new EntryModel(x.Key, x.Value.Value, GotoInstanceViewCommand))
            .OrderByDescending(x => x.LastPlayAtRaw);
    }

    public IEnumerable<EntryModel> Entries { get; private set; }

    public RelayCommand<string> GotoInstanceViewCommand { get; }

    private void GotoInstanceView(string? key)
    {
        _navigation.Navigate(typeof(InstanceView), key, new DrillInNavigationTransitionInfo());
    }
}
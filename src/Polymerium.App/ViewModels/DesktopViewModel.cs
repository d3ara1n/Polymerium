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
        _navigation = navigation;
        GotoInstanceViewCommand = new RelayCommand<string>(GotoInstanceView);

        Entries = profileManager.Managed.Select(x => new EntryModel(x.Key, x.Value.Value, GotoInstanceViewCommand))
            .OrderByDescending(x => x.LastPlayAtRaw);
    }

    public IEnumerable<EntryModel> Entries { get; private set; }

    public RelayCommand<string> GotoInstanceViewCommand { get; }

    private void GotoInstanceView(string? key)
    {
        if (key != null)
            _navigation.Navigate(typeof(InstanceView), key, new DrillInNavigationTransitionInfo());
    }
}
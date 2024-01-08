using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Managers;
using System.Collections.Generic;
using System.Linq;

namespace Polymerium.App.ViewModels
{
    public class InstanceListViewModel : ViewModelBase
    {
        private NavigationService _navigation;

        public IEnumerable<EntryModel> Entries { get; private set; }

        public RelayCommand<string> GotoDetailCommand { get; }

        public InstanceListViewModel(NavigationService navigation, EntryManager entryManager)
        {
            GotoDetailCommand = new RelayCommand<string>(GotoDetail);

            _navigation = navigation;
            Entries = entryManager.Scan().Select(x => new EntryModel(x.Key, x.Profile, GotoDetailCommand)).OrderByDescending(x => x.LastPlayAtRaw);
        }

        private void GotoDetail(string? key)
        {
            _navigation.Navigate(typeof(InstanceDetailView), key, new DrillInNavigationTransitionInfo());
        }
    }
}

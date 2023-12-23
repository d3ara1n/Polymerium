using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident;
using Polymerium.Trident.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Entries = entryManager.Entries.Select(x => new EntryModel(x.Key, x.Name, ExtractCategory(x.Reference), x.Thumbnail?.AbsoluteUri ?? string.Empty, x.IsLiked, GotoDetailCommand));
        }

        private string ExtractCategory(string? purl)
        {
            return "custom";
        }

        private void GotoDetail(string? key)
        {
            _navigation.Navigate(typeof(InstanceDetailView), key, new DrillInNavigationTransitionInfo());
        }
    }
}

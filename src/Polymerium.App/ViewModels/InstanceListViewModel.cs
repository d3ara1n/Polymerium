using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Animation;
using PackageUrl;
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
            Entries = entryManager.Scan().Select(x => new EntryModel(x.Key, x.Profile.Name, ExtractCategory(x.Profile.Reference), x.Profile.Thumbnail?.AbsoluteUri ?? string.Empty, GotoDetailCommand));
        }

        private string ExtractCategory(string? purl)
        {
            try
            {
                var pkg = new PackageURL(purl);
                return pkg.Type;
            }
            catch (MalformedPackageUrlException)
            {
                return "custom";
            }
        }

        private void GotoDetail(string? key)
        {
            _navigation.Navigate(typeof(InstanceDetailView), key, new DrillInNavigationTransitionInfo());
        }
    }
}

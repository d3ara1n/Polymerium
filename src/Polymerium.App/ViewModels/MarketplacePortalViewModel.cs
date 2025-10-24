using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;

namespace Polymerium.App.ViewModels;

public partial class MarketplacePortalViewModel(
    ConfigurationService configurationService,
    NavigationService navigationService) : ViewModelBase
{

    #region Commands

    [RelayCommand]
    private void GotoSearchView(string? query)
    {
        if (configurationService.Value.ApplicationSuperPowerActivated)
        {
            if (query == "/gamemode 1")
            {
                navigationService.Navigate<UnknownView>();
                return;
            }
        }

        navigationService.Navigate<MarketplaceSearchView>(new MarketplaceSearchViewModel.SearchArguments(query, null));
    }

    #endregion
}

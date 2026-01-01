using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Facilities;
using Polymerium.App.Modals;
using Polymerium.App.Services;
using Polymerium.App.Views;

namespace Polymerium.App.ViewModels;

public partial class MarketplacePortalViewModel(
    ConfigurationService configurationService,
    NavigationService navigationService,
    OverlayService overlayService) : ViewModelBase
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

            if (query == "Polymerium")
            {
                overlayService.PopModal(new TrophyModal());
                return;
            }
        }

        navigationService.Navigate<MarketplaceSearchView>(new MarketplaceSearchViewModel.SearchArguments(query, null));
    }

    #endregion
}

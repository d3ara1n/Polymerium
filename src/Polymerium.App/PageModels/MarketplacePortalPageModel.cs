using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Facilities;
using Polymerium.App.Modals;
using Polymerium.App.Pages;
using Polymerium.App.Services;

namespace Polymerium.App.PageModels;

public partial class MarketplacePortalPageModel(
    ConfigurationService configurationService,
    NavigationService navigationService,
    OverlayService overlayService
) : ViewModelBase
{
    #region Commands

    [RelayCommand]
    private void GotoSearchView(string? query)
    {
        if (configurationService.Value.ApplicationSuperPowerActivated)
        {
            if (query == "/gamemode 1")
            {
                navigationService.Navigate<UnknownPage>();
                return;
            }

            if (query == "Polymerium")
            {
                overlayService.PopModal(new TrophyModal());
                return;
            }
        }

        navigationService.Navigate<MarketplaceSearchPage>(
            new MarketplaceSearchPageModel.SearchArguments(query, null)
        );
    }

    #endregion
}

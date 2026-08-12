using CommunityToolkit.Mvvm.Input;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Modals;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Services;
using Velopack;

namespace Polymerium.Avalonia.PageModels;

public partial class MarketplacePortalPageModel(
    ConfigurationService configurationService,
    NavigationService navigationService,
    OverlayService overlayService,
    UpdateService updateService) : ViewModelBase
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

            if (query == "/weather clear")
            {
                var asset = new VelopackAsset
                {
                    PackageId = Program.Brand,
                    Version = new(99, 0, 0),
                    Type = VelopackAssetType.Full,
                    NotesMarkdown =
                        "# Mock Update\n\nThis is a simulated release for previewing the update flow.\n\n- Nothing will actually download\n- Reachable only with super power activated"
                };
                updateService.ApplyMockUpdate(new(new(asset, isDowngrade: false)));
                navigationService.Navigate<LandingPage>();
                return;
            }

            if (query == "Polymerium")
            {
                overlayService.PopModal(new TrophyModal());
                return;
            }
        }

        navigationService.Navigate<MarketplaceModpacksPage>(new MarketplaceModpacksPageModel.SearchArguments(query,
                                                                null));
    }

    [RelayCommand]
    private void GotoModpacks() => navigationService.Navigate<MarketplaceModpacksPage>();

    #endregion
}

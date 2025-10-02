using System.Threading.Tasks;
using Polymerium.App.Facilities;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels;

public class LandingViewModel(NavigationService navigationService) : ViewModelBase
{
    #region Overrides

    protected override Task OnInitializeAsync()
    {
        // This page is always the root page
        navigationService.ClearHistory();
        return base.OnInitializeAsync();
    }

    #endregion
}

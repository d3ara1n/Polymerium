using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Services;
using Polymerium.App.Views;

namespace Polymerium.App.ViewModels;

public partial class NewInstanceViewModel : ObservableObject
{
    private readonly IOverlayService _overlayService;
    private readonly NavigationService _navigationService;

    public NewInstanceViewModel(IOverlayService overlayService, NavigationService navigationService)
    {
        _overlayService = overlayService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public void OpenWizard()
    {
        _overlayService.Show(new CreateInstanceWizardDialog() { OverlayService = _overlayService });
    }

    [RelayCommand]
    public void GotoSearchPage()
    {
        _navigationService.Navigate<SearchCenterView>();
    }
}
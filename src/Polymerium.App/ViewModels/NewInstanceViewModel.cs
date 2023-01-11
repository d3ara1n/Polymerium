using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Services;
using Polymerium.App.Views;

namespace Polymerium.App.ViewModels;

public partial class NewInstanceViewModel : ObservableObject
{
    private readonly IOverlayService _overlayService;

    public NewInstanceViewModel(IOverlayService overlayService)
    {
        _overlayService = overlayService;
    }

    [RelayCommand]
    public void OpenWizard()
    {
        _overlayService.Show(new CreateInstanceWizardDialog() { OverlayService = _overlayService });
    }
}

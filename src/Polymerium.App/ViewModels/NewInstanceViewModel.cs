using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Services;
using Polymerium.App.Views;
using System.IO;
using System.Windows.Input;

namespace Polymerium.App.ViewModels;

public class NewInstanceViewModel : ObservableObject
{
    private readonly NavigationService _navigationService;
    private readonly IOverlayService _overlayService;

    public NewInstanceViewModel(IOverlayService overlayService, NavigationService navigationService)
    {
        _overlayService = overlayService;
        _navigationService = navigationService;
        OpenWizardCommand = new RelayCommand(OpenWizard);
        GotoSearchPageCommand = new RelayCommand(GotoSearchPage);
    }

    public ICommand OpenWizardCommand { get; }
    public ICommand GotoSearchPageCommand { get; }

    public void OpenWizard()
    {
        _overlayService.Show(new CreateInstanceWizardDialog { OverlayService = _overlayService });
    }

    public void GotoSearchPage()
    {
        _navigationService.Navigate<SearchCenterView>(
            new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight }
        );
    }

    public void ArchiveAccepted(string fileName)
    {
        if (File.Exists(fileName))
        {
            var dialog = new ImportModpackWizardDialog(fileName);
            dialog.OverlayService = _overlayService;
            _overlayService.Show(dialog);
        }
    }
}

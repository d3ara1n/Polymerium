using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Services;
using Polymerium.App.Views;

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
        _navigationService.Navigate<SearchCenterView>();
    }

    public async Task ArchiveAcceptedAsync(string fileName)
    {
        // TODO: 放到对话框中：第一阶段确认信息并提供机会修改实例名和目录名。第二阶段转圈圈
        if (File.Exists(fileName))
        {
            var dialog = new ImportModpackWizardDialog(fileName);
            dialog.OverlayService = _overlayService;
            _overlayService.Show(dialog);
        }
    }
}
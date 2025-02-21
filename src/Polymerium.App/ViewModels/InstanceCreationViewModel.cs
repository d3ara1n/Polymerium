using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Dialogs;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Services;
using System;
using System.Linq;

namespace Polymerium.App.ViewModels;

public partial class InstanceCreationViewModel : ViewModelBase
{
    #region Injected

    private readonly OverlayService _overlayService;
    private readonly PrismLauncherService _prismLauncherService;

    public InstanceCreationViewModel(OverlayService overlayService, PrismLauncherService prismLauncherService)
    {
        _overlayService = overlayService;
        _prismLauncherService = prismLauncherService;
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void PickVersion()
    {
        _overlayService.PopDialog(new VersionPickerDialog(async token =>
        {
            if (token.IsCancellationRequested) return Enumerable.Empty<GameVersionModel>().ToList();

            var index = await _prismLauncherService.GetGameVersionsAsync();
            throw new NotImplementedException();
        }));
    }

    #endregion
}
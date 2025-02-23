using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Assets;
using Polymerium.App.Dialogs;
using Polymerium.App.Exceptions;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using Polymerium.Trident.Utilities;

namespace Polymerium.App.ViewModels;

public partial class InstanceHomeViewModel : ViewModelBase
{
    public InstanceHomeViewModel(ViewBag bag, ProfileManager profileManager, OverlayService overlayService)
    {
        _profileManager = profileManager;
        _overlayService = overlayService;

        if (bag.Parameter is string key)
        {
            if (profileManager.TryGetImmutable(key, out var profile))
            {
                Basic = new InstanceBasicModel(key,
                                               profile.Name,
                                               profile.Setup.Version,
                                               profile.Setup.Loader,
                                               profile.Setup.Source);
                var screenshotPath = ProfileHelper.PickScreenshotRandomly(key);
                Screenshot = screenshotPath is not null
                                 ? new Bitmap(screenshotPath)
                                 : AssetUriIndex.WALLPAPER_IMAGE_BITMAP;
                PackageCount = profile.Setup.Stage.Count + profile.Setup.Stash.Count;
            }
            else
            {
                throw new PageNotReachedException(typeof(InstanceView),
                                                  $"Key '{key}' is not valid instance or not found");
            }
        }
        else
        {
            throw new PageNotReachedException(typeof(InstanceView), "Key to the instance is not provided");
        }
    }

    #region Injected

    private readonly ProfileManager _profileManager;
    private readonly OverlayService _overlayService;

    #endregion

    #region Reactive

    [ObservableProperty]
    private InstanceBasicModel _basic;

    [ObservableProperty]
    private Bitmap _screenshot;

    [ObservableProperty]
    private int _packageCount;

    #endregion

    #region Commands

    [RelayCommand]
    private void SwitchAccount() => _overlayService.PopDialog(new AccountPickerDialog());

    #endregion
}
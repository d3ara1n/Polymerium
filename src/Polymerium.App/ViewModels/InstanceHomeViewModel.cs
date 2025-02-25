using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
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
using Polymerium.Trident.Services.Instances;
using Polymerium.Trident.Utilities;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Tasks;

namespace Polymerium.App.ViewModels;

public partial class InstanceHomeViewModel : ViewModelBase
{
    public InstanceHomeViewModel(
        ViewBag bag,
        ProfileManager profileManager,
        OverlayService overlayService,
        InstanceManager instanceManager)
    {
        _profileManager = profileManager;
        _overlayService = overlayService;
        _instanceManager = instanceManager;

        if (bag.Parameter is string key)
        {
            if (profileManager.TryGetImmutable(key, out var profile))
            {
                UpdateModels(key, profile);
                var screenshotPath = ProfileHelper.PickScreenshotRandomly(key);
                Screenshot = screenshotPath is not null
                                 ? new Bitmap(screenshotPath)
                                 : AssetUriIndex.WALLPAPER_IMAGE_BITMAP;
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

    private void UpdateModels(string key, Profile profile)
    {
        Basic = new InstanceBasicModel(key,
                                       profile.Name,
                                       profile.Setup.Version,
                                       profile.Setup.Loader,
                                       profile.Setup.Source);
        PackageCount = profile.Setup.Stage.Count + profile.Setup.Stash.Count;
    }

    #region Commands

    [RelayCommand]
    private void SwitchAccount() => _overlayService.PopDialog(new AccountPickerDialog());

    #endregion

    #region Tracking

    protected override Task OnInitializedAsync(CancellationToken token)
    {
        _instanceManager.InstanceUpdating += OnProfileUpdating;
        _profileManager.ProfileUpdated += OnProfileUpdated;
        if (_instanceManager.IsTracking(Basic.Key, out var tracker))
            if (tracker is UpdateTracker update)
            {
                // 已经处于更新状态而未收到事件
                State = InstanceState.Updating;
                update.StateUpdated += OnProfileUpdateStateChanged;
            }

        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync(CancellationToken token)
    {
        _instanceManager.InstanceUpdating -= OnProfileUpdating;
        _profileManager.ProfileUpdated -= OnProfileUpdated;

        return Task.CompletedTask;
    }

    private void OnProfileUpdateStateChanged(TrackerBase sender, TrackerState state)
    {
        if (sender is UpdateTracker update)
            if (state is TrackerState.Faulted or TrackerState.Finished)
            {
                update.StateUpdated -= OnProfileUpdateStateChanged;
                Dispatcher.UIThread.Post(() => State = InstanceState.Idle);
                // 更新的事情交给 ProfileManager.ProfileUpdated
            }
    }

    private void OnProfileUpdating(object? sender, UpdateTracker tracker)
    {
        Dispatcher.UIThread.Post(() => State = InstanceState.Updating);

        tracker.StateUpdated += OnProfileUpdateStateChanged;
    }

    private void OnProfileUpdated(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        UpdateModels(e.Key, e.Value);
    }

    #endregion

    #region Injected

    private readonly ProfileManager _profileManager;
    private readonly OverlayService _overlayService;
    private readonly InstanceManager _instanceManager;

    #endregion

    #region Reactive

    [ObservableProperty]
    private InstanceBasicModel _basic;

    [ObservableProperty]
    private Bitmap _screenshot;

    [ObservableProperty]
    private int _packageCount;

    [ObservableProperty]
    private InstanceState _state = InstanceState.Idle;

    #endregion
}
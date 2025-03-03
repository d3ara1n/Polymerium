using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Dialogs;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Toasts;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using Polymerium.Trident.Utilities;
using System;
using System.Reactive.Linq;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.ViewModels;

public partial class InstanceHomeViewModel : InstanceViewModelBase
{
    private IDisposable? _subscription;

    public InstanceHomeViewModel(
        ViewBag bag,
        ProfileManager profileManager,
        OverlayService overlayService,
        InstanceManager instanceManager,
        NotificationService notificationService) : base(bag, instanceManager, profileManager)
    {
        _overlayService = overlayService;
        _notificationService = notificationService;
    }

    protected override void OnUpdateModel(string key, Profile profile)
    {
        var screenshotPath = ProfileHelper.PickScreenshotRandomly(key);
        Screenshot = screenshotPath is not null ? new Bitmap(screenshotPath) : AssetUriIndex.WALLPAPER_IMAGE_BITMAP;
        PackageCount = profile.Setup.Stage.Count + profile.Setup.Stash.Count;
        base.OnUpdateModel(key, profile);
    }

    #region Tracking

    protected override void OnInstanceDeploying(DeployTracker tracker)
    {
        _subscription?.Dispose();
        _subscription = Observable
                       .Interval(TimeSpan.FromSeconds(1))
                       .Subscribe(_ =>
                        {
                            var progress = tracker.Progress;
                            if (progress.Percentage.HasValue)
                            {
                                DeployingProgress = progress.Percentage.Value;
                                DeployingPending = false;
                            }
                            else
                            {
                                DeployingPending = true;
                            }

                            DeployingMessage = progress.Message;
                        });
        base.OnInstanceDeploying(tracker);
    }

    protected override void OnInstanceUpdated(UpdateTracker tracker)
    {
        _subscription?.Dispose();
    }

    protected override void OnInstanceDeployed(DeployTracker tracker)
    {
        _subscription?.Dispose();
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void SwitchAccount() => _overlayService.PopDialog(new AccountPickerDialog());

    [RelayCommand]
    private void Play()
    {
        try
        {
            InstanceManager.Deploy(Basic.Key);
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex.Message, "Update failed", NotificationLevel.Danger);
        }
    }

    [RelayCommand]
    private void Abort()
    {
        if (InstanceManager.IsTracking(Basic.Key, out var tracker) && tracker is DeployTracker)
            tracker.Abort();
    }

    [RelayCommand]
    private void Stop()
    {
        State = InstanceState.Idle;
    }

    [RelayCommand]
    private void OpenDashboard()
    {
        _overlayService.PopToast(new ExhibitionModpackToast());
    }

    #endregion

    #region Injected

    private readonly OverlayService _overlayService;
    private readonly NotificationService _notificationService;

    #endregion

    #region Reactive

    [ObservableProperty]
    private Bitmap _screenshot;

    [ObservableProperty]
    private int _packageCount;

    [ObservableProperty]
    private double _deployingProgress;

    [ObservableProperty]
    private string _deployingMessage;

    [ObservableProperty]
    private bool _deployingPending;

    #endregion
}
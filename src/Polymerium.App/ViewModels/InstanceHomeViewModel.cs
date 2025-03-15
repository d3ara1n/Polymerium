using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Assets;
using Polymerium.App.Dialogs;
using Polymerium.App.Facilities;
using Polymerium.App.Services;
using Polymerium.App.Toasts;
using Polymerium.Trident.Engines.Deploying;
using Polymerium.Trident.Igniters;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using Polymerium.Trident.Utilities;
using Trident.Abstractions.Extensions;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.ViewModels;

public partial class InstanceHomeViewModel : InstanceViewModelBase
{
    private CompositeDisposable? _subscription;

    public InstanceHomeViewModel(
        ViewBag bag,
        ProfileManager profileManager,
        OverlayService overlayService,
        InstanceManager instanceManager,
        NotificationService notificationService,
        ConfigurationService configurationService) : base(bag, instanceManager, profileManager)
    {
        _overlayService = overlayService;
        _notificationService = notificationService;
        _configurationService = configurationService;
    }

    protected override void OnUpdateModel(string key, Profile profile)
    {
        base.OnUpdateModel(key, profile);
        var screenshotPath = ProfileHelper.PickScreenshotRandomly(key);
        Screenshot = screenshotPath is not null ? new Bitmap(screenshotPath) : AssetUriIndex.WALLPAPER_IMAGE_BITMAP;
        PackageCount = profile.Setup.Stage.Count + profile.Setup.Stash.Count;
    }

    #region Tracking

    protected override void OnInstanceDeploying(DeployTracker tracker)
    {
        base.OnInstanceDeploying(tracker);
        _subscription?.Dispose();
        _subscription = new CompositeDisposable();
        DeployingMessage = GetStageTitle(tracker.CurrentStage);
        tracker
           .ProgressStream.Buffer(TimeSpan.FromSeconds(1))
           .Where(x => x.Any())
           .Select(x => x.Last())
           .Subscribe(x =>
            {
                DeployingProgressTotal = x.Item2;
                DeployingProgressCurrent = x.Item1;
                DeployingPending = false;
            })
           .DisposeWith(tracker)
           .DisposeWith(_subscription);
        tracker
           .StageStream.Subscribe(stage =>
            {
                DeployingMessage = GetStageTitle(stage);
                DeployingPending = true;
            })
           .DisposeWith(tracker)
           .DisposeWith(_subscription);
    }

    private string GetStageTitle(DeployStage stage) =>
        stage switch
        {
            DeployStage.CheckArtifact => "Checking artifacts...",
            DeployStage.InstallVanilla => "Installing vanilla...",
            DeployStage.ProcessLoader => "Processing loader...",
            DeployStage.ResolvePackage => "Resolving packages...",
            DeployStage.BuildArtifact => "Building artifacts...",
            DeployStage.GenerateManifest => "Generating manifest...",
            DeployStage.SolidifyManifest => "Solidifying files...",
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
        };

    #endregion

    #region Commands

    [RelayCommand]
    private void SwitchAccount() => _overlayService.PopDialog(new AccountPickerDialog());

    [RelayCommand]
    private void Play()
    {
        try
        {
            var profile = ProfileManager.GetImmutable(Basic.Key);
            var options = new LaunchOptions(javaHomeLocator: major => profile.GetOverride(Profile.OVERRIDE_JAVA_HOME,
                                                                          major switch
                                                                          {
                                                                              8 => _configurationService.Value
                                                                                 .RuntimeJavaHome8,
                                                                              11 => _configurationService.Value
                                                                                 .RuntimeJavaHome11,
                                                                              17 => _configurationService.Value
                                                                                 .RuntimeJavaHome17,
                                                                              21 => _configurationService.Value
                                                                                 .RuntimeJavaHome21,
                                                                              _ => throw new
                                                                                  ArgumentOutOfRangeException(nameof
                                                                                          (major),
                                                                                      major,
                                                                                      "Not supported java version")
                                                                          })
                                                                   ?? throw new
                                                                          InvalidOperationException("Java home fallback unset"),
                                            additionalArguments:
                                            profile.GetOverride(Profile.OVERRIDE_JAVA_ADDITIONAL_ARGUMENTS,
                                                                _configurationService.Value
                                                                   .GameJavaAdditionalArguments),
                                            maxMemory: profile.GetOverride(Profile.OVERRIDE_JAVA_MAX_MEMORY,
                                                                           _configurationService.Value
                                                                              .GameJavaMaxMemory),
                                            windowSize:
                                            (profile.GetOverride(Profile.OVERRIDE_WINDOW_WIDTH, _configurationService.Value.GameWindowInitialWidth),
                                             profile.GetOverride(Profile.OVERRIDE_WINDOW_HEIGHT,
                                                                 _configurationService.Value.GameWindowInitialHeight)),
                                            launchMode: LaunchMode.Managed);
            InstanceManager.DeployAndLaunch(Basic.Key, options);
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex, "Update failed");
        }
    }

    [RelayCommand]
    private void Abort()
    {
        if (InstanceManager.IsTracking(Basic.Key, out var tracker) && tracker is DeployTracker)
            tracker.Abort();
    }

    [RelayCommand]
    private void Eject()
    {
        if (InstanceManager.IsTracking(Basic.Key, out var tracker) && tracker is LaunchTracker launch)
        {
            launch.IsDetaching = true;
            tracker.Abort();
        }
    }

    [RelayCommand]
    private void Stop()
    {
        if (InstanceManager.IsTracking(Basic.Key, out var tracker) && tracker is LaunchTracker)
            tracker.Abort();
    }

    [RelayCommand]
    private void OpenDashboard()
    {
        if (InstanceManager.IsTracking(Basic.Key, out var tracker) && tracker is LaunchTracker launch)
        {
            var toast = new InstanceDashboardToast();
            toast.SetItems(launch.ScrapBuffer);
            _overlayService.PopToast(toast);
        }
    }

    [RelayCommand]
    private void SwitchMode()
    {
        Mode = Mode switch
        {
            LaunchMode.Managed => LaunchMode.FireAndForget,
            LaunchMode.FireAndForget => LaunchMode.Managed,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    #endregion

    #region Injected

    private readonly OverlayService _overlayService;
    private readonly NotificationService _notificationService;
    private readonly ConfigurationService _configurationService;

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial Bitmap? Screenshot { get; set; }

    [ObservableProperty]
    public partial int PackageCount { get; set; }

    [ObservableProperty]
    public partial double DeployingProgressTotal { get; set; }

    [ObservableProperty]
    public partial double DeployingProgressCurrent { get; set; }

    [ObservableProperty]
    public partial string DeployingMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool DeployingPending { get; set; }

    [ObservableProperty]
    public partial LaunchMode Mode { get; set; } = LaunchMode.Managed;

    #endregion
}
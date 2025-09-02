using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Dialogs;
using Polymerium.App.Exceptions;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Toasts;
using Polymerium.App.Utilities;
using Polymerium.App.Views;
using Polymerium.App.Widgets;
using Trident.Core.Engines.Deploying;
using Trident.Core.Igniters;
using Trident.Core.Services;
using Trident.Core.Services.Instances;
using Trident.Core.Utilities;
using Trident.Abstractions.Extensions;
using Trident.Abstractions.FileModels;
using Resources = Polymerium.App.Properties.Resources;

namespace Polymerium.App.ViewModels;

public partial class InstanceHomeViewModel(
    ViewBag bag,
    ProfileManager profileManager,
    OverlayService overlayService,
    InstanceManager instanceManager,
    NavigationService navigationService,
    NotificationService notificationService,
    ConfigurationService configurationService,
    PersistenceService persistenceService,
    ScrapService scrapService,
    InstanceService instanceService,
    WidgetHostService widgetHostService) : InstanceViewModelBase(bag, instanceManager, profileManager)
{
    // Launch Lifecycle
    private CompositeDisposable? _subscription;
    private IDisposable? _timerSubscription;


    #region Other

    private void UpdateTime(string key)
    {
        var activity = persistenceService.GetLastActivity(key);
        LastPlayedAtRaw = activity?.End;
        LastPlayTimeRaw = activity?.End - activity?.Begin ?? TimeSpan.Zero;
        TotalPlayTimeRaw = persistenceService.GetTotalPlayTime(key);
        PercentageInTotalPlayTime = persistenceService.GetPercentageInTotalPlayTime(key);
    }

    internal void ViewForTimerLaunch()
    {
        _timerSubscription?.Dispose();
        if (InstanceManager.IsTracking(Basic.Key, out var tracker) && tracker is LaunchTracker launch)
        {
            var start = DateTimeOffset.Now - launch.StartedAt;
            _timerSubscription = Observable
                                .Interval(TimeSpan.FromSeconds(1))
                                .Subscribe(x => TimerCount = start + TimeSpan.FromSeconds(x));
        }
    }

    internal void ViewForTimerDestruct() => _timerSubscription?.Dispose();

    #endregion

    #region Overrides

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        base.OnInitializeAsync(token);

        var selector = persistenceService.GetAccountSelector(Basic.Key);
        if (selector != null)
        {
            var account = persistenceService.GetAccount(selector.Uuid);
            if (account != null)
            {
                var cooked = AccountHelper.ToCooked(account);
                SelectedAccount = new(cooked.GetType(),
                                      cooked.Uuid,
                                      cooked.Username,
                                      account.EnrolledAt,
                                      account.LastUsedAt);
            }
        }

        foreach (var widget in Widgets.Where(x => widgetHostService.GetIsPinned(Basic.Key, x.GetType().Name)))
        {
            PinnedWidgets.Add(widget);
        }


        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync(CancellationToken token)
    {
        _subscription?.Dispose();
        _timerSubscription?.Dispose();
        foreach (var widget in PinnedWidgets)
        {
            widget.DeinitializeAsync();
        }

        PinnedWidgets.Clear();
        return base.OnDeinitializeAsync(token);
    }


    protected override void OnModelUpdated(string key, Profile profile)
    {
        base.OnModelUpdated(key, profile);
        var screenshotPath = ProfileHelper.PickScreenshotRandomly(key);
        Screenshot = screenshotPath is not null ? new(screenshotPath) : AssetUriIndex.WallpaperImageBitmap;
        PackageCount = profile.Setup.Packages.Count;
        UpdateTime(key);
    }

    #endregion

    #region Tracking

    protected override void OnInstanceDeploying(DeployTracker tracker)
    {
        base.OnInstanceDeploying(tracker);
        _subscription?.Dispose();
        _subscription = new();
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
            DeployStage.CheckArtifact => Resources.DeployStage_CheckArtifact,
            DeployStage.InstallVanilla => Resources.DeployStage_InstallVanilla,
            DeployStage.ProcessLoader => Resources.DeployStage_ProcessLoader,
            DeployStage.ResolvePackage => Resources.DeployStage_ResolvePackage,
            DeployStage.BuildArtifact => Resources.DeployStage_BuildArtifact,
            DeployStage.EnsureRuntime => Resources.DeployStage_EnsureRuntime,
            DeployStage.GenerateManifest => Resources.DeployStage_GenerateManifest,
            DeployStage.SolidifyManifest => Resources.DeployStage_SolidifyManifest,
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
        };

    protected override void OnInstanceLaunched(LaunchTracker tracker)
    {
        base.OnInstanceLaunched(tracker);

        UpdateTime(Basic.Key);
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task SwitchAccountAsync()
    {
        var accounts = persistenceService
                      .GetAccounts()
                      .Select(x =>
                       {
                           var cooked = AccountHelper.ToCooked(x);
                           return SelectedAccount?.Uuid == cooked.Uuid
                                      ? SelectedAccount
                                      : new(cooked.GetType(),
                                            cooked.Uuid,
                                            cooked.Username,
                                            x.EnrolledAt,
                                            x.LastUsedAt);
                       })
                      .ToList();
        var dialog = new AccountPickerDialog
        {
            GotoManagerViewCommand = OpenAccountsViewCommand,
            AccountsSource = accounts,
            Result = SelectedAccount
        };
        if (await overlayService.PopDialogAsync(dialog) && dialog.Result is AccountModel account)
        {
            SelectedAccount = account;
            persistenceService.SetAccountSelector(Basic.Key, account.Uuid);
        }
    }

    [RelayCommand]
    private async Task PlayAsync()
    {
        try
        {
            await instanceService.DeployAndLaunchAsync(Basic.Key, Mode);
        }
        catch (AccountNotFoundException)
        {
            notificationService.PopMessage(Resources.InstanceHomeView_AccountNotFoundDangerNotificationPrompt,
                                           Resources.InstanceHomeView_AccountNotFoundDangerNotificationTitle,
                                           NotificationLevel.Danger,
                                           actions:
                                           [
                                               new(Resources
                                                      .InstanceHomeView_AccountNotFoundDangerNotificationSelectActionText,
                                                   SwitchAccountCommand)
                                           ]);
        }
        catch (AccountInvalidException ex)
        {
            notificationService.PopMessage(ex,
                                           Resources.InstanceHomeView_AccountAuthenticationDangerNotificationTitle);
        }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex, Resources.InstanceHomeView_DeployDangerNotificationTitle);
        }
    }

    [RelayCommand]
    private void Abort()
    {
        if (InstanceManager.IsTracking(Basic.Key, out var tracker) && tracker is DeployTracker)
        {
            tracker.Abort();
        }
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
        {
            tracker.Abort();
        }
    }

    [RelayCommand]
    private void OpenDashboard()
    {
        if (InstanceManager.IsTracking(Basic.Key, out var tracker) && tracker is LaunchTracker launch)
        {
            var toast = new InstanceDashboardToast { Header = Basic.Name };
            if (scrapService.TryGetBuffer(launch.Key, out var buffer))
            {
                toast.SetItems(buffer);
            }

            overlayService.PopToast(toast);
        }
    }


    [RelayCommand]
    private void OpenAccountsView(Dialog? self)
    {
        if (self != null)
        {
            navigationService.Navigate<AccountsView>();
            self.Dismiss();
        }
    }

    [RelayCommand]
    private void SwitchMode() =>
        Mode = Mode switch
        {
            LaunchMode.Managed => LaunchMode.FireAndForget,
            LaunchMode.FireAndForget => configurationService.Value.ApplicationSuperPowerActivated
                                            ? LaunchMode.Debug
                                            : LaunchMode.Managed,
            LaunchMode.Debug => LaunchMode.Managed,
            _ => throw new ArgumentOutOfRangeException()
        };

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LastPlayedAt))]
    public partial DateTimeOffset? LastPlayedAtRaw { get; set; }

    public string LastPlayedAt => LastPlayedAtRaw.Humanize();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LastPlayTime))]
    public partial TimeSpan LastPlayTimeRaw { get; set; }

    public string LastPlayTime => LastPlayTimeRaw.Humanize(maxUnit: TimeUnit.Day, minUnit: TimeUnit.Second);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPlayTime))]
    public partial TimeSpan TotalPlayTimeRaw { get; set; }

    public double TotalPlayTime => TotalPlayTimeRaw.TotalHours;

    [ObservableProperty]
    public partial double PercentageInTotalPlayTime { get; set; }

    [ObservableProperty]
    public partial TimeSpan TimerCount { get; set; }

    [ObservableProperty]
    public partial AccountModel? SelectedAccount { get; set; }

    public ObservableCollection<WidgetBase> PinnedWidgets { get; } = [];

    #endregion
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.Avalonia.Assets;
using Polymerium.Avalonia.Dialogs;
using Polymerium.Avalonia.Exceptions;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using Polymerium.Avalonia.Widgets;
using TridentCore.Abstractions.Extensions;
using TridentCore.Abstractions.FileModels;
using TridentCore.Core.Engines.Deploying;
using TridentCore.Core.Exceptions;
using TridentCore.Core.Igniters;
using TridentCore.Core.Services;
using TridentCore.Core.Services.Instances;
using TridentCore.Core.Utilities;

namespace Polymerium.Avalonia.PageModels;

public partial class InstanceHomePageModel(
    IViewContext<InstancePageModelBase.InstanceContextParameter> context,
    ProfileManager profileManager,
    OverlayService overlayService,
    InstanceStateAggregator aggregator,
    InstanceManager instanceManager,
    NavigationService navigationService,
    NotificationService notificationService,
    ConfigurationService configurationService,
    PersistenceService persistenceService,
    InstanceService instanceService,
    WidgetHostService widgetHostService) : InstancePageModelBase(context, aggregator, instanceManager, profileManager)
{
    private IDisposable? _timerSubscription;

    #region Other

    private void UpdateTime(string key)
    {
        var activity = persistenceService.GetLastActivity(key);
        LastPlayedAtRaw = DateTimeHelper.FromPersistedLocalDateTime(activity?.End);
        LastPlayTimeRaw = activity?.End - activity?.Begin ?? TimeSpan.Zero;
        TotalPlayTimeRaw = persistenceService.GetTotalPlayTime(key);
        PercentageInTotalPlayTime = persistenceService.GetPercentageInTotalPlayTime(key);
    }

    internal void ViewForTimerLaunch()
    {
        _timerSubscription?.Dispose();
        if (InstanceManager.ActivityOf(Basic.Key) is InstanceActivity.Running running)
        {
            var start = DateTimeOffset.Now - running.StartedAt;
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
        var selector = persistenceService.GetAccountSelector(Basic.Key);
        if (selector != null)
        {
            var account = persistenceService.GetAccount(selector.Uuid);
            if (account != null)
            {
                var cooked = AccountHelper.ToCooked(account);
                SelectedAccount = AccountHelper.CreateModelFromAccount(cooked,
                                                                       DateTimeHelper
                                                                          .FromPersistedLocalDateTime(account
                                                                              .EnrolledAt),
                                                                       DateTimeHelper
                                                                          .FromPersistedLocalDateTime(account
                                                                              .LastUsedAt));
            }
        }

        foreach (var widget in Widgets.Where(x => widgetHostService.GetIsPinned(Basic.Key, x.GetType().Name)))
        {
            PinnedWidgets.Add(widget);
        }

        return base.OnInitializeAsync(token);
    }

    protected override Task OnDeinitializeAsync()
    {
        _timerSubscription?.Dispose();

        PinnedWidgets.Clear();
        return Task.CompletedTask;
    }

    protected override void OnModelUpdated(string key, Profile profile)
    {
        base.OnModelUpdated(key, profile);
        Screenshot ??= GetRandomScreenshot(key);
        PackageCount = profile.Setup.Packages.Count;
        UpdateTime(key);
    }

    private Bitmap GetRandomScreenshot(string key)
    {
        var screenshotPath = InstanceHelper.PickScreenshotRandomly(key);
        return screenshotPath is not null ? new(screenshotPath) : AssetUriIndex.WallpaperImageBitmap;
    }

    #endregion

    #region Tracking

    protected override void OnActivityStarted(InstanceActivity activity)
    {
        base.OnActivityStarted(activity);
        ApplyDeploying(activity);
    }

    protected override void OnActivityProgressed(InstanceActivity activity)
    {
        base.OnActivityProgressed(activity);
        ApplyDeploying(activity);
    }

    protected override void OnActivityCompleted(InstanceActivity activity)
    {
        base.OnActivityCompleted(activity);

        if (activity is InstanceActivity.Running)
        {
            UpdateTime(Basic.Key);
        }
    }

    // 每帧快照都自带阶段与文件计数，直接读即可，无需分别订阅两条流再合并。
    private void ApplyDeploying(InstanceActivity activity)
    {
        if (activity is not InstanceActivity.Deploying deploying)
        {
            return;
        }

        DeployingMessage = deploying.CurrentStage;
        if (deploying.FileCount is { Total: > 0 } count)
        {
            DeployingProgress = (double)count.Current / count.Total;
            DeployingProgressCurrent = count.Current;
            DeployingProgressTotal = count.Total;
            HasDeployingFileCount = true;
            DeployingPending = false;
        }
        else
        {
            HasDeployingFileCount = false;
            DeployingPending = true;
        }
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
                                      : AccountHelper.CreateModelFromAccount(cooked,
                                                                             DateTimeHelper.FromPersistedLocalDateTime(x
                                                                                .EnrolledAt),
                                                                             DateTimeHelper.FromPersistedLocalDateTime(x
                                                                                .LastUsedAt));
                       })
                      .ToList();
        var dialog = new AccountPickerDialog
        {
            GotoManagerViewCommand = OpenAccountsPageCommand,
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
    private void Play()
    {
        try
        {
            instanceService.DeployAndLaunch(Basic.Key, Mode);
        }
        catch (AccountNotFoundException)
        {
            notificationService.PopMessage(LanguageManager.Instance.InstanceHomePage_AccountNotFoundDangerNotificationMessage.Current(),
                                           LanguageManager.Instance.InstanceHomePage_AccountNotFoundDangerNotificationTitle.Current(),
                                           GrowlLevel.Danger,
                                           thumbnail:
                                           SelectedAccount?.FaceUrl ?? ThumbnailHelper.ForInstance(Basic.Key),
                                           actions:
                                           [
                                               new(LanguageManager.Instance.InstanceHomePage_AccountNotFoundDangerNotificationSelectActionText.Current(),
                                                   SwitchAccountCommand)
                                           ]);
        }
        catch (AccountException ex)
        {
            notificationService.PopMessage(ex,
                                           LanguageManager.Instance.InstanceHomePage_AccountAuthenticationDangerNotificationTitle.Current(),
                                           thumbnail: SelectedAccount?.FaceUrl
                                                   ?? ThumbnailHelper.ForInstance(Basic.Key));
        }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex,
                                           LanguageManager.Instance.InstanceHomePage_DeployDangerNotificationTitle.Current(),
                                           thumbnail: ThumbnailHelper.ForInstance(Basic.Key));
        }
    }

    [RelayCommand]
    private void Abort()
    {
        if (InstanceManager.ActivityOf(Basic.Key) is InstanceActivity.Deploying)
        {
            InstanceManager.Abort(Basic.Key);
        }
    }

    [RelayCommand]
    private void Eject() => InstanceManager.Detach(Basic.Key);

    [RelayCommand]
    private void Stop()
    {
        if (InstanceManager.ActivityOf(Basic.Key) is InstanceActivity.Running)
        {
            InstanceManager.Abort(Basic.Key);
        }
    }

    [RelayCommand]
    private void OpenAccountsPage(Dialog? self)
    {
        if (self != null)
        {
            navigationService.Navigate<AccountsPage>();
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
    public partial double DeployingProgress { get; set; }

    [ObservableProperty]
    public partial int DeployingProgressCurrent { get; set; }

    [ObservableProperty]
    public partial int DeployingProgressTotal { get; set; }

    [ObservableProperty]
    public partial bool HasDeployingFileCount { get; set; }

    [ObservableProperty]
    public partial DeployStage DeployingMessage { get; set; }

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

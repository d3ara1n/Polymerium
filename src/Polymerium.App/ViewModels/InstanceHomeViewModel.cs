using System;
using System.Linq;
using System.Net;
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
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Toasts;
using Polymerium.App.Utilities;
using Polymerium.App.Views;
using Polymerium.Trident.Accounts;
using Polymerium.Trident.Engines.Deploying;
using Polymerium.Trident.Igniters;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using Polymerium.Trident.Utilities;
using Refit;
using Trident.Abstractions.Extensions;
using Trident.Abstractions.FileModels;

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
    MicrosoftService microsoftService,
    MinecraftService minecraftService,
    XboxLiveService xboxLiveService,
    ScrapService scrapService) : InstanceViewModelBase(bag, instanceManager, profileManager)
{
    private CompositeDisposable? _subscription;
    private IDisposable? _timerSubscription;


    protected override void OnUpdateModel(string key, Profile profile)
    {
        base.OnUpdateModel(key, profile);
        var screenshotPath = ProfileHelper.PickScreenshotRandomly(key);
        Screenshot = screenshotPath is not null ? new Bitmap(screenshotPath) : AssetUriIndex.WALLPAPER_IMAGE_BITMAP;
        PackageCount = profile.Setup.Packages.Count;
        UpdateTime(key);
    }

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
        // var start = InstanceManager.IsTracking(Basic.Key, out var tracker) && tracker is LaunchTracker launch
        //                 ? DateTimeOffset.Now - launch.StartedAt
        //                 : TimeSpan.Zero;
        // _timerSubscription = Observable
        //                     .Interval(TimeSpan.FromSeconds(1))
        //                     .Subscribe(x => TimerCount = start + TimeSpan.FromSeconds(x));
        if (InstanceManager.IsTracking(Basic.Key, out var tracker) && tracker is LaunchTracker launch)
        {
            var start = DateTimeOffset.Now - launch.StartedAt;
            _timerSubscription = Observable
                                .Interval(TimeSpan.FromSeconds(1))
                                .Subscribe(x => TimerCount = start + TimeSpan.FromSeconds(x));
        }
    }

    internal void ViewForTimerDestruct()
    {
        _timerSubscription?.Dispose();
    }

    protected override Task OnInitializedAsync(CancellationToken token)
    {
        base.OnInitializedAsync(token);

        var selector = persistenceService.GetAccountSelector(Basic.Key);
        if (selector != null)
        {
            var account = persistenceService.GetAccount(selector.Uuid);
            if (account != null)
            {
                var cooked = AccountHelper.ToCooked(account);
                SelectedAccount = new AccountModel(cooked.GetType(),
                                                   cooked.Uuid,
                                                   cooked.Username,
                                                   account.EnrolledAt,
                                                   account.LastUsedAt);
            }
        }

        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync(CancellationToken token)
    {
        _subscription?.Dispose();
        _timerSubscription?.Dispose();
        return base.OnDeinitializeAsync(token);
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

    protected override void OnInstanceLaunched(LaunchTracker tracker)
    {
        base.OnInstanceLaunched(tracker);

        UpdateTime(Basic.Key);
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task SwitchAccount()
    {
        var accounts = persistenceService
                      .GetAccounts()
                      .Select(x =>
                       {
                           var cooked = AccountHelper.ToCooked(x);
                           return SelectedAccount?.Uuid == cooked.Uuid
                                      ? SelectedAccount
                                      : new AccountModel(cooked.GetType(),
                                                         cooked.Uuid,
                                                         cooked.Username,
                                                         x.EnrolledAt,
                                                         x.LastUsedAt);
                       })
                      .ToList();
        var dialog = new AccountPickerDialog
        {
            GotoManagerViewCommand = OpenAccountsViewCommand, AccountsSource = accounts, Result = SelectedAccount
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
        var selector = persistenceService.GetAccountSelector(Basic.Key);
        if (selector != null)
        {
            var account = persistenceService.GetAccount(selector.Uuid);
            if (account != null)
            {
                var cooked = AccountHelper.ToCooked(account);

                if (cooked is MicrosoftAccount msa)
                    try
                    {
                        var profile =
                            await minecraftService.AcquireAccountProfileByMinecraftTokenAsync(msa.AccessToken);
                    }
                    catch (ApiException ex)
                    {
                        if (ex.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            var microsoft = await microsoftService.RefreshUserAsync(msa.RefreshToken);
                            var xbox =
                                await xboxLiveService
                                   .AuthenticateForXboxLiveTokenByMicrosoftTokenAsync(microsoft.AccessToken);
                            var xsts = await xboxLiveService.AuthorizeForServiceTokenByXboxLiveTokenAsync(xbox.Token);
                            var minecraft = await minecraftService.AuthenticateByXboxLiveServiceTokenAsync(xsts.Token,
                                                xsts.DisplayClaims.Xui.First().Uhs);

                            msa.AccessToken = minecraft.AccessToken;
                            msa.RefreshToken = microsoft.RefreshToken;
                            persistenceService.UpdateAccount(account.Uuid, AccountHelper.ToRaw(msa));
                        }
                        else
                        {
                            notificationService.PopMessage(ex, "Failed to authenticate account and rescue");
                            return;
                        }
                    }

                persistenceService.UseAccount(account.Uuid);
                try
                {
                    var profile = ProfileManager.GetImmutable(Basic.Key);
                    // Profile 的引用会被捕获，也就是在 Deploy 期间修改 OVERRIDE_JAVA_HOME 也会产生影响
                    var deploy =
                        new DeployOptions(profile.GetOverride(Profile.OVERRIDE_BEHAVIOR_DEPLOY_FASTMODE, false),
                                          profile.GetOverride(Profile.OVERRIDE_BEHAVIOR_RESOLVE_DEPENDENCY, false));
                    var launch = new LaunchOptions(javaHomeLocator: major =>
                                                       profile.GetOverride(Profile.OVERRIDE_JAVA_HOME,
                                                                           major switch
                                                                           {
                                                                               8 => configurationService.Value
                                                                                  .RuntimeJavaHome8,
                                                                               11 => configurationService.Value
                                                                                  .RuntimeJavaHome11,
                                                                               16 => configurationService.Value
                                                                                  .RuntimeJavaHome17,
                                                                               17 => configurationService.Value
                                                                                  .RuntimeJavaHome17,
                                                                               21 => configurationService.Value
                                                                                  .RuntimeJavaHome21,
                                                                               _ => throw new
                                                                                   ArgumentOutOfRangeException(nameof
                                                                                           (major),
                                                                                       major,
                                                                                       "Not supported java version")
                                                                           })
                                                    ?? throw new InvalidOperationException("Java home fallback unset"),
                                                   additionalArguments:
                                                   profile.GetOverride(Profile.OVERRIDE_JAVA_ADDITIONAL_ARGUMENTS,
                                                                       configurationService.Value
                                                                          .GameJavaAdditionalArguments),
                                                   maxMemory: profile.GetOverride(Profile.OVERRIDE_JAVA_MAX_MEMORY,
                                                       configurationService.Value.GameJavaMaxMemory),
                                                   windowSize:
                                                   (profile.GetOverride(Profile.OVERRIDE_WINDOW_WIDTH, configurationService.Value.GameWindowInitialWidth),
                                                    profile.GetOverride(Profile.OVERRIDE_WINDOW_HEIGHT,
                                                                        configurationService.Value
                                                                           .GameWindowInitialHeight)),
                                                   launchMode: Mode,
                                                   account: cooked,
                                                   brand: Program.Brand);
                    InstanceManager.DeployAndLaunch(Basic.Key, deploy, launch);

                    return;
                }
                catch (Exception ex)
                {
                    notificationService.PopMessage(ex, "Update failed");
                }
            }
        }

        notificationService.PopMessage("Account is not provided or removed after set",
                                       "Account not found",
                                       NotificationLevel.Danger,
                                       actions: [new NotificationAction("Select Account", SwitchAccountCommand)]);
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
            if (scrapService.TryGetBuffer(launch.Key, out var buffer))
                toast.SetItems(buffer);
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
    private void SwitchMode()
    {
        Mode = Mode switch
        {
            LaunchMode.Managed => LaunchMode.FireAndForget,
            LaunchMode.FireAndForget => LaunchMode.Debug,
            LaunchMode.Debug => LaunchMode.Managed,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

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

    #endregion
}
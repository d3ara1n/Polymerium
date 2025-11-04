using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Dialogs;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Properties;
using Polymerium.App.Services;
using Polymerium.App.Utilities;
using Polymerium.App.Views;
using Trident.Abstractions;
using Trident.Abstractions.Utilities;
using Trident.Core.Igniters;
using Trident.Core.Services;
using Trident.Core.Utilities;

namespace Polymerium.App.ViewModels;

public partial class LandingViewModel(
    NavigationService navigationService,
    DataService dataService,
    MojangLauncherService mojangLauncherService,
    PersistenceService persistenceService,
    ProfileManager profileManager,
    InstanceService instanceService,
    OverlayService overlayService,
    NotificationService notificationService) : ViewModelBase
{
    #region Reactive

    [ObservableProperty]
    public partial LazyObject? MinecraftNews { get; set; }

    [ObservableProperty]
    public partial double TotalPlayHours { get; set; }

    [ObservableProperty]
    public partial int ActiveDays { get; set; }

    [ObservableProperty]
    public partial int TotalSessions { get; set; }

    [ObservableProperty]
    public partial int InstanceCount { get; set; }

    [ObservableProperty]
    public partial int AccountCount { get; set; }

    [ObservableProperty]
    public partial RecentPlayModel? RecentPlay { get; set; }

    #endregion

    #region Overrides

    protected override Task OnInitializeAsync()
    {
        // This page is always the root page
        navigationService.ClearHistory();


        MinecraftNews = new(async _ =>
        {
            var news = await dataService.GetMinecraftReleasePatchesAsync();
            var models = news
                        .Entries.Take(24)
                        .Select((x, i) =>
                         {
                             var url = mojangLauncherService.GetAbsoluteImageUrl(x.Image.Url);

                             return new MinecraftReleasePatchModel(url, x.Type, x.Title, x.ShortText, x.Date);
                         })
                        .ToList();
            return models;
        });

        TotalPlayHours = persistenceService.GetTotalPlayTime().TotalHours;
        ActiveDays = persistenceService.GetActiveDays();
        TotalSessions = persistenceService.GetSessionCount();

        InstanceCount = profileManager.Profiles.Count();
        AccountCount = persistenceService.GetAccounts().Count();

        profileManager.ProfileAdded += OnProfileAdded;
        profileManager.ProfileRemoved += OnProfileRemoved;

        var last = persistenceService.GetLastActivity();
        if (last is not null && profileManager.TryGetImmutable(last.Key, out var profile))
        {
            var iconPath = ProfileHelper.PickIcon(last.Key);
            var icon = iconPath is not null ? new(iconPath) : AssetUriIndex.DirtImageBitmap;
            RecentPlay = new()
            {
                Key = last.Key,
                Name = profile.Name,
                Version = profile.Setup.Version,
                LoaderLabel =
                    profile.Setup.Loader is not null && LoaderHelper.TryParse(profile.Setup.Loader, out var loader)
                        ? LoaderHelper.ToDisplayName(loader.Identity)
                        : Resources.Enum_Vanilla,
                Thumbnail = icon,
                LastPlayedRaw = last.End
            };
            if (persistenceService.GetAccountSelector(last.Key) is { } selector
             && persistenceService.GetAccount(selector.Uuid) is { } account)
            {
                var cooked = AccountHelper.ToCooked(account);
                RecentPlay.Account = new(cooked.GetType(),
                                         cooked.Uuid,
                                         cooked.Username,
                                         account.EnrolledAt,
                                         account.LastUsedAt);
            }
        }

        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync()
    {
        profileManager.ProfileAdded -= OnProfileAdded;
        profileManager.ProfileRemoved -= OnProfileRemoved;


        return Task.CompletedTask;
    }

    #endregion

    #region Other

    private void OnProfileRemoved(object? sender, ProfileManager.ProfileChangedEventArgs e) => InstanceCount--;

    private void OnProfileAdded(object? sender, ProfileManager.ProfileChangedEventArgs e) => InstanceCount++;

    #endregion

    #region Commands

    [RelayCommand]
    private void OpenInstanceFolder(string? key)
    {
        if (key != null)
        {
            try
            {
                var dir = PathDef.Default.DirectoryOfHome(key);
                TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchDirectoryInfoAsync(new(dir));
            }
            catch (Exception ex)
            {
                notificationService.PopMessage(ex, "Failed to open folder");
            }
        }
    }

    [RelayCommand]
    private void ViewInstance(string? key)
    {
        if (key != null)
        {
            navigationService.Navigate<InstanceView>(key);
        }
    }

    [RelayCommand]
    private async Task PickAccountAsync()
    {
        if (RecentPlay is not null)
        {
            var accounts = persistenceService
                          .GetAccounts()
                          .Select(x =>
                           {
                               var cooked = AccountHelper.ToCooked(x);
                               return RecentPlay.Account?.Uuid == cooked.Uuid
                                          ? RecentPlay.Account
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
                Result = accounts.FirstOrDefault(x => x.Uuid == RecentPlay.Account?.Uuid)
            };
            if (await overlayService.PopDialogAsync(dialog) && dialog.Result is AccountModel account)
            {
                RecentPlay.Account = account;
                persistenceService.SetAccountSelector(RecentPlay.Key, account.Uuid);
            }
        }
    }

    [RelayCommand]
    private void OpenAccountsView() => navigationService.Navigate<AccountsView>();

    [RelayCommand]
    private async Task PlayAsync(string key)
    {
        try
        {
            await instanceService.DeployAndLaunchAsync(key, LaunchMode.Managed);
        }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex, "Failed to launch instance");
        }
    }

    #endregion
}

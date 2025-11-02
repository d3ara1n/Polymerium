using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Properties;
using Polymerium.App.Services;
using Polymerium.App.Utilities;
using Polymerium.App.Views;
using Trident.Abstractions;
using Trident.Abstractions.Utilities;
using Trident.Core.Services;
using Trident.Core.Utilities;

namespace Polymerium.App.ViewModels;

public partial class LandingViewModel(
    NavigationService navigationService,
    DataService dataService,
    MojangLauncherService mojangLauncherService,
    PersistenceService persistenceService,
    ProfileManager profileManager,
    InstanceManager instanceManager) : ViewModelBase
{
    public InstanceManager InstanceManager { get; } = instanceManager;

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
                LoaderLabel = profile.Setup.Loader is not null
                                  ? LoaderHelper.ToDisplayName(profile.Setup.Loader)
                                  : Resources.Enum_Vanilla,
                Thumbnail = icon,
                LastPlayedRaw = last.End
            };
            if (persistenceService.GetAccountSelector(last.Key) is { } selector
             && persistenceService.GetAccount(selector.Uuid) is { } account)
            {
                var cooked = AccountHelper.ToCooked(account);
                RecentPlay.Account = new()
                {
                    UserName = cooked.Username,
                    Uuid = cooked.Uuid,
                    TypeName = cooked.UserType,
                    FaceUrl = new($"https://starlightskins.lunareclipse.studio/render/pixel/{cooked.Uuid}/face",
                                  UriKind.Absolute)
                };
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
            var dir = PathDef.Default.DirectoryOfHome(key);
            TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchDirectoryInfoAsync(new(dir));
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

    #endregion
}

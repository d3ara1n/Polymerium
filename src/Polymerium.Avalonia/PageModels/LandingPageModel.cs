using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Polymerium.Avalonia.Assets;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Properties;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Toasts;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Igniters;
using TridentCore.Core.Services;
using TridentCore.Core.Utilities;

namespace Polymerium.Avalonia.PageModels;

public partial class LandingPageModel(
    NavigationService navigationService,
    DataService dataService,
    MojangService mojangServuce,
    PersistenceService persistenceService,
    ProfileManager profileManager,
    InstanceService instanceService,
    OverlayService overlayService,
    NotificationService notificationService,
    InstanceManager instanceManager
) : ViewModelBase
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

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        // This page is always the root page
        navigationService.ClearHistory();

        LoadMinecraftNews();

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
            var iconPath = InstanceHelper.PickIcon(last.Key);
            var icon = iconPath is not null ? new(iconPath) : AssetUriIndex.DirtImageBitmap;

            var screenshotPath = InstanceHelper.PickScreenshotRandomly(last.Key);
            Bitmap? screenshot = screenshotPath is not null ? new(screenshotPath) : null;

            RecentPlay = new()
            {
                Key = last.Key,
                Name = profile.Name,
                Version = profile.Setup.Version,
                LoaderLabel =
                    profile.Setup.Loader is not null
                    && LoaderHelper.TryParse(profile.Setup.Loader, out var loader)
                        ? LoaderHelper.ToDisplayName(loader.Identity)
                        : Resources.Enum_Vanilla,
                Thumbnail = icon,
                LastPlayedRaw = DateTimeHelper.FromPersistedLocalDateTime(last.End),
                LastPlayTimeRaw = last.End - last.Begin,
                PackageCount = profile.Setup.Packages.Count,
                SessionCount = persistenceService.GetSessionCount(last.Key),
                Screenshot = screenshot,
            };
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

    private void OnProfileRemoved(object? sender, ProfileManager.ProfileChangedEventArgs e) =>
        InstanceCount--;

    private void OnProfileAdded(object? sender, ProfileManager.ProfileChangedEventArgs e) =>
        InstanceCount++;

    private void LoadMinecraftNews() =>
        MinecraftNews = new(async _ =>
        {
            var news = await dataService.GetMinecraftReleasePatchesAsync();
            var models = news
                .Entries.Take(24)
                .Select(
                    (x, i) =>
                    {
                        var url = mojangServuce.GetAbsoluteImageUrl(x.Image.Url);

                        return new MinecraftReleasePatchModel(
                            url,
                            x.Type,
                            x.Title,
                            x.ShortText,
                            x.Date
                        );
                    }
                )
                .ToList();
            return models;
        });

    #endregion

    #region Commands

    [RelayCommand]
    private Task OpenInstanceFolder(string? key)
    {
        if (key != null)
        {
            var dir = PathDef.Default.DirectoryOfHome(key);
            return TopLevelHelper.LaunchDirectoryInfoAsync(
                TopLevelHelper.GetTopLevel(),
                new(dir),
                Resources.Shared_FailedToOpenInstanceFolderDangerNotificationTitle,
                notificationService,
                thumbnail: ThumbnailHelper.ForInstance(key)
            );
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void ViewInstance(string? key)
    {
        if (key != null)
        {
            navigationService.Navigate<InstancePage>(key);
        }
    }

    [RelayCommand]
    private void GotoMarketplace() => navigationService.Navigate<MarketplaceSearchPage>();

    [RelayCommand]
    private void TryOne()
    {
        var keys = profileManager.Profiles.Select(x => x.Item1).ToArray();
        if (keys.Length > 0)
        {
            var key = keys.ElementAt(Random.Shared.Next(keys.Length));
            navigationService.Navigate<InstancePage>(key);
        }
    }

    [RelayCommand]
    private void OpenAccountsPage() => navigationService.Navigate<AccountsPage>();

    [RelayCommand]
    private void RefreshMinecraftNews() => LoadMinecraftNews();

    [RelayCommand]
    private void Play(string key)
    {
        try
        {
            instanceService.DeployAndLaunch(key, LaunchMode.Managed);
        }
        catch (Exception ex)
        {
            notificationService.PopMessage(
                ex,
                Resources.Shared_FailedToLaunchInstanceDangerNotificationTitle,
                thumbnail: ThumbnailHelper.ForInstance(key)
            );
        }
    }

    [RelayCommand]
    private async Task ViewFeaturedModpackAsync(FeaturedModpackModel? modpack)
    {
        if (modpack is not null)
        {
            try
            {
                var project = await dataService.QueryProjectAsync(
                    modpack.Label,
                    modpack.Namespace,
                    modpack.ProjectId
                );
                var model = new ExhibitModpackModel(
                    project.Label,
                    project.Namespace,
                    project.ProjectId,
                    project.ProjectName,
                    project.Author,
                    project.Reference,
                    project.Thumbnail ?? modpack.Thumbnail,
                    project.Tags,
                    project.DownloadCount,
                    project.Summary,
                    project.UpdatedAt,
                    [.. project.Gallery.Select(x => x.Url)]
                );
                overlayService.PopToast(
                    new ExhibitModpackToast
                    {
                        DataService = dataService,
                        PersistenceService = persistenceService,
                        DataContext = model,
                        InstallCommand = InstallVersionCommand,
                    }
                );
            }
            catch (Exception ex)
            {
                notificationService.PopMessage(
                    ex,
                    Resources.LandingPage_LoadModpackDetailsDangerNotificationTitle,
                    thumbnail: modpack.Thumbnail
                );
            }
        }
    }

    [RelayCommand]
    private void InstallVersion(ExhibitVersionModel? version)
    {
        if (version is not null)
        {
            instanceManager.Install(
                version.ProjectName,
                version.Label,
                version.Namespace,
                version.ProjectId,
                version.VersionId
            );
            notificationService.PopMessage(
                Resources.MarketplaceSearchPage_ModpackInstallingNotificationMessage.Replace(
                    "{0}",
                    version.VersionName
                ),
                version.ProjectName
            );
        }
    }

    #endregion
}

using System;
using System.Linq;
using System.Threading;
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
using Polymerium.App.Toasts;
using Polymerium.App.Views;
using Trident.Abstractions;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;
using Trident.Core.Igniters;
using Trident.Core.Services;
using Trident.Core.Utilities;

namespace Polymerium.App.ViewModels;

public partial class LandingViewModel(
    NavigationService navigationService,
    DataService dataService,
    MojangService mojangServuce,
    PersistenceService persistenceService,
    ProfileManager profileManager,
    InstanceService instanceService,
    OverlayService overlayService,
    NotificationService notificationService,
    RepositoryAgent repositoryAgent,
    InstanceManager instanceManager) : ViewModelBase
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

    [ObservableProperty]
    public partial LazyObject? FeaturedModpacks { get; set; }

    #endregion

    #region Overrides

    protected override Task OnInitializeAsync()
    {
        // This page is always the root page
        navigationService.ClearHistory();

        LoadMinecraftNews();
        LoadFeaturedModpacks();

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
                LastPlayedRaw = last.End,
                LastPlayTimeRaw = last.End - last.Begin
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

    private void OnProfileRemoved(object? sender, ProfileManager.ProfileChangedEventArgs e) => InstanceCount--;

    private void OnProfileAdded(object? sender, ProfileManager.ProfileChangedEventArgs e) => InstanceCount++;

    private void LoadMinecraftNews() =>
        MinecraftNews = new(async _ =>
        {
            var news = await dataService.GetMinecraftReleasePatchesAsync();
            var models = news
                        .Entries.Take(24)
                        .Select((x, i) =>
                         {
                             var url = mojangServuce.GetAbsoluteImageUrl(x.Image.Url);

                             return new MinecraftReleasePatchModel(url, x.Type, x.Title, x.ShortText, x.Date);
                         })
                        .ToList();
            return models;
        });

    private void LoadFeaturedModpacks() =>
        FeaturedModpacks = new(async _ =>
        {
            var exhibits = await dataService.GetFeaturedModpacksAsync();
            var models = exhibits
                        .Select(x => new FeaturedModpackModel(x.Label,
                                                              x.Namespace,
                                                              x.Pid,
                                                              x.Name,
                                                              x.Author,
                                                              x.Thumbnail ?? AssetUriIndex.DirtImage,
                                                              x.Tags))
                        .ToList();
            return models;
        });

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
    private void GotoMarketplace() => navigationService.Navigate<MarketplacePortalView>();

    [RelayCommand]
    private void TryOne()
    {
        var keys = profileManager.Profiles.Select(x => x.Item1).ToArray();
        if (keys.Length > 0)
        {
            var key = keys.ElementAt(Random.Shared.Next(keys.Length));
            navigationService.Navigate<InstanceView>(key);
        }
    }

    [RelayCommand]
    private void OpenAccountsView() => navigationService.Navigate<AccountsView>();

    [RelayCommand]
    private void RefreshMinecraftNews() => LoadMinecraftNews();

    [RelayCommand]
    private void RefreshFeaturedModpacks() => LoadFeaturedModpacks();

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

    [RelayCommand]
    private async Task ViewFeaturedModpackAsync(FeaturedModpackModel? modpack)
    {
        if (modpack is not null)
        {
            try
            {
                var project = await dataService.QueryProjectAsync(modpack.Label, modpack.Namespace, modpack.ProjectId);
                var model = new ExhibitModpackModel(project.Label,
                                                    project.Namespace,
                                                    project.ProjectId,
                                                    project.ProjectName,
                                                    project.Author,
                                                    project.Reference,
                                                    project.Tags,
                                                    project.DownloadCount,
                                                    project.Summary,
                                                    project.UpdatedAt,
                                                    [.. project.Gallery.Select(x => x.Url)]);
                overlayService.PopToast(new ExhibitModpackToast
                {
                    DataService = dataService,
                    DataContext = model,
                    InstallCommand = InstallVersionCommand
                });
            }
            catch (Exception ex)
            {
                notificationService.PopMessage(ex, "Failed to load modpack details");
            }
        }
    }

    [RelayCommand]
    private void InstallVersion(ExhibitVersionModel? version)
    {
        if (version is not null)
        {
            instanceManager.Install(version.ProjectName,
                                    version.Label,
                                    version.Namespace,
                                    version.ProjectId,
                                    version.VersionId);
            notificationService.PopMessage(Resources.MarketplaceSearchView_ModpackInstallingNotificationMessage
                                                    .Replace("{0}", version.VersionName),
                                           version.ProjectName);
        }
    }

    #endregion
}

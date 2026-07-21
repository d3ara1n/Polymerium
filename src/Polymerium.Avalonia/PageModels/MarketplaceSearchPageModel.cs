using TridentCore.Pref;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Mvvm.Activation;
using Huskui.Avalonia.Mvvm.States;
using Polymerium.Avalonia.Assets;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Properties;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Toasts;
using Polymerium.Avalonia.Utilities;
using Refit;
using Semver;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Services;

namespace Polymerium.Avalonia.PageModels;

public partial class MarketplaceSearchPageModel : ViewModelBase, IStatefulViewModel<MarketplaceSearchPageModel.State>
{
    public MarketplaceSearchPageModel(
        IViewContext<SearchArguments> context,
        RepositoryAgent agent,
        InstanceManager instanceManager,
        NotificationService notificationService,
        OverlayService overlayService,
        DataService dataService,
        PersistenceService persistenceService)
    {
        _agent = agent;
        _instanceManager = instanceManager;
        _notificationService = notificationService;
        _overlayService = overlayService;
        _dataService = dataService;
        _persistenceService = persistenceService;

        var r = agent.Labels.Select(x => new RepositoryBasicModel(x, x.ToString().ToUpper())).ToList();
        Repositories = r;
        if (context.Parameter is { } arguments)
        {
            QueryText = arguments.Query ?? string.Empty;
            SelectedRepository = r.FirstOrDefault(x => x.Label == arguments.Label) ?? r.First();
        }
        else
        {
            SelectedRepository = r.First();
        }
    }

    #region Properties

    public IEnumerable<RepositoryBasicModel> Repositories { get; }

    #endregion

    #region Overrides

    protected override async Task OnInitializeAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }

        _ = SearchAsync();

        foreach (var repository in Repositories)
        {
            if (repository.Loaders == null || repository.Versions == null)
            {
                var status = await _dataService.CheckStatusAsync(repository.Label);
                repository.Loaders =
                [
                    .. status.SupportedLoaders.Select(x => new LoaderBasicModel(x, LoaderHelper.ToDisplayName(x))),
                ];
                repository.Versions =
                [
                    .. status.SupportedVersions.OrderByDescending(x => SemVersion.TryParse(x,
                                                                           SemVersionStyles.OptionalPatch,
                                                                           out var sem)
                                                                           ? sem
                                                                           : new(0, 0, 0),
                                                                  SemVersion.SortOrderComparer),
                ];
            }
        }
    }

    #endregion

    #region Nested type: SearchArguments

    public record SearchArguments(string? Query, string? Label);

    #endregion

    #region Injected

    private readonly RepositoryAgent _agent;
    private readonly InstanceManager _instanceManager;
    private readonly NotificationService _notificationService;
    private readonly OverlayService _overlayService;
    private readonly DataService _dataService;
    private readonly PersistenceService _persistenceService;

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial RepositoryBasicModel? SelectedRepository { get; set; }

    partial void OnSelectedRepositoryChanged(RepositoryBasicModel? value)
    {
        if (value is null)
        {
            return;
        }

        HeaderImage = value.Label switch
        {
            "curseforge" => AssetUriIndex.RepositoryHeaderCurseforgeBitmap,
            "modrinth" => AssetUriIndex.RepositoryHeaderModrinthBitmap,
            "favorite" => AssetUriIndex.RepositoryHeaderFavoriteBitmap,
            _ => HeaderImage,
        };

        _ = SearchAsync();
    }

    [ObservableProperty]
    public partial string? FilteredVersion { get; set; }

    partial void OnFilteredVersionChanged(string? value)
    {
        _ = SearchAsync();
    }

    [ObservableProperty]
    public partial LoaderBasicModel? FilteredLoader { get; set; }

    partial void OnFilteredLoaderChanged(LoaderBasicModel? value)
    {
        _ = SearchAsync();
    }

    [ObservableProperty]
    public partial InfiniteCollection<ExhibitModel>? Exhibits { get; set; }

    [ObservableProperty]
    public partial Bitmap? HeaderImage { get; set; }

    [ObservableProperty]
    public partial string QueryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial State? ViewState { get; set; }

    #endregion

    #region Commands

    [RelayCommand]
    private void ClearFilters()
    {
        FilteredLoader = null;
        FilteredVersion = null;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (SelectedRepository is null)
        {
            return;
        }

        if (Exhibits is { IsFetching: true })
        {
            return;
        }

        try
        {
            var handle = await _agent.SearchAsync(SelectedRepository.Label,
                                                  QueryText,
                                                  new(FilteredVersion, FilteredLoader?.LoaderId, ResourceKind.Modpack));
            var source = new InfiniteCollection<ExhibitModel>(async (i, token) =>
            {
                handle.PageIndex = (uint)(i < 0 ? 0 : i);
                try
                {
                    var rv = await handle.FetchAsync(token);
                    var tasks = rv
                               .Select(x => new ExhibitModel(x.Label,
                                                             x.Namespace,
                                                             x.Pid,
                                                             x.Name,
                                                             x.Summary,
                                                             x.Thumbnail ?? AssetUriIndex.DirtImage,
                                                             x.Author,
                                                             x.Tags,
                                                             x.UpdatedAt,
                                                             x.DownloadCount,
                                                             x.Reference)
                               {
                                   IsFavorite =
                                        _persistenceService.IsFavoriteProject(x.Label, x.Namespace, x.Pid),
                               })
                               .ToArray();
                    return tasks;
                }
                catch (ApiException ex)
                {
                    _notificationService.PopMessage(ex, Resources.Error_BadNetwork, GrowlLevel.Warning);
                    Debug.WriteLine(ex);
                }
                catch (HttpRequestException ex)
                {
                    _notificationService.PopMessage(ex, Resources.Error_BadNetwork, GrowlLevel.Warning);
                    Debug.WriteLine(ex);
                }

                return [];
            });
            Exhibits = source;
        }
        catch (ApiException ex)
        {
            _notificationService.PopMessage(ex, Resources.Error_BadNetwork, GrowlLevel.Warning);
            Debug.WriteLine(ex);
        }
        catch (HttpRequestException ex)
        {
            _notificationService.PopMessage(ex, Resources.Error_BadNetwork, GrowlLevel.Warning);
            Debug.WriteLine(ex);
        }
    }

    [RelayCommand]
    private void InstallLatest(ExhibitModel? exhibit)
    {
        if (exhibit is not null)
        {
            _instanceManager.Install(exhibit.ProjectName, exhibit.Label, exhibit.Namespace, exhibit.ProjectId, null);
            _notificationService.PopMessage(Resources.MarketplaceSearchPage_ModpackInstallingNotificationMessage
                                                     .Replace("{0}", exhibit.ProjectName),
                                            exhibit.ProjectName,
                                            thumbnail: exhibit.Thumbnail);
        }
    }

    [RelayCommand]
    private async Task FavoriteModpackAsync(ExhibitModel? exhibit)
    {
        if (SelectedRepository is null)
        {
            return;
        }

        if (exhibit is null)
        {
            return;
        }

        if (exhibit.IsFavorite)
        {
            _persistenceService.RemoveFavoriteProject(exhibit.Label, exhibit.Namespace, exhibit.ProjectId);
            exhibit.IsFavorite = false;
            if (SelectedRepository.Label == "favorite")
            {
                await SearchAsync();
            }

            return;
        }

        var project = await _dataService.QueryProjectAsync(new ProjectIdentifier(exhibit.Label, exhibit.Namespace, exhibit.ProjectId));
        _persistenceService.AddFavoriteProject(project);
        exhibit.IsFavorite = true;
    }

    [RelayCommand]
    private async Task ViewModpack(ExhibitModel? exhibit)
    {
        if (exhibit is not null)
        {
            try
            {
                var project = await _dataService.QueryProjectAsync(new ProjectIdentifier(exhibit.Label, exhibit.Namespace, exhibit.ProjectId));
                var model = new ExhibitModpackModel(project.Label,
                                                    project.Namespace,
                                                    project.ProjectId,
                                                    project.ProjectName,
                                                    project.Author,
                                                    project.Reference,
                                                    project.Thumbnail ?? exhibit.Thumbnail,
                                                    project.Tags,
                                                    project.DownloadCount,
                                                    project.Summary,
                                                    project.UpdatedAt,
                                                    [.. project.Gallery.Select(x => x.Url)]);
                _overlayService.PopToast(new ExhibitModpackToast
                {
                    DataService = _dataService,
                    PersistenceService = _persistenceService,
                    DataContext = model,
                    InstallCommand = InstallVersionCommand,
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _notificationService.PopMessage(ex,
                                                Resources.MarketplaceSearchPage_ModpackLoadingDangerNotificationTitle,
                                                GrowlLevel.Warning);
            }
        }
    }

    [RelayCommand]
    private Task OpenWebsite(ExhibitModel? exhibit)
    {
        if (exhibit is not null)
        {
            return TopLevelHelper.LaunchUriAsync(TopLevelHelper.GetTopLevel(),
                                                 exhibit.Reference,
                                                 Resources
                                                    .MarketplaceSearchPage_OpenProjectWebsiteDangerNotificationTitle,
                                                 _notificationService,
                                                 thumbnail: exhibit.Thumbnail);
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void InstallVersion(ExhibitVersionModel? version)
    {
        if (version is not null)
        {
            _instanceManager.Install(version.ProjectName,
                                     version.Label,
                                     version.Namespace,
                                     version.ProjectId,
                                     version.VersionId);
            _notificationService.PopMessage(Resources.MarketplaceSearchPage_ModpackInstallingNotificationMessage
                                                     .Replace("{0}", version.VersionName),
                                            version.ProjectName);
        }
    }

    #endregion

    #region Nested type: State

    public partial class State : ModelBase
    {
        [ObservableProperty]
        public partial int LayoutIndex { get; set; }
    }

    #endregion
}

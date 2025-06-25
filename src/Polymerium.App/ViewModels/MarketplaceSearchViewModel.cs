using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Properties;
using Polymerium.App.Services;
using Polymerium.App.Toasts;
using Polymerium.Trident.Services;
using Refit;
using Semver;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.ViewModels;

public partial class MarketplaceSearchViewModel : ViewModelBase
{
    public MarketplaceSearchViewModel(
        ViewBag bag,
        RepositoryAgent agent,
        InstanceManager instanceManager,
        NotificationService notificationService,
        OverlayService overlayService,
        DataService dataService,
        ConfigurationService configurationService)
    {
        _agent = agent;
        _instanceManager = instanceManager;
        _notificationService = notificationService;
        _overlayService = overlayService;
        _dataService = dataService;
        _configurationService = configurationService;

        LayoutIndex = configurationService.Value.InterfaceMarketplaceLayout;

        var r = agent.Labels.Select(x => new RepositoryBasicModel(x, x.ToString().ToUpper())).ToList();
        Repositories = r;
        if (bag.Parameter is SearchArguments arguments)
        {
            QueryText = arguments.Query ?? string.Empty;
            SelectedRepository = r.FirstOrDefault(x => x.Label == arguments.Label) ?? r.First();
        }
        else
        {
            SelectedRepository = r.First();
        }
    }

    public IEnumerable<RepositoryBasicModel> Repositories { get; }

    protected override async Task OnInitializedAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;

        _ = SearchAsync();

        foreach (var repository in Repositories)
            if (repository.Loaders == null || repository.Versions == null)
            {
                var status = await _dataService.CheckStatusAsync(repository.Label);
                repository.Loaders = status
                                    .SupportedLoaders.Select(x => new LoaderBasicModel(x,
                                                                 x switch
                                                                 {
                                                                     LoaderHelper.LOADERID_FORGE => "Forge",
                                                                     LoaderHelper.LOADERID_NEOFORGE => "NeoForge",
                                                                     LoaderHelper.LOADERID_FABRIC => "Fabric",
                                                                     LoaderHelper.LOADERID_QUILT => "QUILT",
                                                                     LoaderHelper.LOADERID_FLINT => "Flint Loader",
                                                                     _ => x
                                                                 }))
                                    .ToList();
                repository.Versions = status
                                     .SupportedVersions
                                     .OrderByDescending(x => SemVersion.TryParse(x,
                                                                 SemVersionStyles.OptionalPatch,
                                                                 out var sem)
                                                                 ? sem
                                                                 : new SemVersion(0, 0, 0),
                                                        SemVersion.SortOrderComparer)
                                     .ToList();
            }
    }

    #region Nested type: SearchArguments

    public record SearchArguments(string? Query, string? Label);

    #endregion

    #region Injected

    private readonly RepositoryAgent _agent;
    private readonly InstanceManager _instanceManager;
    private readonly NotificationService _notificationService;
    private readonly OverlayService _overlayService;
    private readonly DataService _dataService;
    private readonly ConfigurationService _configurationService;

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial int LayoutIndex { get; set; }

    partial void OnLayoutIndexChanged(int value)
    {
        _configurationService.Value.InterfaceMarketplaceLayout = value;
    }

    [ObservableProperty]
    public partial RepositoryBasicModel SelectedRepository { get; set; }

    partial void OnSelectedRepositoryChanged(RepositoryBasicModel value)
    {
        var cur = SelectedRepository;
        HeaderImage = cur.Label switch
        {
            "curseforge" => AssetUriIndex.REPOSITORY_HEADER_CURSEFORGE_BITMAP,
            "modrinth" => AssetUriIndex.REPOSITORY_HEADER_MODRINTH_BITMAP,
            "favorite" => AssetUriIndex.REPOSITORY_HEADER_FAVORITE_BITMAP,
            _ => HeaderImage
        };
    }

    [ObservableProperty]
    public partial string? FilteredVersion { get; set; }

    [ObservableProperty]
    public partial LoaderBasicModel? FilteredLoader { get; set; }

    [ObservableProperty]
    public partial InfiniteCollection<ExhibitModel>? Exhibits { get; set; }

    [ObservableProperty]
    public partial Bitmap? HeaderImage { get; set; }

    [ObservableProperty]
    public partial string QueryText { get; set; } = string.Empty;

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
        try
        {
            var handle = await _agent.SearchAsync(SelectedRepository.Label,
                                                  QueryText,
                                                  new Filter(FilteredVersion,
                                                             FilteredLoader?.LoaderId,
                                                             ResourceKind.Modpack));
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
                                                             x.Thumbnail ?? AssetUriIndex.DIRT_IMAGE,
                                                             x.Author,
                                                             x.Tags,
                                                             x.UpdatedAt,
                                                             x.DownloadCount,
                                                             x.Reference))
                               .ToArray();
                    return tasks;
                }
                catch (ApiException ex)
                {
                    _notificationService.PopMessage(ex, "Network unreachable", NotificationLevel.Warning);
                    Debug.WriteLine(ex);
                }

                return [];
            });
            Exhibits = source;
        }
        catch (ApiException ex)
        {
            _notificationService.PopMessage(ex, "Network unreachable", NotificationLevel.Warning);
            Debug.WriteLine(ex);
        }
    }

    [RelayCommand]
    private void InstallLatest(ExhibitModel? exhibit)
    {
        if (exhibit is not null)
        {
            _instanceManager.Install(exhibit.ProjectName, exhibit.Label, exhibit.Ns, exhibit.ProjectId, null);
            _notificationService.PopMessage(Resources.MarketplaceSearchView_ModpackInstallingNotificationPrompt
                                                     .Replace("{0}", exhibit.ProjectName),
                                            exhibit.ProjectName);
        }
    }

    [RelayCommand]
    private async Task ViewModpack(ExhibitModel? exhibit)
    {
        if (exhibit is not null)
            try
            {
                var project = await _dataService.QueryProjectAsync(exhibit.Label, exhibit.Ns, exhibit.ProjectId);
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
                                                    project.Gallery.Select(x => x.Url).ToList());
                _overlayService.PopToast(new ExhibitModpackToast
                {
                    DataService = _dataService, DataContext = model, InstallCommand = InstallVersionCommand
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _notificationService.PopMessage(ex,
                                                Resources.MarketplaceSearchView_ModpackLoadingDangerNotificationTitle,
                                                NotificationLevel.Warning);
            }
    }

    [RelayCommand]
    private void OpenWebsite(ExhibitModel? exhibit)
    {
        if (exhibit is not null)
            TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchUriAsync(exhibit.Reference);
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
            _notificationService.PopMessage(Resources.MarketplaceSearchView_ModpackInstallingNotificationPrompt
                                                     .Replace("{0}", version.VersionName),
                                            version.ProjectName);
        }
    }

    #endregion
}
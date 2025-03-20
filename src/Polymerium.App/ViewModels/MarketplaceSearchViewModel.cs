using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
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
    [ObservableProperty]
    public partial string QueryText { get; set; } = string.Empty;

    public MarketplaceSearchViewModel(
        ViewBag bag,
        RepositoryAgent agent,
        InstanceManager instanceManager,
        NotificationService notificationService,
        OverlayService overlayService,
        DataService dataService)
    {
        _agent = agent;
        _instanceManager = instanceManager;
        _notificationService = notificationService;
        _overlayService = overlayService;
        _dataService = dataService;
        // TODO: 名字应该在本地化键值对中获取
        var r = agent.Labels.Select(x => new RepositoryBasicModel(x, x.ToString().ToUpper())).ToList();
        Repositories = r;
        if (bag.Parameter is SearchArguments arguments)
        {
            SelectedRepository = r.FirstOrDefault(x => x.Label == arguments.Label) ?? r.First();
            QueryText = arguments.Query ?? string.Empty;
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
            if (repository.Loaders.Count == 0 || repository.Versions.Count == 0)
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

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(SelectedRepository))
        {
            var cur = SelectedRepository;
            HeaderImage = cur.Label switch
            {
                "curseforge" => new Bitmap(AssetLoader.Open(new Uri(AssetUriIndex.REPOSITORY_HEADER_CURSEFORGE))),
                "modrinth" => new Bitmap(AssetLoader.Open(new Uri(AssetUriIndex.REPOSITORY_HEADER_MODRINTH))),
                _ => HeaderImage
            };
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

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial RepositoryBasicModel SelectedRepository { get; set; }

    [ObservableProperty]
    public partial string? FilteredVersion { get; set; }

    [ObservableProperty]
    public partial LoaderBasicModel? FilteredLoader { get; set; }

    [ObservableProperty]
    public partial InfiniteCollection<ExhibitModel>? Exhibits { get; set; }

    [ObservableProperty]
    public partial Bitmap? HeaderImage { get; set; }

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
            var source = new InfiniteCollection<ExhibitModel>(async i =>
            {
                handle.PageIndex = (uint)(i < 0 ? 0 : i);
                try
                {
                    var rv = await handle.FetchAsync();
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
            _notificationService.PopMessage("Network unreachable", level: NotificationLevel.Warning);
            Debug.WriteLine(ex);
        }
    }

    [RelayCommand]
    private void InstallLatest(ExhibitModel? exhibit)
    {
        if (exhibit is not null)
        {
            _instanceManager.Install(exhibit.ProjectName, exhibit.Label, exhibit.Ns, exhibit.ProjectId, null);
            _notificationService.PopMessage($"{exhibit.ProjectName} has added to install queue");
        }
    }

    [RelayCommand]
    private async Task ViewDetails(ExhibitModel? exhibit)
    {
        if (exhibit is not null)
            try
            {
                var versions = await _dataService.InspectVersionsAsync(exhibit.Label,
                                                                       exhibit.Ns,
                                                                       exhibit.ProjectId,
                                                                       Filter.Empty with
                                                                       {
                                                                           Kind = ResourceKind.Modpack
                                                                       });
                var project = await _dataService.QueryProjectAsync(exhibit.Label, exhibit.Ns, exhibit.ProjectId);
                var model = new ExhibitModpackModel(project.ProjectName,
                                                    project.ProjectId,
                                                    project.Author,
                                                    project.Label,
                                                    project.Reference,
                                                    project.Tags,
                                                    project.DownloadCount,
                                                    project.Summary,
                                                    project.Description,
                                                    project.UpdatedAt,
                                                    project.Gallery.Select(x => x.Url).ToList(),
                                                    versions
                                                       .Select(x => new ExhibitVersionModel(project.Label,
                                                                   project.Namespace,
                                                                   project.ProjectName,
                                                                   project.ProjectId,
                                                                   x.VersionName,
                                                                   x.VersionId,
                                                                   x.Changelog,
                                                                   x.PublishedAt,
                                                                   x.DownloadCount,
                                                                   x.ReleaseType,
                                                                   PackageHelper.ToPurl(x.Label,
                                                                       x.Namespace,
                                                                       x.ProjectId,
                                                                       x.VersionId)))
                                                       .ToList());
                _overlayService.PopToast(new ExhibitModpackToast
                {
                    DataContext = model, InstallCommand = InstallVersionCommand
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _notificationService.PopMessage(ex, "Failed to load project information", NotificationLevel.Warning);
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
                                     version.Versionid);
            _notificationService.PopMessage($"{version.ProjectName}({version.VersionName}) has added to install queue");
        }
    }

    #endregion
}
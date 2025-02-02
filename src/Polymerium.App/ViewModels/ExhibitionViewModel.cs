using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.Trident.Services;
using Refit;
using Semver;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.ViewModels;

public partial class ExhibitionViewModel : ViewModelBase
{
    private readonly RepositoryAgent _agent;
    private readonly IHttpClientFactory _factory;
    private readonly InstanceManager _instanceManager;

    public ExhibitionViewModel(RepositoryAgent agent, IHttpClientFactory factory, InstanceManager instanceManager)
    {
        _agent = agent;
        _factory = factory;
        _instanceManager = instanceManager;
        // TODO: 名字应该在本地化键值对中获取
        var r = agent.Labels.Select(x => new RepositoryBasicModel(x, x switch
        {
            CurseForgeService.LABEL => "CurseForge",
            _ => x
        })).ToList();
        Repositories = r;

        SelectedRepository = r.First();
    }

    public IEnumerable<RepositoryBasicModel> Repositories { get; }

    protected override async Task OnInitializedAsync(Dispatcher dispatcher, CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        foreach (var repository in Repositories)
            if (repository.Loaders.Count == 0 || repository.Versions.Count == 0)
            {
                var status = await _agent.CheckStatusAsync(repository.Label);
                repository.Loaders = status.SupportedLoaders.Select(x => new LoaderDisplayModel(x, x switch
                {
                    LoaderHelper.LOADERID_FORGE => "Forge",
                    LoaderHelper.LOADERID_NEOFORGE => "NeoForge",
                    LoaderHelper.LOADERID_FABRIC => "Fabric",
                    LoaderHelper.LOADERID_QUILT => "QUILT",
                    LoaderHelper.LOADERID_FLINT => "Flint Loader",
                    _ => x
                })).ToList();
                repository.Versions = status.SupportedVersions
                    .OrderByDescending(
                        x => SemVersion.TryParse(x, SemVersionStyles.OptionalPatch, out var sem)
                            ? sem
                            : new SemVersion(0, 0, 0),
                        SemVersion.SortOrderComparer).ToList();
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

    #region Reactive Properties

    [ObservableProperty] private RepositoryBasicModel _selectedRepository;
    [ObservableProperty] private string? _filteredVersion;
    [ObservableProperty] private LoaderDisplayModel? _filteredLoader;
    [ObservableProperty] private InfiniteCollection<ExhibitModel>? _exhibits;
    [ObservableProperty] private Bitmap? _headerImage;

    #endregion

    #region Commands

    [RelayCommand]
    private void ClearFilters()
    {
        FilteredLoader = null;
        FilteredVersion = null;
    }

    [RelayCommand]
    private async Task SearchAsync(string query)
    {
        try
        {
            var handle = await _agent.SearchAsync(SelectedRepository.Label, query,
                new Filter(FilteredVersion, FilteredLoader?.LoaderId, ResourceKind.Modpack));
            var source = new InfiniteCollection<ExhibitModel>(async i =>
            {
                handle.PageIndex = (uint)(i < 0 ? 0 : i);
                try
                {
                    var rv = await handle.FetchAsync();
                    var tasks = rv.Select(async x =>
                    {
                        Bitmap? thumbnail = null;
                        if (!Debugger.IsAttached && x.Thumbnail is { IsAbsoluteUri: true })
                        {
                            using var client = _factory.CreateClient();
                            var data = await client.GetByteArrayAsync(x.Thumbnail.AbsoluteUri);
                            thumbnail = new Bitmap(new MemoryStream(data));
                        }
                        else
                        {
                            thumbnail = new Bitmap(AssetLoader.Open(new Uri(AssetUriIndex.DIRT_IMAGE)));
                        }

                        return new ExhibitModel(x.Label, x.Namespace, x.Pid, x.Name, x.Summary,
                            thumbnail, x.Author, x.Tags, x.UpdatedAt, x.DownloadCount, x.Reference);
                    }).ToArray();
                    await Task.WhenAll(tasks);
                    return tasks.Select(x => x.Result);
                }
                catch (ApiException ex)
                {
                    // TODO: pop notification
                    Debug.WriteLine(ex);
                }

                return [];
            });
            Exhibits = source;
        }
        catch (ApiException ex)
        {
            // TODO: pop notification
            Debug.WriteLine(ex);
        }
    }

    [RelayCommand]
    private void Install(ExhibitModel exhibit)
    {
        _instanceManager.Install(exhibit.Name, exhibit.Label, exhibit.Ns, exhibit.Pid, null);
    }

    #endregion
}
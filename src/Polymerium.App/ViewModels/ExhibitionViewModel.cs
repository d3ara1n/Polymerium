using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
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
    public IEnumerable<RepositoryBaiscModel> Repositories { get; }

    #region Reactive Properties

    [ObservableProperty] private RepositoryBaiscModel _selectedRepository;
    [ObservableProperty] private string? _filteredVersion;
    [ObservableProperty] private LoaderDisplayModel? _filteredLoader;
    [ObservableProperty] private InfiniteCollection<ExhibitModel>? _exhibits;

    #endregion

    #region Commands

    [RelayCommand]
    private void ClearFilters()
    {
        FilteredLoader = null;
        FilteredVersion = null;
    }

    [RelayCommand]
    private async Task Search(string query)
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
                        if (x.Thumbnail is { IsAbsoluteUri: true })
                        {
                            using var client = _factory.CreateClient();
                            var data = await client.GetByteArrayAsync(x.Thumbnail.AbsoluteUri);
                            thumbnail = new Bitmap(new MemoryStream(data));
                        }
                        else
                        {
                            Debug.WriteLine(x.Thumbnail);
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

                return Enumerable.Empty<ExhibitModel>();
            });
            Exhibits = source;
        }
        catch (ApiException ex)
        {
            // TODO: pop notification
            Debug.WriteLine(ex);
        }
    }

    #endregion

    public ExhibitionViewModel(RepositoryAgent agent, IHttpClientFactory factory)
    {
        _agent = agent;
        _factory = factory;
        // TODO: 名字应该在本地化键值对中获取
        var r = agent.Labels.Select(x => new RepositoryBaiscModel(x, x switch
        {
            CurseForgeService.LABEL => "CurseForge",
            _ => x
        })).ToList();
        Repositories = r;

        _selectedRepository = r.First();
    }

    protected override async Task OnInitializedAsync(Dispatcher dispatcher, CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        foreach (var repository in Repositories)
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
}
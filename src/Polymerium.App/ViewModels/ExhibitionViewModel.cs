using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.Trident.Services;
using Semver;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.ViewModels;

public partial class ExhibitionViewModel : ViewModelBase
{
    private readonly RepositoryAgent _agent;
    public IEnumerable<RepositoryBaiscModel> Repositories { get; }

    #region Reactive Properties

    [ObservableProperty] private RepositoryBaiscModel _selectedRepository;
    [ObservableProperty] private string? _filteredVersion;
    [ObservableProperty] private LoaderDisplayModel? _filteredLoader;

    #endregion

    #region Commands

    [RelayCommand]
    private void ClearFilters()
    {
        FilteredLoader = null;
        FilteredVersion = null;
    }

    #endregion

    public ExhibitionViewModel(RepositoryAgent agent)
    {
        _agent = agent;
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
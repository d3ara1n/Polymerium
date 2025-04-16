using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Exceptions;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using Refit;
using Semver;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.ViewModels;

public partial class PackageExplorerViewModel : ViewModelBase
{
    public PackageExplorerViewModel(
        ViewBag bag,
        RepositoryAgent agent,
        DataService dataService,
        ProfileManager profileManager,
        NotificationService notificationService)
    {
        _agent = agent;
        _dataService = dataService;
        _profileManager = profileManager;
        _notificationService = notificationService;

        if (bag.Parameter is string key)
        {
            if (profileManager.TryGetImmutable(key, out var profile))
            {
                Basic = new InstanceBasicModel(key,
                                               profile.Name,
                                               profile.Setup.Version,
                                               profile.Setup.Loader,
                                               profile.Setup.Source);
            }
            else
            {
                throw new PageNotReachedException(typeof(InstanceView),
                                                  $"Key '{key}' is not valid instance or not found");
            }
        }
        else
        {
            throw new PageNotReachedException(typeof(InstanceView), "Key to the instance is not provided");
        }


        var r = agent.Labels.Select(x => new RepositoryBasicModel(x, x.ToString().ToUpper())).ToList();
        Repositories = r;
        SelectedRepository = r.First();
        IsFilterEnabled = true;
    }

    protected override async Task OnInitializedAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;

        foreach (var repository in Repositories)
            if (repository.Loaders == null || repository.Versions == null)
            {
                var status = await _dataService.CheckStatusAsync(repository.Label);
                repository.Kinds = status.SupportedKinds.Where(x => x != ResourceKind.Modpack).ToList();
            }
    }

    public IEnumerable<RepositoryBasicModel> Repositories { get; }

    #region Reactive

    [ObservableProperty]
    public partial InstanceBasicModel Basic { get; set; }

    [ObservableProperty]
    public partial RepositoryBasicModel SelectedRepository { get; set; }

    [ObservableProperty]
    public partial Filter Filter { get; set; } = Filter.Empty;

    partial void OnFilterChanged(Filter value)
    {
        _ = SearchAsync();
    }

    [ObservableProperty]
    public partial string QueryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsFilterEnabled { get; set; }

    partial void OnIsFilterEnabledChanged(bool value)
    {
        if (value)
        {
            Filter = Filter with { Loader = null, Version = null };
        }
        else
        {
            Filter = Filter with { Loader = Basic.Loader, Version = Basic.Version };
        }
    }

    [ObservableProperty]
    public partial ResourceKind? SelectedKind { get; set; }

    partial void OnSelectedKindChanged(ResourceKind? value)
    {
        if (value != null)
        {
            Filter = Filter with { Kind = value };
        }
    }

    [ObservableProperty]
    public partial InfiniteCollection<ExhibitModel>? Exhibits { get; set; }

    #endregion

    #region Injected

    private readonly RepositoryAgent _agent;
    private readonly DataService _dataService;
    private readonly ProfileManager _profileManager;
    private readonly NotificationService _notificationService;

    #endregion

    #region Commands

    [RelayCommand]
    private async Task SearchAsync()
    {
        try
        {
            var handle = await _agent.SearchAsync(SelectedRepository.Label, QueryText, Filter);
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

    #endregion
}
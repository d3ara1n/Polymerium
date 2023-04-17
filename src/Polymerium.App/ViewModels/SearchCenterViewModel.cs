using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.Abstractions.Resources;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Core.Components;
using Polymerium.Core.Resources;

namespace Polymerium.App.ViewModels;

public class SearchCenterViewModel : ObservableObject
{
    private readonly IOverlayService _overlayService;

    private GameInstanceModel? instanceScope;
    private IResourceRepository? selectedRepository;

    private ResourceType? selectedResourceType;

    public SearchCenterViewModel(
        ViewModelContext context,
        IEnumerable<IResourceRepository> repositories,
        IOverlayService overlayService
    )
    {
        Instance = context.AssociatedInstance!;
        _overlayService = overlayService;
        Repositories = repositories;
        SupportedResources = new ObservableCollection<ResourceType>();
        ClearScopeCommand = new RelayCommand(ClearScope);
    }

    public GameInstanceModel Instance { get; set; }

    public IEnumerable<IResourceRepository> Repositories { get; }
    public ICommand ClearScopeCommand { get; }
    public ObservableCollection<ResourceType> SupportedResources { get; set; }

    public GameInstanceModel? InstanceScope
    {
        get => instanceScope;
        set => SetProperty(ref instanceScope, value);
    }

    public ResourceType? SelectedResourceType
    {
        get => selectedResourceType;
        set => SetProperty(ref selectedResourceType, value);
    }

    public IResourceRepository? SelectedRepository
    {
        get => selectedRepository;
        set
        {
            if (SetProperty(ref selectedRepository, value))
                UpdateSupportedResources();
        }
    }

    public void UpdateSupportedResources()
    {
        (var old, SelectedResourceType) = (SelectedResourceType, null);
        SelectedResourceType = null;
        SupportedResources.Clear();
        var supported = SelectedRepository!.SupportedResources;
        foreach (uint i in Enum.GetValues(typeof(ResourceType)))
        {
            var type = (ResourceType)i;
            if (
                type != ResourceType.None
                && (supported & type) == type
                && (InstanceScope == null || type != ResourceType.Modpack)
            )
                SupportedResources.Add(type);
        }

        SelectedResourceType = SupportedResources.Any(x => x == old)
            ? old
            : SupportedResources.First();
    }

    public async Task<IEnumerable<SearchCenterResultItemModel>> QueryAsync(
        string query,
        ResourceType type,
        uint offset = 0,
        uint limit = 10,
        CancellationToken token = default
    )
    {
        var repository = SelectedRepository;
        if (repository == null)
            return Enumerable.Empty<SearchCenterResultItemModel>();
        string? version = null;
        string? modLoader = null;
        if (InstanceScope?.Components.Any(x => ComponentMeta.MINECRAFT != x.Identity) == true)
            modLoader = InstanceScope.Components
                .First(x => ComponentMeta.MINECRAFT != x.Identity)
                .Identity;
        if (InstanceScope?.Components.Any(x => ComponentMeta.MINECRAFT == x.Identity) == true)
            version = InstanceScope.Components
                .First(x => ComponentMeta.MINECRAFT == x.Identity)
                .Version;
        var results = await repository.SearchProjectsAsync(
            query,
            type,
            modLoader,
            version,
            offset,
            limit,
            token
        );
        return results.Select(
            x =>
                new SearchCenterResultItemModel(
                    x.Name,
                    x.IconSource,
                    x.Author,
                    x.Downloads,
                    x.Summary,
                    type,
                    x
                )
        );
    }

    public void ShowDetailDialog(SearchCenterResultItemModel model)
    {
        var dialog = new SearchDetailDialog(model.Resource, InstanceScope)
        {
            OverlayService = _overlayService
        };
        _overlayService.Show(dialog);
    }

    private void ClearScope()
    {
        InstanceScope = null;
        UpdateSupportedResources();
    }
}

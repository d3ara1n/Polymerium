using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Abstractions.Resources;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Core.Resources;

namespace Polymerium.App.ViewModels;

public class SearchCenterViewModel : ObservableObject
{
    private readonly IOverlayService _overlayService;
    private IResourceRepository? selectedRepository;

    private ResourceType? selectedResourceType;

    public SearchCenterViewModel(IEnumerable<IResourceRepository> repositories, IOverlayService overlayService)
    {
        _overlayService = overlayService;
        Repositories = repositories;
        SupportedResources = new ObservableCollection<ResourceType>();
        // HACK: SelectedIndex = 0 不会反向引用
        SelectedRepository = Repositories.First();
    }

    public IEnumerable<IResourceRepository> Repositories { get; }

    public ObservableCollection<ResourceType> SupportedResources { get; set; }

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
            {
                (var old, SelectedResourceType) = (SelectedResourceType, null);
                SelectedResourceType = null;
                SupportedResources.Clear();
                var supported = value!.SupportedResources;
                foreach (uint i in Enum.GetValues(typeof(ResourceType)))
                {
                    var type = (ResourceType)i;
                    if ((supported & type) == type) SupportedResources.Add(type);
                }

                SelectedResourceType = SupportedResources.Any(x => x == old) ? old : SupportedResources.First();
            }
        }
    }

    public async Task<IEnumerable<SearchCenterResultItemModel>> QueryAsync(string query, ResourceType type,
        uint offset = 0, uint limit = 10, CancellationToken token = default)
    {
        var repository = SelectedRepository;
        if (repository == null) return Enumerable.Empty<SearchCenterResultItemModel>();
        var results = type switch
        {
            ResourceType.Modpack => await repository.SearchModpacksAsync(query, null, offset, limit, token),
            _ => throw new NotImplementedException()
        };
        return results.Select(x =>
            new SearchCenterResultItemModel(x.Name, x.IconSource, x.Author, x.Summary, ResourceType.Modpack,
                x));
    }

    public void ShowDetailDialog(SearchCenterResultItemModel model)
    {
        var dialog = new SearchDetailDialog(model.Resource)
        {
            OverlayService = _overlayService
        };
        _overlayService.Show(dialog);
    }
}
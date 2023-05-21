using Polymerium.Abstractions.Resources;

namespace Polymerium.App.Models;

public class SearchCenterNavigationArguments
{
    public SearchCenterNavigationArguments(
        string query,
        bool instanceScopeOverride = false,
        RepositoryLabel? repository = null,
        ResourceType? type = null,
        bool searchImmediately = false
    )
    {
        Query = query;
        InstanceScopeOverride = instanceScopeOverride;
        Repository = repository;
        Type = type;
        SearchImmediately = searchImmediately;
    }

    public string Query { get; set; }
    public bool InstanceScopeOverride { get; set; }
    public RepositoryLabel? Repository { get; set; }
    public ResourceType? Type { get; set; }
    public bool SearchImmediately { get; set; }
}
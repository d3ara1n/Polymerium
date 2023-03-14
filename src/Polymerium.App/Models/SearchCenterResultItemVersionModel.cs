using System;
using Polymerium.Core.Resources;

namespace Polymerium.App.Models;

public class SearchCenterResultItemVersionModel
{
    public SearchCenterResultItemVersionModel(string id, string display, RepositoryAssetFile file, Uri resourceUrl)
    {
        Id = id;
        Display = display;
        File = file;
        ResourceUrl = resourceUrl;
    }

    public string Id { get; set; }
    public string Display { get; set; }
    public RepositoryAssetFile File { get; set; }
    public Uri ResourceUrl { get; set; }
}
using System;
using Polymerium.Core.Resources;

namespace Polymerium.App.Models;

public class SearchCenterResultItemVersionModel
{
    public SearchCenterResultItemVersionModel(string id, string display, DateTimeOffset releaseDateTime,
        RepositoryAssetFile file, Uri resourceUrl)
    {
        Id = id;
        Display = display;
        File = file;
        ReleaseDateTime = releaseDateTime;
        ResourceUrl = resourceUrl;
    }

    public string Id { get; set; }
    public string Display { get; set; }
    public DateTimeOffset ReleaseDateTime { get; set; }
    public RepositoryAssetFile File { get; set; }
    public Uri ResourceUrl { get; set; }
}
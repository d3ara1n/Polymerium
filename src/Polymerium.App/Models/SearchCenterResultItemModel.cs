using System;
using System.Collections.Generic;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Resources;

namespace Polymerium.App.Models;

public class SearchCenterResultItemModel
{
    public SearchCenterResultItemModel(
        string caption,
        Uri? iconSource,
        string author,
        long downloads,
        string summary,
        IEnumerable<string> tags,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        ResourceType type,
        RepositoryAssetMeta resource
    )
    {
        Caption = caption;
        IconSource = iconSource;
        Author = author;
        Downloads = downloads;
        Summary = summary;
        Tags = tags;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Type = type;
        Resource = resource;
    }

    public string Caption { get; set; }
    public Uri? IconSource { get; set; }
    public string Author { get; set; }
    public long Downloads { get; set; }
    public string Summary { get; set; }
    public IEnumerable<string> Tags { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ResourceType Type { get; set; }
    public RepositoryAssetMeta Resource { get; set; }
}
using System;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Resources;

namespace Polymerium.App.Models;

public class SearchCenterResultItemModel
{
    public SearchCenterResultItemModel(string caption, Uri iconSource, string author, string summary, ResourceType type,
        RepositoryAssetMeta tag)
    {
        Caption = caption;
        IconSource = iconSource;
        Author = author;
        Summary = summary;
        Type = type;
        Resource = tag;
    }

    public string Caption { get; set; }
    public Uri IconSource { get; set; }
    public string Author { get; set; }
    public string Summary { get; set; }
    public ResourceType Type { get; set; }
    public RepositoryAssetMeta Resource { get; set; }
}
using System;
using Polymerium.Abstractions.Resources;

namespace Polymerium.App.Models;

public class SearchCenterResultItemModel
{
    public SearchCenterResultItemModel(string caption, Uri iconSource, string author, string summary, string body,
        ResourceType type, object tag)
    {
        Caption = caption;
        IconSource = iconSource;
        Author = author;
        Summary = summary;
        Body = body;
        Type = type;
        Tag = tag;
    }

    public string Caption { get; set; }
    public Uri IconSource { get; set; }
    public string Author { get; set; }
    public string Summary { get; set; }
    public string Body { get; set; }
    public ResourceType Type { get; set; }
    public object Tag { get; set; }
}
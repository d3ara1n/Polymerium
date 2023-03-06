using System;
using Polymerium.Abstractions.Resources;

namespace Polymerium.App.Models;

public class InstanceAttachmentItemModel
{
    public InstanceAttachmentItemModel(ResourceType type, string caption, string author, Uri? iconSource,
        string version, string summary, Uri attachment)
    {
        Type = type;
        Caption = caption;
        Author = author;
        IconSource = iconSource;
        Version = version;
        Summary = summary;
        Attachment = attachment;
    }

    public ResourceType Type { get; set; }
    public string Caption { get; set; }
    public string Author { get; set; }
    public Uri? IconSource { get; set; }
    public string Version { get; set; }
    public string Summary { get; set; }
    public Uri Attachment { get; set; }
}
using System;
using System.Windows.Input;
using Polymerium.Abstractions.Resources;

namespace Polymerium.App.Models;

public class InstanceAttachmentItemModel
{
    public InstanceAttachmentItemModel(ResourceType type, string caption, string author, Uri? iconSource,
        Uri? reference,
        string version, string summary, Uri attachment, ICommand openReferenceCommand)
    {
        Type = type;
        Caption = caption;
        Author = author;
        IconSource = iconSource;
        Reference = reference;
        Version = version;
        Summary = summary;
        Attachment = attachment;
        OpenReferenceCommand = openReferenceCommand;
    }

    public ResourceType Type { get; set; }
    public string Caption { get; set; }
    public string Author { get; set; }
    public Uri? IconSource { get; set; }
    public Uri? Reference { get; set; }
    public string Version { get; set; }
    public string Summary { get; set; }
    public Uri Attachment { get; set; }
    public ICommand OpenReferenceCommand { get; set; }
}
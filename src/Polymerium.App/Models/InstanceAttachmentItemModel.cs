using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Abstractions.Meta;
using Polymerium.Abstractions.Resources;

namespace Polymerium.App.Models;

public class InstanceAttachmentItemModel : ObservableObject
{
    public InstanceAttachmentItemModel(
        ResourceType type,
        string caption,
        string author,
        Uri? iconSource,
        Uri? reference,
        string version,
        string summary,
        Attachment attachment,
        bool isLocked,
        ICommand openReferenceCommand,
        ICommand removeCommand,
        ICommand enableCommand,
        ICommand disableCommand
    )
    {
        Type = type;
        Caption = caption;
        Author = author;
        IconSource = iconSource;
        Reference = reference;
        Version = version;
        Summary = summary;
        Attachment = attachment;
        IsLocked = isLocked;
        OpenReferenceCommand = openReferenceCommand;
        RemoveCommand = removeCommand;
        EnableCommand = enableCommand;
        DisableCommand = disableCommand;
    }

    public ResourceType Type { get; set; }
    public string Caption { get; set; }
    public string Author { get; set; }
    public Uri? IconSource { get; set; }
    public Uri? Reference { get; set; }
    public string Version { get; set; }
    public string Summary { get; set; }
    public Attachment Attachment { get; set; }
    public bool IsLocked { get; set; }
    public bool IsEnabled
    {
        get => Attachment.Enabled;
        set => SetProperty(Attachment.Enabled, value, Attachment, (a, v) => a.Enabled = v);
    }
    public ICommand OpenReferenceCommand { get; set; }
    public ICommand RemoveCommand { get; set; }
    public ICommand EnableCommand { get; set; }
    public ICommand DisableCommand { get; set; }
}

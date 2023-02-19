using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Meta;

namespace Polymerium.App.Models;

public class GameInstanceModel : ObservableObject
{
    public GameInstanceModel(GameInstance instance)
    {
        Inner = instance;
        Components = new SynchronizedCollection<Component>(Inner.Metadata.Components);
        Attachments = new SynchronizedCollection<Uri>(Inner.Metadata.Attachments);
        Configuration = new ConfigurationModel(Inner.Configuration);
    }

    public GameInstance Inner { get; }

    public SynchronizedCollection<Component> Components { get; }
    public SynchronizedCollection<Uri> Attachments { get; }

    public ConfigurationModel Configuration { get; }

    public string CoreVersion { get; set; }

    public string Name
    {
        get => Inner.Name;
        set
        {
            Inner.Name = value;
            OnPropertyChanged();
        }
    }

    public string FolderName
    {
        get => Inner.FolderName;
        set
        {
            Inner.FolderName = value;
            OnPropertyChanged();
        }
    }

    public string Author
    {
        get => Inner.Author;
        set
        {
            Inner.Author = value;
            OnPropertyChanged();
        }
    }

    public string Id => Inner.Id;

    public string ThumbnailFile
    {
        get => Inner.ThumbnailFile ?? string.Empty;
        set
        {
            Inner.ThumbnailFile = value;
            OnPropertyChanged();
        }
    }

    public string? BoundAccountId
    {
        get => Inner.BoundAccountId;
        set
        {
            Inner.BoundAccountId = value;
            OnPropertyChanged();
        }
    }

    public DateTimeOffset CreatedAt
    {
        get => Inner.CreatedAt;
        set => Inner.CreatedAt = value;
    }

    public DateTimeOffset? LastPlay
    {
        get => Inner.LastPlay;
        set
        {
            Inner.LastPlay = value;
            OnPropertyChanged();
        }
    }

    public DateTimeOffset? LastRestore
    {
        get => Inner.LastRestore;
        set
        {
            Inner.LastRestore = value;
            OnPropertyChanged();
        }
    }

    public int ExceptionCount
    {
        get => Inner.ExceptionCount;
        set
        {
            Inner.ExceptionCount = value;
            OnPropertyChanged();
        }
    }

    public int PlayCount
    {
        get => Inner.PlayCount;
        set
        {
            Inner.PlayCount = value;
            OnPropertyChanged();
        }
    }

    public TimeSpan PlayTime
    {
        get => Inner.PlayTime;
        set
        {
            Inner.PlayTime = value;
            OnPropertyChanged();
        }
    }
}